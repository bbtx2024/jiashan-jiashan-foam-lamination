using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Component.Camera;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Station;
using QA_Infrastructure.NLogOut;
using QA_Infrastructure;
using System.Windows;
using IComponent = QA.Business.Interfaces.IComponent;
using MessageBox = HandyControl.Controls.MessageBox;
using QA_Infrastructure.GeneralTool;
using QA.Business.Define;

namespace QA.SpotCheckPages.Calibration.ViewModels
{
    //[Export(typeof(ICalibrationViewModel))]
    public class PressurizeCalibViewModel : Screen, INotifyPropertyChanged, ICalibrationViewModel, IHandle<List<IComponent>>
    {
        #region Field
        private readonly IEventAggregator _eventAggregator;
        private MotionGoogol_Component _mGoogol_Component;
        private PLC_Component _plc_Component;
        private Camera_Component _camera_Component;
        private CacheParamManager _cacheParamManager;
        private MyUserManager _myUserManager;
        private ParamManager _paramManager;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "保压定位标定";

        public ushort OrderID { get; set; } = 2;

        private bool _enableButtons = false;
        public bool EnableButtons
        {
            get => _enableButtons;
            set
            {
                _enableButtons = value;
                NotifyOfPropertyChange(() => EnableButtons);
            }
        }
        #endregion

        #region Constructor
        public PressurizeCalibViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _myUserManager = IoC.Get<MyUserManager>();
        }
        #endregion

        #region Handle
        public void Handle(List<IComponent> message)
        {
            EnableButtons = _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeManager;
            _paramManager = IoC.Get<ParamManager>();
        }
        #endregion
        #region Method
        public async void CalibPress()
        {
            //if (_paramManager.MESParam.TapeNames == TapeNames.Flex_GND_Tape)
            //{
            //    MessageBox.Error("Flex_GND_Tape无保压相机！");
            //    return;
            //}
            if (MessageBox.Show($"确定开始保压定位标定吗?请操作PLC进行标定", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                        {
                            MessageBox.Error("轴系在运动，等静止再操作！");
                            return;
                        }
                        if (!_mGoogol_Component.CleanAlarm())
                        {
                            MessageBox.Error("清除报警失败！");
                            return;
                        }
                        if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid))
                        {
                            MessageBox.Error("设置轴速为示教中速失败！");
                            return;
                        }
                        if (!_mGoogol_Component.SetPrUpLight(true))
                        {
                            MessageBox.Error("保压上光源打开失败！");
                            return;
                        }
                        _camera_Component.SendCB8();
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                });
            }
        }
        #endregion
    }
}
