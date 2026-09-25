using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
using QA.Business.Message;
using QA.Business.Station;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Status.GantryNo3Steps
{
    public class ScannerCarrierSnStep : IStepStation3
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
        private Camera_Component _camera_Component;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.PressurizeStep;
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
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
        //    if (Enable.IsCDDCheck)
        //    {
        //        await Task.Delay(0);
        //        _stepStatus.AutoResetEvt_Pressurize.Reset();
        //        _stepStatus.AutoResetEvt_Pressurize.WaitOne();
        //        if (!_mGoogol_Component.SetPrUpLight(true))
        //        {
        //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"保压相机打开失败！", En_Logout_Type.Alarm);
        //            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        //        }
        //        //等待PLC启动相机信号
        //        while (!_plc_Component.GetCameraStartup())
        //        {
        //            if (_stepStatus.NextStep3 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
        //            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
        //            await Task.Delay(200);
        //        }
        //        float poss = _plc_Component.GetXpos();
        //        string TLTSN = $"Pressurize_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
        //        if (!_camera_Component.TFC(TLTSN, 1, "Foam+SIP", poss, 0, 0, out string[] stringdatas))
        //        {

        //            await Task.Delay(1000);
        //            string relativeDirng = "保压拍照测试\\NG\\" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        //            string fileNameSuffixng = $"0";
        //            _camera_Component.AddGroupInfo(TLTSN, relativeDirng, fileNameSuffixng);
        //            _camera_Component.GroupPicture();
        //        }
        //        await Task.Delay(1000);
        //        string relativeDirs = "保压拍照测试\\OK\\" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        //        string fileNameSuffixs = $"0";
        //        _camera_Component.AddGroupInfo(TLTSN, relativeDirs, fileNameSuffixs);
        //        _camera_Component.GroupPicture();

        //        _plc_Component.SetCameraOK();
        //        if (!_mGoogol_Component.SetPrUpLight(false))
        //        {
        //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"保压相机关闭失败！", En_Logout_Type.Alarm);
        //            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        //        }
        //        return (false, EN_RunRet.TaskOk, EN_RunStep.PressurizeStep);
        //    }


        //    //生产流程
        //    await Task.Delay(0);
        //    _stepStatus.AutoResetEvt_Pressurize.Reset();
        //    _stepStatus.AutoResetEvt_Pressurize.WaitOne();

        //    if (_stepStatus.NextStep3 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
        //    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);

        //    /*****新增保压*****/
        //    //等待PLC启动相机信号
        //    while (!_plc_Component.GetCameraStartup())
        //    {
        //        if (_stepStatus.NextStep3 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
        //        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
        //        await Task.Delay(200);
        //    }

        //    if (_stepStatus.NextStep3 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
        //    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);

        //    if (!_mGoogol_Component.SetPrUpLight(true))
        //    {
        //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"保压相机打开失败！", En_Logout_Type.Alarm);
        //        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        //    }
        //    float pos = _plc_Component.GetXpos();
        //    int preCDDnum = -1;
        //PreCDD:
        //    string sn = $"TFC_{_stepStatus.pressurCarrierSN}_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
        //    if (!_camera_Component.TFC(sn, 1, "Foam+SIP", pos, 0, 0, out string[] stringdata))
        //    {
        //        string relativeDir1 = "Pressurize\\NG\\" + sn;
        //        string fileNameSuffix1 = $"NG";
        //        _camera_Component.AddGroupInfo(sn, relativeDir1, fileNameSuffix1);
        //        Thread.Sleep(500);
        //        _camera_Component.GroupPicture();
        //        preCDDnum++;
        //        if (preCDDnum < _stepStatus.ParamManager.CameraParam.PreNGCDDnum)
        //        {
        //            goto PreCDD;
        //        }
        //        if (CDDNG("保压视觉定位NG,是否重新定位？"))
        //        {
        //            goto PreCDD;
        //        }
        //        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        //    }
        //    else
        //    {
        //        float data = _stepStatus.ParamManager.CameraParam.PressurizePosX - float.Parse(stringdata[0]);
        //        //视觉防呆，谨慎修改
        //        if (data > 0.05 || data < -0.05)
        //        {
        //            if (CDDNG($"{data}偏移值超限0.05以上,是否重新定位？"))
        //            {
        //                goto PreCDD;
        //            }
        //            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        //        }
        //        _plc_Component.SetPOS(data);
        //        _plc_Component.SetCameraOK();
        //    }
        //    if (!_mGoogol_Component.SetPrUpLight(false))
        //    {
        //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"保压相机关闭失败！", En_Logout_Type.Alarm);
        //        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        //    }
        //    string relativeDir = "Pressurize\\OK\\" + sn;
        //    string fileNameSuffix = $"OK";

        //    while (_camera_Component.groupInfoList.Count != 0)
        //    {
        //        await Task.Delay(5);
        //    }
        //    _camera_Component.AddGroupInfo(sn, relativeDir, fileNameSuffix);
            return (false, EN_RunRet.TaskOk, EN_RunStep.PressurizeStep);
        }

        /// <summary>
        /// 保压相机定位NG弹框提示
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool CDDNG(string msg)
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

    }
}
