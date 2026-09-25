using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Component.Scanner;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Station;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Status.GantryNo1Steps
{
    public class ScannerCarrierSnStep : IStepStation1
    {
        #region Field    
        private StepStatus _stepStatus;
        private MotionGoogol_Component _mGoogol_Component;
        private Scanner_TcpComponent _scanner_Component;
        private PLC_Component _plc_Component;
        private Hive_Component _hive_Component;
        private MES_Component _mes_Component;
        private GlobalVariable _globalVariable;
        private IEventAggregator _eventAggregator;
        private CacheParamManager _cacheParamManager;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.ScannerCarrierSnStep;
        private HomeUiParam_Enable Enable { get => _stepStatus.CacheParamManager.HomeUiParam.Enable; }
        #endregion

        #region Constructor
        public ScannerCarrierSnStep()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _scanner_Component = (Scanner_TcpComponent)IoC.Get<IScanner>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _mes_Component = (MES_Component)IoC.Get<IMES>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            //相机只定位采集数据
            if (Enable.IsCDDCheck)
            {
                return (false, EN_RunRet.TaskOk, EN_RunStep.UpCameraIdentifyProductStep);
            }



            ///生产流程
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            //等待上一板载具放行后解锁AutoResetEvt_CtrlScanner，相机获取轴锁，且吸嘴轴Y小于安全距离
            _stepStatus.AutoResetEvt_CtrlScanner.WaitOne();
            while (!_stepStatus.GetMoveMutex() || !_mGoogol_Component.Station2InSafeArea())
            {
                if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.MotionErr, _stepStatus.NextStep1);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                await Task.Delay(200);
            }
            if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.MotionErr, _stepStatus.NextStep1);
            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
            //提前移动到扫码位，不用等待移动完成
            var carrierPos = _stepStatus.CacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos;
            if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { carrierPos[0], carrierPos[1] }, false))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Scan->提前移动到扫描载具码位置失败(Lỗi vị trí quét chuyển động sang tàu sân bay)：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { carrierPos[0], carrierPos[1] })}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            //等待载具到位信号
            while (!_plc_Component.IsCarrierReady())
            {
                if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.MotionErr, _stepStatus.NextStep1);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                await Task.Delay(200);
            }
            if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.MotionErr, _stepStatus.NextStep1);
            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
            _globalVariable.CT.SetStartTime();
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, "板间CT：" + _globalVariable.CT.CycleCT.ToString("F2") + " s", En_Logout_Type.Run, true);
            //刷新穴位状态
            for (int cav = 1; cav <= 12; cav++)
            {
                PublishCavityStateMsg(cav, EN_TrayStatus.NeedWork);
            }
            //移动到扫码位
            if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { carrierPos[0], carrierPos[1] }, true))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Scan->移动到扫描载具码位置失败(Thử chạy đến điểm quét mã tàu sân bay)：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { carrierPos[0], carrierPos[1] })}", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }
            if (_stepStatus.NextStep1 != RunStep) return (false, EN_RunRet.MotionErr, _stepStatus.NextStep1);
            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
            //扫载具码
            CarrierStatus carrierStatus = new CarrierStatus();
            if (_plc_Component.IsSimulateRun())
            {
                carrierStatus.carrierSN = $"Virtual{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Scanner->空跑模式，不扫载具码", En_Logout_Type.Run, true);
            }
            else if (!_scanner_Component.Param.BUse && _hive_Component.HiveStatus == EN_HiveStatus.Engineering)
            {
                carrierStatus.carrierSN = $"Virtual{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Scanner->扫码未启用", En_Logout_Type.Run, true);
            }
            else
            {
                if (!_scanner_Component.DataManManualTriger(out string carrierSN))
                {
                    while (!_scanner_Component.DataManManualTriger(out carrierSN))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Scan->扫码失败(Quét mã thất bại)", En_Logout_Type.Run, true);
                        var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, "扫载具码失败(Quét mã thất bại)", 101);
                        if (MessageBox.Show("载具码扫码失败，是否重新扫码？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
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
                }
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Scan->扫码完成，载具码：{carrierSN}", En_Logout_Type.Run, true);
                carrierStatus.carrierSN = carrierSN;
                AutoCreateSipSn(carrierSN, out carrierStatus.sipSN);

            }
            //绑定tapeSN
            for (int i = 0; i < 12; i++)
            {
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;

                carrierStatus.Tape_SN[i] = feederId == FeederId.左飞达 ? _stepStatus.ParamManager.MESParam.Feeder1TapeSN: _stepStatus.ParamManager.MESParam.Feeder2TapeSN;
            }
            _stepStatus.AddCarrier(carrierStatus);
            //自锁，不让扫码再次进来
            _stepStatus.AutoResetEvt_CtrlScanner.Reset();
            _stepStatus.ResetIsUpCamFinishedState();
            return (false, EN_RunRet.TaskOk, EN_RunStep.UpCameraIdentifyProductStep);
        }

        private void PublishCavityStateMsg(int cavity, EN_TrayStatus status)
        {
            _eventAggregator.Publish(new CarrierInfoPanelMessage() { Cavity = cavity, Status = status, }, action => { Task.Run(action); });
        }

        private void AutoCreateSipSn(string carrierSN, out string[] sipSNs)
        {
            sipSNs = new string[12];

            for (int i = 0; i < 12; i++)
            {
                Thread.Sleep(1);
                sipSNs[i] = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
        }

        private bool CheckCarrierISBad(CarrierStatus carrierStatus)
        {
            int existCount = 0;
            string dir = $@"D:\QKProject\Data\Other\CarrierNG";
            //string dir = $@"D:\CsvData\产品信息\UPCam\CarrierNG";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            try
            {
                string file = $@"{dir}\{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
                bool exists = File.Exists(file);
                if (exists)
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        sr.ReadLine();
                        while (sr.EndOfStream)
                        {
                            string[] data = sr.ReadLine().Split(',');
                            //读取到的CarrierSN和当前载具CarrierSN一致
                            if (data[2] == carrierStatus.carrierSN)
                            {
                                existCount++;
                            }
                        }
                    }

                    //当前载具在当天出现NG次数超过两次,就需要上传到MES判定为NG的Carrier
                    if (existCount > 1)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
