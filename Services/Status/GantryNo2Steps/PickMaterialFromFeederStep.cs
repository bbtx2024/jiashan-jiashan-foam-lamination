using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.Camera;
using QA.Business.Component.HIVE;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.Alarm;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Steps
{
    public class PickMaterialFromFeederStep : IStepStation2
    {
        #region Field    
        private StepStatus _stepStatus;
        private CacheParamManager _cacheParamManager;
        private ParamManager _paramManager;
        private protected MotionGoogol_Component _mGoogol_Component;
        private protected PLC_Component _plc_Component;
        private GlobalVariable _globalVariable;
        private Camera_Component _camera_Component;
        private Hive_Component _hive_Component;
        private MotionGoogolParam _mGoogolParam;
        #endregion

        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.PickMaterialFromFeederStep;

        private HomeUiParam_Enable Enable { get => _stepStatus.CacheParamManager.HomeUiParam.Enable; }

        public PickMaterialFromFeederStep()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _paramManager = IoC.Get<ParamManager>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _mGoogolParam = IoC.Get<MotionGoogolParam>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            //生产流程
            try
            {
                await Task.Delay(0);
                _plc_Component.SetPcMachineStatus(PcToPlcMachineStatus.运行中);
                if (_stepStatus.IFitCav10)
                {
                    //运动到Y2轴安全位置
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Y2, _paramManager.MotionGoogolParam.SafeY2, true, true))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Y2位置{_paramManager.MotionGoogolParam.SafeY2}失败", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                    _stepStatus.IFitCav10 = false;
                }
                //上视觉结束后再取料模式要在这里卡住，等待上视觉结束信号
                if (Enable.PickOpportunity == EN_PickOpportunity.AfterUpCam)
                {
                    if (!_plc_Component.IsSimulateRun() && !Enable.IsFeederCheck)
                    {
                        //避让相机
                        //运动到Z轴安全位置
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2位置0失败", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //获取参数，判断移动到哪一个避位点
                        FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                        switch (feederId)
                        {
                            case FeederId.左飞达:
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder1Pos, true))
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[1]}", En_Logout_Type.Alarm, true);
                                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                                }
                                break;
                            case FeederId.右飞达:
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[1]}", En_Logout_Type.Alarm, true);
                                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                                }
                                break;
                            default:
                                break;
                        }

                    }
                    else
                    {
                        //提前移动到取料位，默认用一号吸嘴，NPI不要求ct
                        if (!MoveToFeederSingleTape(1, false, true).Item1)
                        {
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                    //上视觉结束时，才会释放ManualResetEvt_CtrlPlace锁
                    _stepStatus.ManualResetEvt_CtrlPlace.WaitOne();
                    if (_mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                    {
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    }
                    //二次确认轴移动权限，防止撞机
                    bool upCamOk = _stepStatus.GetMoveMutex() == false && _plc_Component.IsCarrierReady();
                    if (!upCamOk)
                    {
                        return (false, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
                    }
                }
                //判断是否是启动后Feeder第一次拍照,空跑不拍照
                if ((Enable.PickOpportunity == EN_PickOpportunity.AfterUpCam || _stepStatus.isOnePick) && !_plc_Component.IsSimulateRun() && !Enable.IsFeederCheck)
                {
                    //获取参数，判断移动到哪一个避位点
                    FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                    switch (feederId)
                    {
                        case FeederId.左飞达:
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder1Pos, true))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[1]}", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                            break;
                        case FeederId.右飞达:
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[1]}", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                            break;
                        default:
                            break;
                    }
                    if (!_stepStatus.FeederCDD())
                    {
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                    _stepStatus.isOnePick = false;
                }
                if (_plc_Component.IsSimulateRun())
                {
                    //提前移动到取料位，默认用一号吸嘴，NPI不要求ct
                    if (!MoveToFeederSingleTape(1, false, true).Item1)
                    {
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                }
                //获取吸嘴真空吸状态，false说明当前无料，需要取料
                if (!_mGoogol_Component.GetMaterialsReady(out _stepStatus.PickTapeOk) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->获取四个吸嘴吸准备信号失败(Lấy được 4 ống hút. Tín hiệu chuẩn bị thất bại.)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                if (_plc_Component.IsSimulateRun())
                {
                    _stepStatus.PickTapeOk[0] = false;
                    _stepStatus.PickTapeOk[1] = false;
                }
                //当前已经吸到的数目（启用且有料）
                int sucNum = 0;
                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    if (_stepStatus.UseNozzle(nozzleNo) && _stepStatus.PickTapeOk[nozzleNo - 1])
                    {
                        sucNum++;
                    }
                }
                //判断哪些吸嘴要取料
                bool[] needGetTape = new bool[2];
                if (Enable.PickOpportunity == EN_PickOpportunity.AfterUpCam)
                {
                    //上视觉结束后再取料，应取料直至到上视觉ok数目
                    //无需关心平台物料情况
                    if (sucNum >= _stepStatus.CurNeedProcess.Count)
                    {
                        return (false, EN_RunRet.TaskOk, EN_RunStep.DownCameraIdentifyMaterialStep);
                    }
                    for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                    {
                        if (_stepStatus.UseNozzle(nozzleNo) && !_stepStatus.PickTapeOk[nozzleNo - 1])
                        {
                            needGetTape[nozzleNo - 1] = true;
                            sucNum++;
                        }
                        if (sucNum >= _stepStatus.CurNeedProcess.Count)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    //直接取料，应取料直至所有启用的吸嘴都取到
                    //若启用吸嘴为奇数个，无需关心平台物料情况；否则需要保证平台不会剩余物料
                    int useNozzleCount = _stepStatus.GetEnableNozzleCount();
                    if (sucNum >= useNozzleCount)
                    {
                        return (false, EN_RunRet.TaskOk, EN_RunStep.DownCameraIdentifyMaterialStep);
                    }
                    for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                    {
                        if (_stepStatus.UseNozzle(nozzleNo) && !_stepStatus.PickTapeOk[nozzleNo - 1])
                        {
                            needGetTape[nozzleNo - 1] = true;
                        }
                    }
                }
                //获取第一个吸嘴和最后一个吸嘴
                int firstNozzleNo = 0;
                int lastNozzleNo = 0;
                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    if (needGetTape[nozzleNo - 1])
                    {
                        if (firstNozzleNo == 0)
                        {
                            firstNozzleNo = nozzleNo;
                        }
                        lastNozzleNo = nozzleNo;
                    }
                }
                CarrierStatus carrierStatus = new CarrierStatus();
                //NPI模式判断是否有新载具进来，量产模式不进行判断，直接进行飞达取料
                if (Enable.PickOpportunity == EN_PickOpportunity.AfterUpCam)
                {
                    while (_stepStatus.GetCurCarrier() == null)
                    {
                        await Task.Delay(50);
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    }
                    carrierStatus = _stepStatus.GetCurCarrier();
                }

                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    if (needGetTape[nozzleNo - 1])
                    {
                        if (_plc_Component.IsSimulateRun() || Enable.IsFeederCheck)//空跑或则feeder盲取
                        {
                            var ret = GetOneTape(nozzleNo, nozzleNo == firstNozzleNo, nozzleNo == lastNozzleNo, carrierStatus);
                            if (!ret.Item1)
                            {
                                return ret;
                            }
                        }
                        //视觉引导取料
                        else
                        {
                            //如果不是第一个吸嘴，代表没有拍照，执行拍照
                            if (nozzleNo != firstNozzleNo)
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"Pick->平台Tape2单独定位", En_Logout_Type.Alarm, true);
                                if (!_stepStatus.FeederCDD())
                                {
                                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                                }
                            }
                            //初始化需要计算坐标的吸嘴
                            List<int> nozzleNos = new List<int> { nozzleNo };
                            int nozzleNo1 = nozzleNo;
                            //如果Feedeer物料为两个时，需再添加一个吸嘴
                            if (_camera_Component.feederCDDMode.TapeOk.Count(b => b) == 2)
                            {
                                for (int nozzleNo2 = nozzleNo1 + 1; nozzleNo2 <= 2; nozzleNo2++, nozzleNo1++)
                                {
                                    if (needGetTape[nozzleNo2 - 1])
                                    {
                                        nozzleNos.Add(nozzleNo2);
                                    }
                                }
                            }
                            //遍历吸嘴取料
                            for (int i = 0; i < nozzleNos.Count; i++)
                            {
                                if (_camera_Component.feederCDDMode.GetTapeOKNum() > 0)
                                {
                                    bool isTwoTape = false;
                                    //量产模式记录来两个物料，用于判断抛料
                                    if (_camera_Component.feederCDDMode.GetTapeOKNum() == 2 && Enable.PickOpportunity == EN_PickOpportunity.AfterStart)
                                    {
                                        isTwoTape = true;
                                    }
                                    var ret = GetOneTape(nozzleNos[i], nozzleNos[i] == firstNozzleNo, nozzleNos[i] == lastNozzleNo, carrierStatus, _camera_Component.GetPickPos(nozzleNos[i]), isTwoTape);
                                    if (!ret.Item1)
                                    {
                                        return ret;
                                    }
                                }
                                else
                                {
                                    return (false, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
                                }
                                //else
                                //{
                                //    //定位NG按照盲区点位取Tape
                                //    var ret = GetOneTape(nozzleNo, nozzleNo == firstNozzleNo, nozzleNo == lastNozzleNo, carrierStatus);
                                //    if (!ret.Item1)
                                //    {
                                //        return ret;
                                //    }
                                //}
                            }
                            nozzleNo = nozzleNo1;
                        }
                    }
                }
                return (false, EN_RunRet.TaskOk, EN_RunStep.DownCameraIdentifyMaterialStep);

            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{ex.Message}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
        }





        /// <summary>
        /// 移动指定吸嘴到有料的位置
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <param name="waitXYZREnd"></param>
        /// <param name="moveAllR">如果是第一次取料，需要转动所有R</param>
        /// <returns></returns>
        private (bool, EN_RunRet, EN_RunStep) MoveToFeederSingleTape(int nozzleNo, bool waitXYZREnd, bool moveAllR, float[] floats = null)
        {
            float[] feederPos;
            if (_plc_Component.IsSimulateRun())
            {
                //空跑模式下13吸嘴从点位0取料，24吸嘴从点位1取料
                feederPos = _stepStatus.FeederSingleTapePos(nozzleNo, (nozzleNo - 1) % 2);
            }
            else
            {
                //移动到默认取料位
                feederPos = _stepStatus.FeederSingleTapePos(nozzleNo);
                if (floats != null)
                {
                    feederPos[0] = floats[0];
                    feederPos[1] = floats[1];
                    feederPos[3] = floats[2];
                }
            }
            //防呆，判断有没有设定取料位
            if (feederPos[0] < 20)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->取料位X坐标{feederPos[0]}小于20，可能未设置取料位(Vị trí lấy tọa độ X nhỏ hơn 20 và vị trí lấy có thể không được đặt)", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            if (moveAllR)
            {
                //所有启用的吸嘴转到取料角度，防止与其他吸嘴碰到，不用等待结束
                for (int nozzleNo0 = 1; nozzleNo0 <= 2; nozzleNo0++)
                {
                    if (nozzleNo0 == nozzleNo)
                    {
                        continue;
                    }
                    if (_stepStatus.UseNozzle(nozzleNo0))
                    {
                        float feederPosR = _stepStatus.FeederSingleTapePos(nozzleNo0, 0)[3];
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis((En_AxisNum)nozzleNo0 + 4, feederPosR, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->运动到取料吸嘴R失败(Tập thể dục để lấy vòi hút R thất bại)：[R{nozzleNo0}]{feederPosR.ToString("f3")}", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                }
            }
            //飞达高度Z是安全的高度，所以直接移动XYZR就行
            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1 + nozzleNo - 1, feederPos, waitXYZREnd) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->运动到{nozzleNo}号吸嘴 Feeder Pick 坐标失败(Chuyển động đến Sự nịnh hót Feeder Pick Tọa độ Thất bại)，Axis[X2,Y2]:{NLogTrace.GetFloatArrayString(feederPos)}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            return (true, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
        }

        /// <summary>
        /// 让指定的吸嘴吸取一个tape
        /// </summary>
        /// <param name="nozzleNo">要取料的吸嘴序号（1234）</param>
        /// <param name="first">是否为第一个要取料的吸嘴</param>
        /// <param name="last">是否为最后一个要取料的吸嘴</param>
        /// <returns></returns>
        private (bool, EN_RunRet, EN_RunStep) GetOneTape(int nozzleNo, bool first, bool last, CarrierStatus carrierStatus, float[] floats = null,bool isTwoTape = false)
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            //盲取提前到位，视觉引导不需要
            if (floats == null)
            {
                //等待物料到位
                if (!_stepStatus.TrigFeederConveyTape(true))
                {
                    return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                }
            }
            else
            {
                //提前取&平台无料情况下，如果启用偶数个吸嘴，取料来了两个料，应该先抛一个，再取
                if (!_plc_Component.IsSimulateRun()
               && Enable.PickOpportunity != EN_PickOpportunity.AfterUpCam
               && Enable.KeepNoTapeOnFeeder
               && _stepStatus.GetEnableNozzleCount() % 2 == 0
               && isTwoTape == true
               && _stepStatus.PickTapeOk[0] == true)
                {
                    int throwTapeNozzleNo = 0;
                    if (!_mGoogol_Component.GetMaterialsReady(out bool[] sucStatus) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                    {
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->获取二个吸嘴吸准备信号失败(Lấy được 4 ống hút. Tín hiệu chuẩn bị thất bại.)", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                    //遍历，清除第一个吸到料的吸嘴的物料
                    for (int nozzleNo0 = 1; nozzleNo0 <= 2; nozzleNo0++)
                    {
                        if (_stepStatus.UseNozzle(nozzleNo0) && sucStatus[nozzleNo0 - 1])
                        {
                            throwTapeNozzleNo = nozzleNo0;
                            break;
                        }
                    }
                    if (throwTapeNozzleNo != 0)
                    {
                        if (!_stepStatus.ThrowTapes(new HashSet<int> { throwTapeNozzleNo }, true, false, EN_TossingCode.BreakVacuum, carrierStatus, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->清除吸嘴多余物料失败(Loại bỏ vật liệu thừa khỏi miệng hút thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        return (false, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
                    }
                }
            }
            //提前取&平台无料情况下，如果启用偶数个吸嘴，且最后一次取料来了两个料，应该先抛一个，再取
            //if (!_plc_Component.IsSimulateRun()
            //    && Enable.PickOpportunity != EN_PickOpportunity.AfterUpCam
            //    && Enable.KeepNoTapeOnFeeder
            //    && _stepStatus.GetEnableNozzleCount() % 2 == 0
            //    && last
            //    && _plc_Component.IsFeederTapeReady(0) && _plc_Component.IsFeederTapeReady(1)
            //    /*&& _stepStatus.isEndCarrierFinish*/)
            //等待移动到位,如果是第一个吸嘴，需要把所有R移动到位
            if (!MoveToFeederSingleTape(nozzleNo, true, first, floats).Item1)
            {
                return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
            }
            //气缸下
            while (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸下降失败(Thiết lập xi lanh rơi thất bại)", En_Logout_Type.Alarm, true);
                AlarmInfoModel alarm;
                if (nozzleNo == 1)
                {
                    alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, "设置吸嘴气缸1下降失败(Thiết lập xi lanh rơi thất bại)", 64119);
                }
                else
                {
                    alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, "设置吸嘴气缸2下降失败(Thiết lập xi lanh rơi thất bại)", 64121);
                }
                if (MessageBox.Show("设置气缸下降失败，是否重新下降气缸？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    _baseBiz.RemoveRunAlarm(alarm);
                    continue;
                }
                else
                {
                    _baseBiz.RemoveRunAlarm(alarm);
                    return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                }
            }
            //气缸下成功则使用次数+1
            if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
            {
                _cacheParamManager.HomeUiParam.Statistic.AddUseCount(nozzleNo);
            }
            else
            {
                _cacheParamManager.HomeUiParam.Statistic.AddUseDebugCount(nozzleNo);
            }

            if (_plc_Component.IsSimulateRun())
            {
                //空跑不需要取料，开吹
                if (!_mGoogol_Component.SetVacuum(nozzleNo, false, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸开吹失败(Thiết lập xi lanh thổi không thành công)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
            }
            else
            {
                //开吸
                if (!_mGoogol_Component.SetVacuum(nozzleNo, true, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸开吸失败(Thiết lập xi lanh để thở thất bại)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
            }

            #region Feeder吹气
            ////等待一定时间
            //Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
            ////飞达开吹（根据x轴当前位置，确定现在取的是左边还是右边。注意点位使用视觉引导，所以误差定在±5）
            //bool left = nozzleNo == 1
            //    ? Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[0]) < 5
            //    : Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos[0]) < 5;
            //if (_stepStatus.CacheParamManager.HomeUiParam.Enable.FeederId == FeederId.飞达2)
            //{
            //    left = nozzleNo == 1
            //    ? Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[0]) < 5
            //    : Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos[0]) < 5;
            //}
            //if (!_mGoogol_Component.SetFeederVacuum(left, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            //{
            //    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
            //    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
            //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置飞达吹气开失败", En_Logout_Type.Alarm, true);
            //    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            //}
            ////等待一定时间
            //Thread.Sleep(_paramManager.OtherSettingParam.GetTapeFeederBreakTime);
            ////飞达关吹
            //if (!_mGoogol_Component.SetFeederVacuum(left, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            //{
            //    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
            //    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
            //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置飞达吹气关失败", En_Logout_Type.Alarm, true);
            //    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            //}
            #endregion


            Thread.Sleep(_paramManager.OtherSettingParam.GetTapeWaitTime);

            //气缸上到位
            if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸上抬失败(Thiết lập xi lanh nâng thất bại)", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            if (_plc_Component.IsSimulateRun())
            {
                //空跑关吹
                if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸关吹失败(Đóng xi lanh thổi không thành công)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
            }
            else
            {
                //检测真空吸状态，判断是否取到物料
                if (_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                {
                    if (!_mGoogol_Component.GetMaterialReady(nozzleNo))
                    {
                        //关吸开吹，防止吸嘴上有料但是真空吸判定为没有的情况
                        if (!_mGoogol_Component.SetVacuum(nozzleNo, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸关吸开吹失败(Xi lanh tắt, không thở được.)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //等待抛料延时
                        Thread.Sleep(_paramManager.OtherSettingParam.OpenBreakTime);
                        //关吹
                        if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸关吹失败(Xi lanh tắt, không thở được.)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //上抬Z2
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->运动到Z2 0失败(Thể thao đến Z2 thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //气缸上到位
                        if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸上抬失败(Xi lanh tăng thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                        {
                            _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddSuckNgCount(nozzleNo);
                        }
                        else
                        {
                            _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddSuckNgDebugCount(nozzleNo);
                        }

                        //报警，需要人工处理
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", En_Logout_Type.Alarm, true);
                        //根据吸嘴号添加相应的报警
                        var alarm = nozzleNo == 1?_baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", 106):
                            _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", 107);
                        if (MessageBox.Show($"{nozzleNo}号吸嘴取料失败，需要人工清掉不粘板上所有tape！\n确定处理完毕继续吗？(Lấy nguyên liệu từ miệng hút thất bại, cần nhân công rửa sạch ống hút phía dưới! Đã xử lý xong, tiếp tục?)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            _baseBiz.RemoveRunAlarm(alarm);
                            return (false, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
                        }
                        else
                        {
                            _baseBiz.RemoveRunAlarm(alarm);
                            return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                        }
                    }
                }
            }
            _stepStatus.PickTapeOk[nozzleNo - 1] = true;
            //不是空跑时，tape使用次数+1
            if (!_plc_Component.IsSimulateRun() && (_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle))
            {
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                string tapeSn = feederId == FeederId.左飞达?_paramManager.MESParam.Feeder1TapeSN: _paramManager.MESParam.Feeder2TapeSN;
                _cacheParamManager.HomeUiParam.TapeUseInfoParam.AddCountBySn(tapeSn);
            }
            return (true, EN_RunRet.MotionOk, EN_RunStep.PickMaterialFromFeederStep);
        }

        /// <summary>
        /// 移动指定吸嘴到有料的位置
        /// </summary>
        /// <param name="is13">要取料的吸嘴，true表示1号3号，false表示2号4号</param>
        /// <param name="waitXYZRREnd"></param>
        /// <param name="moveAllR">如果是第一次取料，需要转动所有R</param>
        /// <returns></returns>
        private (bool, EN_RunRet, EN_RunStep) MoveToFeederDoubleTape(bool is13, bool waitXYZRREnd, bool moveAllR, float[] floats)
        {
            float[] feederPos = _stepStatus.FeederDoubleTapePos(is13);
            //防呆，判断有没有设定取料位
            if (feederPos[0] < 20)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->取料位X坐标{feederPos[0]}小于20，可能未设置取料位(Vị trí lấy tọa độ X nhỏ hơn 20 và vị trí lấy có thể không được đặt)", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            if (moveAllR)
            {
                //所有启用的吸嘴转到取料角度，防止与其他吸嘴碰到，不用等待结束
                for (int nozzleNo0 = 1; nozzleNo0 <= 2; nozzleNo0++)
                {
                    //if ((is13 && nozzleNo0 <= 2) || (!is13 && nozzleNo0 > 2))
                    //{
                    //    continue;
                    //}
                    if (_stepStatus.UseNozzle(nozzleNo0))
                    {
                        float feederPosR = _stepStatus.FeederSingleTapePos(nozzleNo0, 0)[3];
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis((En_AxisNum)nozzleNo0 + 4, feederPosR, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->运动到取料吸嘴R失败(Tập thể dục để lấy vòi hút R thất bại)：[R{nozzleNo0}]{feederPosR.ToString("f3")}", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                }
            }
            //飞达高度Z是安全的高度，所以直接移动XYZR就行
            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2RAll(feederPos, waitXYZRREnd) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->运动到{(is13 ? "1、2" : "3、4")}号吸嘴 Feeder Pick 坐标失败(Chuyển động đến miệng hút lấy tọa độ thất bại)，Axis[X2,Y2]:{NLogTrace.GetFloatArrayString(feederPos)}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            return (true, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
        }

        /// <summary>
        /// 让指定的两个吸嘴吸取两个tape
        /// </summary>
        /// <param name="is13">要取料的吸嘴，true表示1号3号，false表示2号4号</param>
        /// <param name="first">是否为第一个要取料的吸嘴</param>
        /// <param name="last">是否为最后一个要取料的吸嘴</param>
        /// <returns></returns>
        private (bool, EN_RunRet, EN_RunStep) GetTwoTapes(bool is13, bool first, bool last, float[] floats = null)
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            int nozzle1No = is13 ? 1 : 2;
            int nozzle2No = is13 ? 3 : 4;
            //等待移动到位
            if (!MoveToFeederDoubleTape(is13, true, first, floats).Item1)
            {
                return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
            }
            //气缸下
            while (!_mGoogol_Component.SetTwoCylindersUpDown(is13, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸下降失败(Thiết lập xi lanh rơi thất bại)", En_Logout_Type.Alarm, true);
                var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, "设置气缸下降失败(Thiết lập xi lanh rơi thất bại)", 64119);
                if (MessageBox.Show("设置气缸下降失败，是否重新下降气缸？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    _baseBiz.RemoveRunAlarm(alarm);
                    continue;
                }
                else
                {
                    _baseBiz.RemoveRunAlarm(alarm);
                    return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                }
            }
            //气缸下成功则使用次数+1
            if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
            {
                _cacheParamManager.HomeUiParam.Statistic.AddUseCount(nozzle1No);
                _cacheParamManager.HomeUiParam.Statistic.AddUseCount(nozzle2No);

            }
            else
            {
                _cacheParamManager.HomeUiParam.Statistic.AddUseDebugCount(nozzle1No);
                _cacheParamManager.HomeUiParam.Statistic.AddUseDebugCount(nozzle2No);
            }

            if (_plc_Component.IsSimulateRun())
            {
                //空跑不需要取料，开吹
                if (!_mGoogol_Component.SetVacuum(nozzle1No, false, true) || !_mGoogol_Component.SetVacuum(nozzle2No, false, true)
                    || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸开吹失败(Xi lanh thổi không thành công)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
            }
            else
            {
                //开吸
                if (!_mGoogol_Component.SetVacuum(nozzle1No, true, true) || !_mGoogol_Component.SetVacuum(nozzle2No, true, true)
                     || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸开吸失败(Xi lanh khí thất bại)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
            }
            //等待一定时间
            Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
            //飞达吹气开
            if (!_mGoogol_Component.SetFeederVacuum(true, true) || !_mGoogol_Component.SetFeederVacuum(false, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置飞达吹气开失败", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            //等待一定时间
            Thread.Sleep(_paramManager.OtherSettingParam.GetTapeFeederBreakTime);
            //飞达关吹
            if (!_mGoogol_Component.SetFeederVacuum(true, false) || !_mGoogol_Component.SetFeederVacuum(false, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置飞达吹气关失败", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            //气缸上到位
            if (!_mGoogol_Component.SetTwoCylindersUpDown(is13, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸上抬失败(Xi lanh tăng thất bại)", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            if (_plc_Component.IsSimulateRun())
            {
                //空跑关吹
                if (!_mGoogol_Component.SetVacuum(nozzle1No, false, false) || !_mGoogol_Component.SetVacuum(nozzle2No, false, false)
                    || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                {
                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸关吹失败(Đóng xi lanh thổi không thành công)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
            }
            else
            {
                //检测真空吸状态，判断是否取到物料
                if (_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                {
                    int[] nozzleNoArr = new[] { nozzle1No, nozzle2No };
                    bool processed = false;
                    for (int i = 0; i < 2; i++)
                    {
                        int nozzleNo = nozzleNoArr[i];
                        if (!_mGoogol_Component.GetMaterialReady(nozzleNo))
                        {
                            //关吸开吹，防止吸嘴上有料但是真空吸判定为没有的情况
                            if (!_mGoogol_Component.SetVacuum(nozzleNo, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸关吸开吹失败(Xi lanh tắt, không thở được.)", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                            //等待抛料延时
                            Thread.Sleep(_paramManager.OtherSettingParam.OpenBreakTime);
                            //关吹
                            if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸关吹失败(Xi lanh tắt, không thở được.)", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                            //上抬Z2
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->运动到Z2 0失败(Thể thao đến Z2 thất bại)", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                            //气缸上到位
                            if (!_mGoogol_Component.SetTwoCylindersUpDown(is13, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置气缸上抬失败(Xi lanh tăng thất bại)", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                            if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                            {
                                _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddSuckNgCount(nozzleNo);
                            }
                            else
                            {
                                _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddSuckNgDebugCount(nozzleNo);
                            }

                            //报警，需要人工处理
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->同取时{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", En_Logout_Type.Alarm, true);
                            //根据吸嘴号添加相应的报警
                            var alarm = nozzleNo == 1 ? _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", 106) :
                                _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", 107);
                            if (MessageBox.Show($"同取时{nozzleNo}号吸嘴取料失败，需要人工清掉不粘板上所有tape！\n确定处理完毕继续吗？(Lấy nguyên liệu từ miệng hút thất bại, cần nhân công rửa sạch nguyên liệu phía dưới miệng hút! Đã xử lý xong, tiếp tục?)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                processed = true;
                            }
                            else
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                            }
                        }
                    }
                    if (processed)
                    {
                        return GetTwoTapes(is13, first, last);
                    }
                }
            }
            _stepStatus.PickTapeOk[nozzle1No - 1] = true;
            _stepStatus.PickTapeOk[nozzle2No - 1] = true;
            //不是空跑时，tape使用次数+2
            if (!_plc_Component.IsSimulateRun() && (_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle))
            {
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                string tapeSn = feederId == FeederId.左飞达 ? _paramManager.MESParam.Feeder1TapeSN : _paramManager.MESParam.Feeder2TapeSN;
                _cacheParamManager.HomeUiParam.TapeUseInfoParam.AddCountBySn(tapeSn);
                _cacheParamManager.HomeUiParam.TapeUseInfoParam.AddCountBySn(tapeSn);
            }
            return (true, EN_RunRet.MotionOk, EN_RunStep.PickMaterialFromFeederStep);
        }

    }
}
