using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using IComponents = QA.Business.Interfaces.IComponent;//注意在MEF中也有IComponent
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Steps
{
    public class AutoStart_Station1 : IStepStation1, IHandle<ObservableCollection<AlarmInfoModel>>
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private StepStatus _stepStatus;
        private GlobalVariable _globalVariable;
        private Hive_Component _hive_Component;
        #endregion

        #region Property
        public ObservableCollection<AlarmInfoModel> CurrentAlarmInfoModels { get; set; } = new ObservableCollection<AlarmInfoModel>();
        public EN_RunStep RunStep { get; } = EN_RunStep.AutoStart;
        private HomeUiParam_Enable Enable { get => _stepStatus.CacheParamManager.HomeUiParam.Enable; }
        #endregion

        public AutoStart_Station1()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _stepStatus = IoC.Get<StepStatus>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
        }

        #region Caliburn.Micro
        public void Handle(ObservableCollection<AlarmInfoModel> alarmInfo)
        {
            CurrentAlarmInfoModels = alarmInfo;
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponents> components)
        {
            try
            {
                await Task.Delay(0);
                var motion = components.FirstOrDefault(s => s is MotionGoogol_Component) as MotionGoogol_Component;
                var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;
                var mes = components.FirstOrDefault(s => s is MES_Component) as MES_Component;

                //检测是否复位完成
                if (!motion.IsResetCompleted)
                {
                    ShowErrTip("轴未复位");
                    return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                //检查吸嘴启用了几个
                if (_stepStatus.GetEnableNozzleCount() == 0)
                {
                    ShowErrTip("没有吸嘴处于启用状态");
                    return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                //检查是否有报警
                if (CurrentAlarmInfoModels.Count > 0)
                {
                    ShowErrTip("系统有报警");
                    return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                //检查飞达是否锁紧
                if (!plc.IsFeederLocked())
                {
                    ShowErrTip("飞达未锁紧");
                    return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                //检查下视觉、抛料点位Y是否小于安全距离Y
                var safeY2 = _stepStatus.ParamManager.MotionGoogolParam.SafeY2;
                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    var leftPickY = _stepStatus.FeederSingleTapePos(nozzleNo, 0)[1];
                    if (leftPickY > safeY2)
                    {
                        ShowErrTip($"吸嘴{nozzleNo}左取料点位Y{leftPickY.ToString("f2")}大于安全距离Y{safeY2.ToString("f2")}");
                        return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                    }
                    var rightPickY = _stepStatus.FeederSingleTapePos(nozzleNo, 1)[1];
                    if (rightPickY > safeY2)
                    {
                        ShowErrTip($"吸嘴{nozzleNo}右取料点位Y{rightPickY.ToString("f2")}大于安全距离Y{safeY2.ToString("f2")}");
                        return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                    }
                    var downCamY = _stepStatus.DownCameraPos()[1];
                    if (downCamY > safeY2)
                    {
                        ShowErrTip($"吸嘴{nozzleNo}下视觉点位Y{downCamY.ToString("f2")}大于安全距离Y{safeY2.ToString("f2")}");
                        return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                    }
                }
                var throwTapeY = _stepStatus.CacheParamManager.manualPositionParam.AxisNgSiloPos[1];
                if (throwTapeY > safeY2)
                {
                    ShowErrTip($"1号吸嘴抛料点位Y{throwTapeY.ToString("f2")}大于安全距离Y{safeY2.ToString("f2")}");
                    return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                throwTapeY = _stepStatus.CacheParamManager.manualPositionParam.AxisNgSiloPos2[1];
                if (throwTapeY > safeY2)
                {
                    ShowErrTip($"2号吸嘴抛料点位Y{throwTapeY.ToString("f2")}大于安全距离Y{safeY2.ToString("f2")}");
                    return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                //判断是否为流线模式
                if (plc.IsNotNeedProcessMode())
                {
                    if (MessageBox.Show("当前是流线模式，确定启动吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        _stepStatus.AllowStation2Start = 2;
                        return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                    }
                    //ShowErrTip($"当前是流线模式");
                    //return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                //判断HIVE模式
                if (_hive_Component.HiveStatus == EN_HiveStatus.Engineering)
                {
                    if (MessageBox.Show("当前是工程师模式，确定启动吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        _stepStatus.AllowStation2Start = 2;
                        return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                    }
                }
                else if (_hive_Component.HiveStatus == EN_HiveStatus.PlannedDowntime)
                {
                    ShowErrTip("当前是手动计划停机模式");
                    return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                }
                else if (_hive_Component.HiveStatus == EN_HiveStatus.Downtime)
                {
                    if (_hive_Component.ErrDetail == "Daily Maintenance")
                    {
                        if (MessageBox.Show("当前是工程师模式，确定启动吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        {
                            _stepStatus.AllowStation2Start = 2;
                            return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                        }
                    }
                    else
                    {
                        ShowErrTip("当前是宕机模式");
                        return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                    }
                }
                //检查MES
                //if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                //{
                //    if (!plc.IsSimulateRun())
                //    {
                //        if (!mes.GetSipSNs("", out string[] sipSn))
                //        {
                //            ShowErrTip("MES连接异常");
                //            return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                //        }

                //        if (_stepStatus.ParamManager.MESParam.IsCheckVersion)
                //        {
                //            if (!mes.UploadMachineInfo())
                //            {
                //                ShowErrTip("软件版本与MES卡控值不一致");
                //                return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
                //            }
                //        }
                //    }
                //}
                //设置工作速度
                motion?.ExitAxisMove(false);
                motion?.SetSpeedAll(En_SpeedType.Work);
                //默认所有物料均未加工
                if (_stepStatus.CurrentProcedure != null)
                {
                    _stepStatus.ResetIsUpCamFinishedState();
                }
                //获取当前坐标
                if (!motion.GetCurPos())
                {
                    ShowErrTip("未能获取轴系当前坐标");
                    return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                //检测是否所有轴都在复位位置
                if (motion.CurPos[(byte)En_AxisNum.X1] > 1 || motion.CurPos[(byte)En_AxisNum.Y1] > 1
                    || motion.CurPos[(byte)En_AxisNum.X2] > 1 || motion.CurPos[(byte)En_AxisNum.Y2] > 1 || motion.CurPos[(byte)En_AxisNum.Z2] > 1)
                {
                    ShowErrTip("有轴不在原点位置");
                    return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                //所有气缸缩
                if (!motion.SetAllCylindersUpDown(true))
                {
                    ShowErrTip("吸嘴所有气缸缩失败");
                    return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                //判断要不要抛料
                if (_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                {
                    HashSet<int> throwTapeNozzleNoSet = new HashSet<int>();
                    for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                    {
                        //所有气缸关吹
                        if (!motion.SetVacuum(nozzleNo, false, false))
                        {
                            ShowErrTip("所有气缸关吹失败");
                            return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //开吸且未吸到的吸嘴应抛料（有可能歪了，所以还是要吹一下）
                        if (motion.IsNozzleOpenSuction(nozzleNo) && !motion.GetMaterialReady(nozzleNo))
                        {
                            //开吸未吸到料，直接抛
                            throwTapeNozzleNoSet.Add(nozzleNo);
                        }
                    }
                    if (throwTapeNozzleNoSet.Count > 0)
                    {
                        if (!_stepStatus.ThrowTapes(throwTapeNozzleNoSet, false, true))
                        {
                            ShowErrTip("清料失败");
                            return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                }
                else
                {
                    if (!_stepStatus.ThrowAllTapes(true))
                    {
                        ShowErrTip("清料失败");
                        return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                }
                //锁定轴系，初始不允许吸嘴去贴合区域
                _stepStatus.SetMoveMutex(true);
                _stepStatus.ManualResetEvt_CtrlPlace.Reset();
                //允许PLC启动
                //plc.SetSwReady(true);
                //允许Station2进入其他步骤
                _stepStatus.AllowStation2Start = 1;
                _stepStatus.AllowStation3Start = 1;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "AutoStart1->" + ex.ToString(), En_Logout_Type.Alarm, true);
                _stepStatus.AllowStation2Start = 2;
                _stepStatus.AllowStation3Start = 2;
                return (true, EN_RunRet.MotionOk, EN_RunStep.Err);
            }
            return (true, EN_RunRet.MotionOk, EN_RunStep.AutoRunIdle);
        }

        public void ShowErrTip(string info)
        {
            var _baseBiz = IoC.Get<IBaseBiz>() as BaseBiz;
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"AutoStart->{info}", En_Logout_Type.Alarm, true);
            var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, info, 100);
            MessageBox.Warning($"{info}，无法启动！", "警告");
            _baseBiz.RemoveRunAlarm(alarm);
            _stepStatus.AllowStation2Start = 2;
            _stepStatus.AllowStation3Start = 2;
        }
    }

    public class AutoStart_Station2 : IStepStation2
    {
        #region Field
        StepStatus _stepStatus;
        private readonly MotionGoogol_Component _mGoogol_Component;
        #endregion

        #region Property
        public EN_RunStep RunStep { get; } = EN_RunStep.AutoStart;
        #endregion

        public AutoStart_Station2()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponents> components)
        {
            try
            {
                await Task.Delay(0);
                //检测是否允许启动
                while (_stepStatus.AllowStation2Start == 0)
                {
                    if (_stepStatus.NextStep2 != RunStep)
                    {
                        return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    }
                    await Task.Delay(100);
                }
                if (_stepStatus.AllowStation2Start == 2)
                {
                    return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                //只有AllowStation2Start值为1才能走到这里，继续运行
                _stepStatus.AllowStation2Start = 0;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "AutoStart2->" + ex.ToString(), En_Logout_Type.Alarm, true);
                return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            return (true, EN_RunRet.MotionOk, EN_RunStep.AutoRunIdle);

        }
    }
    public class AutoStart_Station3 : IStepStation3
    {
        #region Field
        StepStatus _stepStatus;
        private readonly MotionGoogol_Component _mGoogol_Component;
        #endregion

        #region Property
        public EN_RunStep RunStep { get; } = EN_RunStep.AutoStart;
        #endregion

        public AutoStart_Station3()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponents> components)
        {
            try
            {
                await Task.Delay(0);
                //检测是否允许启动
                while (_stepStatus.AllowStation3Start == 0)
                {
                    if (_stepStatus.NextStep3 != RunStep)
                    {
                        return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
                    }
                    await Task.Delay(100);
                }
                if (_stepStatus.AllowStation3Start == 2)
                {
                    return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                //只有AllowStation3Start值为1才能走到这里，继续运行
                _stepStatus.AllowStation3Start = 0;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "AutoStart3->" + ex.ToString(), En_Logout_Type.Alarm, true);
                return (true, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            return (true, EN_RunRet.MotionOk, EN_RunStep.AutoRunIdle);

        }
    }
}
