using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.Camera;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Component.Scanner;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Model.Alarm;
using QA.Business.Station;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Status.GantryNo1Steps
{
    public class UpCameraIdentifyProductStep : IStepStation1
    {
        #region Field
        private StepStatus _stepStatus;
        private MotionGoogol_Component _mGoogol_Component;
        private Camera_Component _camera_Component;
        private Scanner_TcpComponent _scanner_Component;
        private MES_Component _mes_Component;
        private PLC_Component _plc_Component;
        private Hive_Component _hive_Component;
        private IEventAggregator _eventAggregator;
        private GlobalVariable _globalVariable;
        private CameraParam _cameraParam = null;
        private CacheParamManager _cacheParamManager;
        #endregion

        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.UpCameraIdentifyProductStep;

        private HomeUiParam_Enable Enable { get => _stepStatus.CacheParamManager.HomeUiParam.Enable; }

        public UpCameraIdentifyProductStep()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _scanner_Component = (Scanner_TcpComponent)IoC.Get<IScanner>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _mes_Component = (MES_Component)IoC.Get<IMES>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _cameraParam = IoC.Get<CameraParam>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {

            await Task.Delay(10);
            //生产流程
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            //等待贴合完毕
            if (!_stepStatus.GetMoveMutex())
            {
                _stepStatus.AutoResetEvt_CtrlVisual.WaitOne();
            }
            while (!_plc_Component.IsCarrierReady() || !_stepStatus.GetMoveMutex() || !_mGoogol_Component.Station2InSafeArea())
            {
                if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep1);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                await Task.Delay(200);
            }
            if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep1);
            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
            CarrierStatus carrierStatus = _stepStatus.GetCurCarrier();
            if (carrierStatus == null)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->未能获取载具信息(Không thể lấy thông tin về tàu sân bay)", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            StringBuilder sb = new StringBuilder();
            foreach (var point in _stepStatus.CurrentProcedure.VisionPoints)
            {
                //提前判断穴位启用状态，并写入carrier信息中
                if (!point.IsUsed)
                {
                    carrierStatus.isEmpty[point.CavityNum - 1] = 1;
                }
                sb.Append(point.isUpCamFinished.ToString()).Append(",");
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->开始时isUpCamFinished：{sb.ToString().TrimEnd(',')}");
            bool openedUpLightHX = false;
            bool openedUpLightTZ = false;
            //启用吸嘴总数，也是需要的上视觉成功总数
            int enableNozzleCount = _stepStatus.GetEnableNozzleCount();
            //对需要进行处理、且未处理过的穴位拍照，直至数目达到启用吸嘴的数目，或所有点位处理完
            //视觉全部ok的，添加到_stepStatus.CurNeedProcess里面；应该保压的，添加到_stepStatus.CurNeedDwell里面
            int upCamProcessCount = 0;
            foreach (var point in _stepStatus.CurrentProcedure.VisionPoints)
            {
                if (Enable.PickOpportunity != EN_PickOpportunity.AfterUpCam
                    && upCamProcessCount % enableNozzleCount == 0
                    && _stepStatus.CurNeedProcess.Count > 0)
                {
                    //如果是固定贴模式且已经有需要贴的
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->固定贴模式且已经有需要贴的，终止上视觉循环");
                    break;
                }
                upCamProcessCount++;
                if (point.isUpCamFinished)
                {
                    //如果穴位已经上视觉处理过
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->穴位已经上视觉处理过，跳过{point.CavityNum}穴");
                    continue;
                }
                if (!point.IsUsed)
                {
                    //如果穴位不启用
                    carrierStatus.cavStateStr[point.CavityNum - 1] = "穴位禁用";
                    point.isUpCamFinished = true;
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->穴位禁用，跳过{point.CavityNum}穴");
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->{point.CavityNum}穴isUpCamFinished->true");
                    continue;
                }
                if (_stepStatus.CurNeedProcess.Count >= enableNozzleCount)
                {
                    //已经取够了需要的视觉个数
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->当前取料个数{_stepStatus.CurNeedProcess.Count}已达到所需个数{enableNozzleCount}，终止上视觉循环");
                    break;
                }
                //拍全图，开启会先拍x-15存图
                if (_camera_Component.IsSaveAllPhotos() == true)
                {
                    //移动到拍照点X-15
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { point.VisionPointX -15, point.VisionPointY }, true))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->运行到穴位：{point.CavityNum} 的视觉点失败(Chuyển động đến điểm nhìn thất bại)：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { point.VisionPointX, point.VisionPointY })}", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                    if (!_camera_Component.SaveAllPhoto(carrierStatus.carrierSN, point.CavityNum, 1))
                    {
                        if (!_camera_Component.SaveAllPhoto(carrierStatus.carrierSN, point.CavityNum, 1))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"S1存全图拍照失败，穴位号：{point.CavityNum}，拍照次数：1", En_Logout_Type.Alarm, true);
                        }
                    }

                }
                //移动到拍照点
                if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { point.VisionPointX, point.VisionPointY }, true))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->运行到穴位：{point.CavityNum} 的视觉点失败(Chuyển động đến điểm nhìn thất bại)：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { point.VisionPointX, point.VisionPointY })}", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.MotionErr, _stepStatus.NextStep1);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                //空跑处理
                if (_plc_Component.IsSimulateRun())
                {
                    //模拟延时
                    await Task.Delay(300);
                    _stepStatus.CurNeedProcess.Add(point);
                    _stepStatus.CurNeedDwell.Add(point);
                    point.isUpCamFinished = true;
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->空跑，跳过{point.CavityNum}穴");
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->{point.CavityNum}穴isUpCamFinished->true");
                    continue;
                }
                //拍照前延时
                await Task.Delay(_stepStatus.ParamManager.CameraParam.UpCamWaitTime);
                if (_camera_Component.IsSaveAllPhotos() == true)
                {
                    if (!_camera_Component.SaveAllPhoto(carrierStatus.carrierSN, point.CavityNum, 2))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"S1存全图拍照失败，穴位号：{point.CavityNum}，拍照次数：2", En_Logout_Type.Alarm, true);
                    }

                }

            //贴合定位（检测mark）；失败时直接跳转至下一穴位
            UPCCDNG:
                int errcode = 0;
                if (!_camera_Component.UpCdd(point.CavityNum, point.VisionPointX, point.VisionPointY, 0, carrierStatus.carrierSN, carrierStatus.sipSN[point.CavityNum - 1], ref errcode))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"UpCam->定位{point.CavityNum}穴贴装目标位置失败", En_Logout_Type.Run, true);
                    carrierStatus.errorCode[point.CavityNum - 1] = (int)errcode;
                    carrierStatus.cavStateStr[point.CavityNum - 1] = "上视觉定位NG";
                    PublishCavityStateMsg(point.CavityNum, (EN_TrayStatus)errcode);
                    point.isUpCamFinished = true;
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->{point.CavityNum}穴isUpCamFinished->true");
                    if ((EN_TrayStatus)errcode != EN_TrayStatus.Empty && (EN_TrayStatus)errcode != EN_TrayStatus.HaveTape)
                    {
                        _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddCamNgCount(3);
                    }
                    //判断是否为空穴，将信息写入Tary穴位文件中
                    if ((EN_TrayStatus)errcode == EN_TrayStatus.Empty)
                    {
                        carrierStatus.isEmpty[point.CavityNum - 1] = 1;
                    }
                    //SaveCamdata(carrierStatus, point.CavityNum, _camera_Component.downCDDMode.ResPosConvert(), false);//存储上视觉NG的定位信息
                    //弹窗提示是否是空穴（只有生产情况下执行）
                    //if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle))
                    //{
                    //    //空穴的情况下不需要提示（只有存在SIP板的穴位需要弹框提示）
                    //    if (carrierStatus.sipSN[point.CavityNum - 1] != "")
                    //    {
                    //        //定位数据大于999.999就是定位到空穴（此时当前穴位是绑定有SIP实际定位没有载具需要弹框提示）
                    //        if (_camera_Component.downCDDMode.ResPosConvert()[0] > 999)
                    //        {
                    //            var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Camera, "上视觉定位为空穴", 121);
                    //            if (MessageBox.Show($"请查看是否穴位是空穴", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    //            {
                    //                _baseBiz.RemoveRunAlarm(alarm);
                    //            }
                    //            else
                    //            {
                    //                _baseBiz.RemoveRunAlarm(alarm);
                    //                goto UPCCDNG;
                    //            }
                    //        }
                    //        //if (!CavityisEmpty()) goto UPCCDNG;
                    //    }
                    //}
                    if (_camera_Component.IsSaveAllPhotos() == true)
                    {
                        //移动到拍照点X+15
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { point.VisionPointX + 15, point.VisionPointY }, true))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->运行到穴位：{point.CavityNum} 的视觉点失败(Chuyển động đến điểm nhìn thất bại)：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { point.VisionPointX, point.VisionPointY })}", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        if (!_camera_Component.SaveAllPhoto(carrierStatus.carrierSN, point.CavityNum, 3))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"S1存全图拍照失败，穴位号：{point.CavityNum}，拍照次数：3", En_Logout_Type.Alarm, true);
                        }

                    }
                    continue;
                }
                NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"UpCam->定位{point.CavityNum}穴贴装目标位置成功", En_Logout_Type.Run, true);
                if (_camera_Component.IsSaveAllPhotos() == true)
                {
                    //移动到拍照点X+15
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { point.VisionPointX + 15, point.VisionPointY }, true))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->运行到穴位：{point.CavityNum} 的视觉点失败(Chuyển động đến điểm nhìn thất bại)：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { point.VisionPointX, point.VisionPointY })}", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                    if (!_camera_Component.SaveAllPhoto(carrierStatus.carrierSN, point.CavityNum, 3))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"S1存全图拍照失败，穴位号：{point.CavityNum}，拍照次数：3", En_Logout_Type.Alarm, true);
                    }

                }
                if (string.IsNullOrEmpty(carrierStatus.sipSN[point.CavityNum - 1]))
                {
                    //定位成功应必然有sipsn，即使调试模式也应该为default，所以此处为异常情况
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->未找到{point.CavityNum}穴SipSN，需检查载具MES状态", En_Logout_Type.Run, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }
                carrierStatus.errorCode[point.CavityNum - 1] = (int)EN_TrayStatus.OK;
                carrierStatus.cavStateStr[point.CavityNum - 1] = "上视觉定位OK";
                //存储上视觉定位信息
                string s1 = "";
                for (int i = 0; i < _camera_Component.downCDDMode.ResPosConvert().Length; i++)
                {
                    s1 += _camera_Component.downCDDMode.ResPosConvert()[i] + ",";
                }
                carrierStatus.markToPhotoCenterOffsets[point.CavityNum - 1] = s1.Substring(0, s1.Length - 1);

                if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.MotionErr, _stepStatus.NextStep1);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);


                if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                {
                GetTapeSN:
                    //检查飞达是否锁紧
                    if (!_plc_Component.IsFeederLocked())
                    {
                        FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                        AlarmInfoModel alarm;
                        if (feederId == FeederId.左飞达)
                        {
                            alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"左飞达未锁紧", 104);
                        }
                        else
                        {
                            alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"右飞达未锁紧", 105);
                        }
                        var result = MessageBox.Show($"飞达未锁紧！\n确定重新检测吗？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        _baseBiz.RemoveRunAlarm(alarm);
                        if (result == MessageBoxResult.OK)
                        {
                            goto GetTapeSN;
                        }
                        else
                        {
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                    //检查B2BTapeSN是否合规
                    if (!_mes_Component.CheckTapeSNOk(out string info))
                    {
                        //var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"未通过卷料SN检查", 111);
                        var result = MessageBox.Show($"未通过卷料SN检查，{info}，需要重新录入卷料SN！\n确定重新检测吗？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        //_baseBiz.RemoveRunAlarm(alarm);
                        if (result == MessageBoxResult.OK)
                        {
                            goto GetTapeSN;
                        }
                        else
                        {
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                    }
                    //检查B2BTapeSN是否为调机料
                    carrierStatus.cavStateStr[point.CavityNum - 1] = "TapeSN已获取";
                    var routingResult = carrierStatus.routingResults[point.CavityNum - 1];
                }
                point.isUpCamFinished = true;
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->{point.CavityNum}穴isUpCamFinished->true");
                carrierStatus.cavStateStr[point.CavityNum - 1] = "上视觉全部OK";
                //只有视觉全部OK的才会存进来
                _stepStatus.CurNeedProcess.Add(point);
                _stepStatus.CurNeedDwell.Add(point);
            }
            sb = new StringBuilder();
            foreach (var point in _stepStatus.CurrentProcedure.VisionPoints)
            {
                sb.Append(point.isUpCamFinished.ToString()).Append(",");
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->结束时isUpCamFinished：{sb.ToString().TrimEnd(',')}");
            //判断当前第几穴定位OK
            foreach (var point in _stepStatus.CurrentProcedure.VisionPoints)
            {
                if (point.isUpCamFinished)
                {
                    _stepStatus.CurrentCavNum = point.CavityNum;
                }
            }
            //判断载具是否还有穴位需要贴合，没有则直接放行
            if (_stepStatus.IsAllUpCamFinished() && _stepStatus.CurNeedProcess.Count == 0)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->上视觉定位完毕，放行载具", En_Logout_Type.Run, true);
                _stepStatus.isEndCarrierFinish = true;
                _stepStatus.isCavityNull = 0;
                //直接将载具放行
                if (!_stepStatus.CarrierFinish(carrierStatus))
                {
                    return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                }
                //注意，此时轴移动权仍在上相机轴
                //_stepStatus.isUPCamOk = false;
                return (false, EN_RunRet.TaskOk, EN_RunStep.ScannerCarrierSnStep);
            }
            else
            {
                #region 相机轴避让
                //使用固定吸嘴的点位进行避让，这个点位肯定设置过
                if (_stepStatus.CurNeedProcess.Count > 0)
                {
                        //不必等待轴移动结束，因为后续吸嘴轴也会让相机轴避让
                        if (!_mGoogol_Component.SafeAvoid())
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->触发防撞系统，运动失败(Kích hoạt hệ thống chống va chạm, chuyển động thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                }
                #endregion

                //重要代码，危险代码，互锁操作，不能随便改
                //将轴移动权转交给吸嘴轴
                _stepStatus.SetMoveMutex(false);
                _stepStatus.ManualResetEvt_CtrlPlace.Set();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->上视觉定位完毕，继续贴合线程", En_Logout_Type.Run, true);
                return (false, EN_RunRet.TaskOk, EN_RunStep.UpCameraIdentifyProductStep);
            }
        }

        private void PublishCavityStateMsg(int cavity, EN_TrayStatus status)
        {
            _eventAggregator.Publish(new CarrierInfoPanelMessage() { Cavity = cavity, Status = status, }, action => { Task.Run(action); });
        }




        ///// <summary>
        ///// 上视觉SIP板防呆检测（是否定位数据超过标准值）
        ///// </summary>
        ///// <param name="xya"></param>
        ///// <param name="index"></param>
        ///// <returns></returns>
        //private bool UpCDDfoolproof(float[] xya, int index)
        //{
        //    float[] MarkXs = new[]
        //             {
        //               _stepStatus.ParamManager.CameraParam.MarkX1,
        //               _stepStatus.ParamManager.CameraParam.MarkX2,
        //               _stepStatus.ParamManager.CameraParam.MarkX3,
        //               _stepStatus.ParamManager.CameraParam.MarkX4,
        //               _stepStatus.ParamManager.CameraParam.MarkX5,
        //               _stepStatus.ParamManager.CameraParam.MarkX6,
        //               _stepStatus.ParamManager.CameraParam.MarkX7,
        //               _stepStatus.ParamManager.CameraParam.MarkX8,
        //               _stepStatus.ParamManager.CameraParam.MarkX9,
        //               _stepStatus.ParamManager.CameraParam.MarkX10,
        //               _stepStatus.ParamManager.CameraParam.MarkX11,
        //               _stepStatus.ParamManager.CameraParam.MarkX12,
        //            };

        //    float[] MarkposXs = new[]
        //             {
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //               _stepStatus.ParamManager.CameraParam.MarkposX,
        //            };
        //    float[] MarkYs = new[]
        //              {
        //               _stepStatus.ParamManager.CameraParam.MarkY1,
        //               _stepStatus.ParamManager.CameraParam.MarkY2,
        //               _stepStatus.ParamManager.CameraParam.MarkY3,
        //               _stepStatus.ParamManager.CameraParam.MarkY4,
        //               _stepStatus.ParamManager.CameraParam.MarkY5,
        //               _stepStatus.ParamManager.CameraParam.MarkY6,
        //               _stepStatus.ParamManager.CameraParam.MarkY7,
        //               _stepStatus.ParamManager.CameraParam.MarkY8,
        //               _stepStatus.ParamManager.CameraParam.MarkY9,
        //               _stepStatus.ParamManager.CameraParam.MarkY10,
        //               _stepStatus.ParamManager.CameraParam.MarkY11,
        //               _stepStatus.ParamManager.CameraParam.MarkY12,
        //            };
        //    float[] MarkposYs = new[]
        //              {
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //               _stepStatus.ParamManager.CameraParam.MarkposY,
        //              };
        //    float[] MarkAs = new[]
        //            {
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //               _stepStatus.ParamManager.CameraParam.MarkA,
        //            };
        //    float[] MarkposAs = new[]
        //           {
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //              _stepStatus.ParamManager.CameraParam.MarkposA,
        //           };

        //    if (!(MarkXs[index] + MarkposXs[index] > xya[0] && MarkXs[index] - MarkposXs[index] < xya[0]))
        //    {
        //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->定位防呆{index + 1}穴X超限：[基准值：{MarkXs[index]},偏移量±:{MarkposXs[index]},X值：{xya[0]}]", En_Logout_Type.Alarm, true);
        //        return false;
        //    }
        //    if (!(MarkYs[index] + MarkposYs[index] > xya[1] && MarkYs[index] - MarkposYs[index] < xya[1]))
        //    {
        //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->定位防呆{index + 1}穴Y超限：[基准值：{MarkYs[index]},偏移量±:{MarkposYs[index]},Y值：{xya[1]}]", En_Logout_Type.Alarm, true);
        //        return false;
        //    }
        //    if (!(MarkAs[index] + MarkposAs[index] > xya[2] && MarkAs[index] - MarkposAs[index] < xya[2]))
        //    {
        //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->定位防呆{index + 1}穴A超限：[基准值：{MarkAs[index]},偏移量±:{MarkposAs[index]},A值：{xya[2]}]", En_Logout_Type.Alarm, true);
        //        return false;
        //    }
        //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"UpCam->定位防呆{index + 1}穴：[基准值：{MarkXs[index]},{MarkYs[index]},{MarkAs[index]},偏移量±:{MarkposXs[index]},{MarkposYs[index]},{MarkposAs[index]}值：{xya[0]},{xya[1]},{xya[2]}]", En_Logout_Type.Run, true);
        //    return true;
        //}

        /// <summary>
        /// 上视觉定位NG弹框提示
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool UPCDDNG(string msg)
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Camera, msg, 110);
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
        /// 保存上相机定位NG数据
        /// </summary>
        /// <param name="carrierStatus"></param>
        /// <param name="CavityNum"></param>
        /// <param name="partxya"></param>
        private void SaveCamdata(CarrierStatus carrierStatus, int CavityNum, float[] partxya, bool isUpCDDfoolproof)
        {
            try
            {
                string s = "";
                for (int i = 0; i < partxya.Length; i++)
                {
                    s += partxya[i] + ",";
                }
                _stepStatus.markToPhotoCenterOffsets[CavityNum - 1] = s.Substring(0, s.Length - 1);
                if (!isUpCDDfoolproof) carrierStatus.cavStateStr[CavityNum] = "上视觉定位NG";
                else carrierStatus.cavStateStr[CavityNum] = "上视觉定位卡控NG";
                carrierStatus.Tape_Nozzle[CavityNum - 1] = _stepStatus.CurrentNozeNum + 1;
                carrierStatus.markToPhotoCenterOffsets[CavityNum - 1] = _stepStatus.markToPhotoCenterOffsets[CavityNum - 1];
                _stepStatus.SaveToCsv(carrierStatus, true, CavityNum - 1);
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->存储定位NG信息数据异常", En_Logout_Type.Alarm, true);
            }
        }

        /// <summary>
        /// 穴位是否是空穴弹框提示
        /// </summary>
        /// <returns></returns>
        private bool CavityisEmpty()
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            //载具空穴数量判断，如果连续2次空穴就判断整盘载具是复投载具，此载具后续穴位不在进行空穴弹框提示
            if (_stepStatus.isCavityNull < 2)
            {
                _stepStatus.isCavityNull++;
                var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Camera, "上视觉定位为空穴", 0);
                if (MessageBox.Show($"请查看是否穴位是空穴", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
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

            return true;
        }


        private void SaveCarrierBadInfo(CarrierStatus carrierStatus, int cavityNum)
        {
            string dir = $@"D:\QKProject\Data\Other\CarrierNG";
            //string dir = $@"D:\CsvData\产品信息\UPCam\CarrierNG";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string file = $@"{dir}\{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
            bool exists = File.Exists(file);
            try
            {
                using (StreamWriter sw = new StreamWriter(file, true, Encoding.Default))
                {
                    if (!exists)
                    {
                        //载具时间（年月日） 载具时间（时分秒） 载具码 穴位号  
                        sw.WriteLine("载具时间年月日,载具时间时分秒,载具码,穴位号");
                    }

                    //时间、载具码、穴位号、吸嘴号、信息
                    sw.WriteLine(carrierStatus.scanTime.ToString("yyyy/MM/dd") + "," + carrierStatus.scanTime.ToString("HH:mm:ss") + "," + carrierStatus.carrierSN + "," + cavityNum);
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.Message, En_Logout_Type.Exception, true);
            }
        }


    }
}
