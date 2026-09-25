using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.Camera;
using QA.Business.Component.HIVE;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Steps
{
    //吸嘴吸取物料到下视觉拍照
    public class DownCameraIdentifyMaterialStep : IStepStation2
    {
        #region Field    
        private CameraParam _cameraParam = null;
        private StepStatus _stepStatus;
        private CacheParamManager _cacheParamManager;
        private ParamManager _paramManager;
        private protected MotionGoogol_Component _mGoogol_Component;
        private protected PLC_Component _plc_Component;
        private protected Camera_Component _camera_Component;
        private Hive_Component _hive_Component;
        private IEventAggregator _eventAggregator;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.DownCameraIdentifyMaterialStep;
        #endregion

        private HomeUiParam_Enable Enable { get => _stepStatus.CacheParamManager.HomeUiParam.Enable; }

        #region Constructor
        public DownCameraIdentifyMaterialStep()
        {
            _cameraParam = IoC.Get<CameraParam>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _stepStatus = IoC.Get<StepStatus>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _paramManager = IoC.Get<ParamManager>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            HashSet<int> throwTapeNozzleNoSet = new HashSet<int>();
            _stepStatus.DownCamOk = new bool[4];
            CarrierStatus carrierStatus = null;

            float[] downCamPos;
            En_AxisNum en_AxisNum;
            //判断移动到哪一个下视觉点位,不等待到位
            if (_stepStatus.PickTapeOk[0] && _stepStatus.UseNozzle(1))
            {
                downCamPos = _stepStatus.DownCameraPos(1);
                en_AxisNum = En_AxisNum.R1;
            }
            else
            {
                downCamPos = _stepStatus.DownCameraPos(2);
                en_AxisNum = En_AxisNum.R2;
            }
            //float[] downCamPos = _stepStatus.DownCameraPos();
            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(en_AxisNum, downCamPos, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->运行到下视觉失败(Chạy đến vị trí dưới tầm nhìn đầu tiên thất bại)，Axis[X2,Y2,Z2,R1,R2]:{NLogTrace.GetFloatArrayString(downCamPos)}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            //提前移动R2轴
            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R2, _stepStatus.DownCameraPos(2)[3],false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->运行到下视觉失败(Chạy đến vị trí dưới tầm nhìn đầu tiên thất bại)，Axis[X2,Y2,Z2,R1,R2]:{NLogTrace.GetFloatArrayString(downCamPos)}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }

            //提前伸出所有需要拍照的吸嘴气缸，不等待到位
            for (int nozzleIdx = 0; nozzleIdx < 2; nozzleIdx++)
            {
                if (_stepStatus.PickTapeOk[nozzleIdx] && _stepStatus.UseNozzle(nozzleIdx + 1))
                {
                    //下降吸嘴气缸
                    while (!_mGoogol_Component.SetCylinderUpDown(nozzleIdx + 1, false, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                    {
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->吸嘴{nozzleIdx + 1}气缸开始伸出失败(Tất cả các xi lanh bắt đầu vươn ra và thất bại.)", En_Logout_Type.Alarm, true);
                        if (MessageBox.Show("气缸伸出失败,是:重新伸出气缸，否：终止程序", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                        {
                            continue;
                        }
                        else
                        {
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                }
            }
            //判断吸嘴气缸下降到位
            for (int nozzleIdx = 0; nozzleIdx < 2; nozzleIdx++)
            {
                if (_stepStatus.PickTapeOk[nozzleIdx] && _stepStatus.UseNozzle(nozzleIdx + 1))
                {
                    while (!_mGoogol_Component.IsCylinderUpDownReady(nozzleIdx + 1, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                    {
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->吸嘴{nozzleIdx + 1}气缸伸出失败(Tất cả các xi lanh bắt đầu vươn ra và thất bại.)", En_Logout_Type.Alarm, true);
                        if (MessageBox.Show("气缸伸出失败,是:重新伸出气缸，否：终止程序", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                        {
                            continue;
                        }
                        else
                        {
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                }
            }
            //运行到下视觉拍照位置
            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(en_AxisNum, downCamPos, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->运行到下视觉失败(Chạy đến vị trí dưới tầm nhìn đầu tiên thất bại)，Axis[X2,Y2,Z2,R1,R2]:{NLogTrace.GetFloatArrayString(downCamPos)}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            bool firstDownCam = true;
            for (int nozzleIdx = 0; nozzleIdx < 2; nozzleIdx++)
            {
                if (_stepStatus.PickTapeOk[nozzleIdx] && _stepStatus.UseNozzle(nozzleIdx + 1))
                {
                    if (firstDownCam)
                    {
                        //第一次下视觉要确保气缸伸到位，注意气缸下到位不代表已经伸到最大距离，应该再等一会
                        while (!_mGoogol_Component.IsCylinderUpDownReady(nozzleIdx + 1, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->{nozzleIdx + 1}号吸嘴气缸伸出失败(Tất cả các xi lanh thoát thất bại)", En_Logout_Type.Alarm, true);
                            if (MessageBox.Show("吸嘴气缸伸出失败,是:重新伸出气缸，否：终止程序", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            {
                                continue;
                            }
                            else
                            {
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                        }
                        await Task.Delay(50);
                        firstDownCam = false;
                    }
                    if (_plc_Component.IsSimulateRun())
                    {
                        //空跑需要模拟拍照需要时间
                        await Task.Delay(300);
                        _stepStatus.DownCamOk[nozzleIdx] = true;
                        continue;
                    }
                    //拍照前延时
                    if (firstDownCam)
                    {
                        await Task.Delay(_stepStatus.ParamManager.CameraParam.DownCamWaitTime);
                    }

                /*********************2024/07/03新增防呆等上相机先拍照********************/
                    //判断是否有新载具进来
                    while (_stepStatus.GetCurCarrier() == null)
                    {
                        await Task.Delay(50);
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    }
                    //判断Y1回到原点
                    while (_mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] >= 1)
                    {
                        await Task.Delay(50);
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    }
                    //上视觉结束时，才会释放ManualResetEvt_CtrlPlace锁
                    _stepStatus.ManualResetEvt_CtrlPlace.WaitOne();
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    carrierStatus = _stepStatus.GetCurCarrier();
                /*************************************************************/

                DownCCDNG:
                    _stepStatus.CurrentNozeNum = nozzleIdx;
                    //运行到下视觉拍照位置
                    downCamPos = _stepStatus.DownCameraPos(nozzleIdx + 1);
                    if (nozzleIdx == 0)
                    {
                        en_AxisNum = En_AxisNum.R1;
                    }
                    else
                    {
                        en_AxisNum = En_AxisNum.R2;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(en_AxisNum, downCamPos, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                    {
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->运行到下视觉拍照点{nozzleIdx+1}失败(Chạy đến vị trí dưới tầm nhìn đầu tiên thất bại)，Axis[X2,Y2,Z2,R1,R2]:{NLogTrace.GetFloatArrayString(downCamPos)}", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                    //如果定位ng，就直接放到ng文件夹；如果ok，等待贴合的时候再移动过去
                    if (!_camera_Component.DownCdd(nozzleIdx + 1, downCamPos[0], downCamPos[1], downCamPos[3]) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                    {
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->定位{nozzleIdx + 1}号吸嘴上的物料失败(Định vị hình ảnh dưới thất bại)，Axis[X2,Y2,Z2,R{nozzleIdx + 1}]", En_Logout_Type.Alarm, true);
                        if (_camera_Component.IsOP_DownCdd() == true)
                        {
                            if (MessageBox.Show($"下视觉NG，是：重新拍照；否：继续", "人工判断下视觉", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                goto DownCCDNG;
                            }
                        }
                        if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                        {
                            _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddCamNgCount(nozzleIdx + 1);
                        }
                        else
                        {
                            _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddCamNgDebugCount(nozzleIdx + 1);
                        }

                        _stepStatus.DownCamOk[nozzleIdx] = false;
                        throwTapeNozzleNoSet.Add(nozzleIdx + 1);
                        carrierStatus.cavStateStr[_stepStatus.CurrentCavNum - 1] = "下视觉定位NG";
                        SaveCamdata(carrierStatus, nozzleIdx, _camera_Component.downCDDMode.ResPosConvert());//存储下视觉NG的定位信息
                                                                                                             //单吸嘴连续失败报警
                        _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx]++;
                        int oneNgCount = _stepStatus.ParamManager.OtherSettingParam.OneNozzleGetTapeFailCount;
                        int oneNgCountNow = _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx];
                        if (oneNgCountNow >= oneNgCount)
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->{nozzleIdx + 1}号吸嘴连续{oneNgCountNow}次下视觉NG(Miệng hút liên tục lấy thất bại)", En_Logout_Type.Alarm, true);
                            var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleIdx + 1}号吸嘴连续{oneNgCountNow}次下视觉NG(Miệng hút liên tục lấy thất bại)", 109);
                            if (MessageBox.Show($"{nozzleIdx + 1}号吸嘴连续{oneNgCountNow}次下视觉NG！\n确定继续吗？\nMiệng hút liên tục lấy nguyên liệu thất bại!\ncó nên tiếp tục hay không?", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx] = 0;
                                _stepStatus.NozzleInhaleNotReadyTimes2[nozzleIdx] = 0;
                            }
                            else
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                            }
                        }
                        //所有吸嘴不连续失败报警
                        int allNgCount = _stepStatus.ParamManager.OtherSettingParam.NozzlesGetTapeFailCount;
                        int allNgCountNow = 0;
                        for (int i = 0; i < 2; i++)
                        {
                            allNgCountNow += _stepStatus.NozzleInhaleNotReadyTimes2[i];
                        }
                        if (allNgCountNow >= allNgCount)
                        {
                            string nozzleNgInfo = $"N1:{_stepStatus.NozzleInhaleNotReadyTimes2[0]} N2:{_stepStatus.NozzleInhaleNotReadyTimes2[1]}";
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->所有吸嘴连续{allNgCountNow}次下视觉NG，{nozzleNgInfo}(Miệng hút liên tục lấy thất bại, {nozzleNgInfo})", En_Logout_Type.Alarm, true);
                            var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"所有吸嘴连续{allNgCountNow}次下视觉NG(Miệng hút liên tục lấy thất bại)", 109);
                            if (MessageBox.Show($"所有吸嘴连续{allNgCountNow}次下视觉NG！\n确定继续吗？\n{nozzleNgInfo}\nMiệng hút liên tục lấy nguyên liệu thất bại!\ncó nên tiếp tục hay không?\n{nozzleNgInfo}", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                for (int i = 0; i < 2; i++)
                                {
                                    _stepStatus.NozzleInhaleNotReadyTimes2[i] = 0;
                                }
                            }
                            else
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                            }
                        }
                    }
                    else
                    {
                        _stepStatus.DownCamOk[nozzleIdx] = true;
                        var data = _camera_Component.downCDDMode.ResPosConvert();
                        //下视觉防呆卡控（生产状态下启用下视觉防呆才可启用）
                        if (_paramManager.CameraParam.IsDownMarkBus && !DownCDDfoolproof(data, nozzleIdx + 1))
                        {
                            carrierStatus.cavStateStr[_stepStatus.CurrentCavNum - 1] = "下视觉定位超标准值";
                            SaveCamdata(carrierStatus, nozzleIdx, _camera_Component.downCDDMode.ResPosConvert());//存储下视觉NG的定位信息
                            if (DownCDDNG($"{nozzleIdx + 1}吸嘴下视觉超范围{data[0]},{data[1]},{data[2]} 是否重拍")) goto DownCCDNG;
                            if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                            {
                                _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddCamNgCount(nozzleIdx + 1);
                            }
                            else
                            {
                                _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddCamNgDebugCount(nozzleIdx + 1);
                            }
                            _stepStatus.DownCamOk[nozzleIdx] = false;
                            throwTapeNozzleNoSet.Add(nozzleIdx + 1);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->定位{nozzleIdx + 1}号吸嘴上的物料失败(Định vị hình ảnh dưới thất bại)，Axis[X2,Y2,Z2,R{nozzleIdx + 1}]", En_Logout_Type.Alarm, true);
                        }
                        else
                        {
                            //单吸嘴连续失败清零
                            _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx] = 0;
                            //存储下视觉定位信息
                            string s = "";
                            for (int i = 0; i < data.Length; i++)
                            {
                                s += data[i] + ",";
                            }
                            _stepStatus.tapeBaseDistance[nozzleIdx] = s.Substring(0, s.Length - 1);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"DownCam->定位{nozzleIdx + 1}号吸嘴上的物料成功，Axis[X2,Y2,Z2,R{nozzleIdx + 1}]:{NLogTrace.GetFloatArrayString(downCamPos)}", En_Logout_Type.Run, true);
                        }
                    }
                    
                }
            }
            //下视觉气缸伸出，拍照结束后应该把所有气缸收回
            while (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "DownCam->未能收回所有气缸(Không thể lấy lại tất cả các xi lanh)", En_Logout_Type.Alarm, true);
                if (MessageBox.Show("吸嘴气缸伸出失败,是:重新伸出气缸，否：终止程序", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    continue;
                }
                else
                {
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
            }
            //把ng料抛掉，再取
            if (throwTapeNozzleNoSet.ToList().Count > 0)
            {
                if (!_stepStatus.ThrowTapes(throwTapeNozzleNoSet, true, false, EN_TossingCode.MaterialDeflect, carrierStatus, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->抛料失败(Ném thất bại)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                return (false, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.PlaceMaterialToCarrierStep);
        }


        /// <summary>
        /// 下视觉Tape定位防呆检测（定位数据是否超过标准值）
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool DownCDDfoolproof(float[] data, int index)
        {
            try
            {
                if (data.Length != 3)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"DownCam->下相机定位防呆-获取定位数据个数异常", En_Logout_Type.Alarm, true);
                    return false;
                }
                float X = data[0];
                float Y = data[1];
                float Angle = data[2];
                float MarkX = index == 1 ? _paramManager.CameraParam.DownMark_X1 : _paramManager.CameraParam.DownMark_X2;
                float MarkposX = index == 1 ? _paramManager.CameraParam.DownMarkpos_X : _paramManager.CameraParam.DownMarkpos_X;
                float MarkY = index == 1 ? _paramManager.CameraParam.DownMark_Y1 : _paramManager.CameraParam.DownMark_Y2;
                float MarkposY = index == 1 ? _paramManager.CameraParam.DownMarkpos_Y : _paramManager.CameraParam.DownMarkpos_Y;
                float MarkAngle = _paramManager.CameraParam.DownMarkAngle;
                float MarkAnglepos = _paramManager.CameraParam.DownMarkAnglePos;

                if (!(MarkX + MarkposX > X && MarkX - MarkposX < X))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->下相机定位防呆{index}号吸嘴X超限：[基准值：{MarkX},偏移量±:{MarkposX},X值：{X}]", En_Logout_Type.Run, true);
                    return false;
                }
                if (!(MarkY + MarkposY > Y && MarkY - MarkposY < Y))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->下相机定位防呆{index}号吸嘴Y超限：[基准值：{MarkY},偏移量±:{MarkposY},Y值：{Y}]", En_Logout_Type.Run, true);
                    return false;
                }
                if (!(MarkAngle + MarkAnglepos > Angle && MarkAngle - MarkAnglepos < Angle))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->下相机定位防呆{index}号吸嘴角度超限：[基准值：{MarkY},偏移量±:{MarkposY},Y值：{Y}]", En_Logout_Type.Run, true);
                    return false;
                }

                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->定位防呆{index}号吸嘴：[基准值：{MarkX},{MarkY},{MarkAngle},偏移量±:{MarkposX},{MarkposY},{MarkAnglepos},值：{X},{Y},{Angle}]", En_Logout_Type.Run, true);
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->下相机定位数据格式错误", En_Logout_Type.Run, true);
                return false;
            }
        }

        /// <summary>
        /// 下视觉定位NG弹框提示
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool DownCDDNG(string msg)
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Camera, msg, 109);
            if (MessageBox.Show(msg, "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                _baseBiz.RemoveRunAlarm(alarm);
                return true;
            }
            else
            {
                _baseBiz.RemoveRunAlarm(alarm);
                return false;
            }
        }

        /// <summary>
        /// 保存下相机定位NG数据
        /// </summary>
        /// <param name="carrierStatus"></param>
        /// <param name="nozzleIdx"></param>
        /// <param name="data"></param>
        private void SaveCamdata(CarrierStatus carrierStatus, int nozzleIdx, float[] data)
        {
            try
            {
                string s = "";
                for (int i = 0; i < data.Length; i++)
                {
                    s += data[i] + ",";
                }
                _stepStatus.tapeBaseDistance[nozzleIdx] = s.Substring(0, s.Length - 1);
                carrierStatus.Tape_Nozzle[_stepStatus.CurrentCavNum - 1] = nozzleIdx + 1;
                carrierStatus.tapeBaseDistance[_stepStatus.CurrentCavNum - 1] = _stepStatus.tapeBaseDistance[nozzleIdx];
                _stepStatus.SaveToCsv(carrierStatus, false, _stepStatus.CurrentCavNum - 1, nozzleIdx);
            }
            catch (Exception)
            { }
        }


    }
}
