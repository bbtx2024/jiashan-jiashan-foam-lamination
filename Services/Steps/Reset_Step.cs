using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using HandyControl.Controls;
using QA.Business.Component.Camera;
using QA.Business.Component.HIVE;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Steps
{
    public class Reset_StepNo1 : IStepStation1
    {
        #region Field
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private PLC_Component _plc_Component;
        private Hive_Component _hive_Component;
        private MotionGoogol_Component _mGoogol_Component;
        private Camera_Component _camera_Component;
        private IEventAggregator _eventAggregator;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Reset;
        #endregion

        #region Constructor
        public Reset_StepNo1()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _eventAggregator = IoC.Get<IEventAggregator>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            try
            {
                _baseBiz.AutoRun = true;
                //清除临时报警
                _baseBiz.ClearRunAlarm();
                //加载系统参数
                _paramManager.LoadParams();
                //重新加载所有组件
                _baseBiz.InitialReset();
                //给plc复位中信号
                _plc_Component.SetPcMachineStatus(PcToPlcMachineStatus.复位中);
                //重置标记
                _stepStatus.AllowStation2Start = 0;
                _stepStatus.AllowStation3Start = 0;
                //重置界面
                for (int cav = 1; cav <= 12; cav++)
                {
                    PublishCavityStateMsg(cav, EN_TrayStatus.Common);
                }
                //如果没有使能，获取使能
                if (!_mGoogol_Component.GetServoEnabledStatus() || _mGoogol_Component.Exit())
                {
                    if (!_mGoogol_Component.SetServeOnOff(true))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, "控制器获取伺服使能失败", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                    }
                    DateTime starttime = DateTime.Now;
                    while (true)
                    {
                        if ((DateTime.Now - starttime).TotalSeconds > 5)
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "控制器获取伺服使能失败", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                        }
                        await Task.Delay(200);
                        if (_mGoogol_Component.GetServoEnabledStatus())
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, "控制器已获取伺服使能", En_Logout_Type.Run, true);
                            break;
                        }
                    }
                }
                //上抬所有气缸
                if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "吸嘴所有气缸上抬失败", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                }
                //不关真空吸，关真空吹
                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, "所有吸嘴关真空吹失败", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                    }
                }
                if (!_plc_Component.SetSuctionFilm(0))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"吸废膜关吸失败", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                }
                //复位所有轴
                if (!_mGoogol_Component.ResetAllAxis() || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "控制器复位失败", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                }
                //设置当前位置为原点位置
                _mGoogol_Component.ZeroPos(En_StationNo.StationNo1);
                _mGoogol_Component.ZeroPos(En_StationNo.StationNo2);
                if (!_mGoogol_Component.SetSoftLimit() || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "设置限位失败", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                }
                if (!_mGoogol_Component.CleanAlarm() || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "清除轴报警失败", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                }
                _mGoogol_Component.SetExitSts(false);
                //设置示教中速
                if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "设置示教中速失败", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                }
                //如果需要自动抛料，且吸嘴真空吸还开着，则全部抛料
                if (_stepStatus.CacheParamManager.HomeUiParam.Enable.AutoThrowTapesAfterReset)
                {
                    for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                    {
                        if (_mGoogol_Component.IsNozzleOpenSuction(nozzleNo))
                        {
                            if (!_stepStatus.ThrowAllTapes(true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "清料失败", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                            }
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[] { 0, 0 }, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "清料后回原点失败", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                            }
                            break;
                        }
                    }
                }
                //设置速度为工作速度
                if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Work) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "设置工作速度失败", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.ResetErr, EN_RunStep.Err);
                }
                //重置缓存状态
                _stepStatus.CurNeedProcess.Clear();
                _stepStatus.CurNeedDwell.Clear();
                _stepStatus.ClearAllCarriers();
                for (int i = 0; i < 2; i++)
                {
                    _stepStatus.NozzleInhaleNotReadyTimes1[i] = 0;
                    _stepStatus.NozzleInhaleNotReadyTimes2[i] = 0;
                }
                _stepStatus.ClearAllCarriers();
                _stepStatus.isCavityNull = 0;
                //二次重置界面，防止出现某一个穴位仍然是err的情况
                for (int cav = 1; cav <= 12; cav++)
                {
                    PublishCavityStateMsg(cav, EN_TrayStatus.Common);
                }
                //对图像进行处理
                //_camera_Component.MoveUnusedImgs(true);
                //_camera_Component.RestMoveUnusedImgs(true);
                //给PLC写复位完成
                _plc_Component.SetPcMachineStatus(PcToPlcMachineStatus.复位完成);
                await Task.Delay(100);
                _plc_Component.SetPcMachineStatus(PcToPlcMachineStatus.空闲中);
                NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, "复位完成", En_Logout_Type.Alarm, true);
            }
            finally
            {
                //复位结束后，无论是否完成复位，都将autorun置为false
                _baseBiz.AutoRun = false;
            }
            return (false, EN_RunRet.ResetErr, EN_RunStep.Idle);
        }

        private void PublishCavityStateMsg(int cavity, EN_TrayStatus status)
        {
            _eventAggregator.Publish(new CarrierInfoPanelMessage() { Cavity = cavity, Status = status, }, action => { Task.Run(action); });
        }
    }

    public class Reset_Step : IStepStation2
    {
        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Reset;
        #endregion

        #region Constructor
        public Reset_Step()
        {
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(100);
            return (false, EN_RunRet.ResetErr, EN_RunStep.Idle);
        }
    }
    public class Reset_StepNo3 : IStepStation3
    {
        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Reset;
        #endregion

        #region Constructor
        public Reset_StepNo3()
        {
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(100);
            return (false, EN_RunRet.ResetErr, EN_RunStep.Idle);
        }
    }
}
