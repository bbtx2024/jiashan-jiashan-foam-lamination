using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using DCCK.PlatformSDK;
using Microsoft.Win32;
using QA.Business;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.Motion.Robot9075;
using QA.Business.Component.PLC;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.RunTimeInfo;
using QA.Business.Station;
using QA.Business.Steps;
using QA.Pages.Interfaces;
using QA.Pages.Models;
using QA.Pages.OtherViews.ViewModels;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Pages.ViewModels
{
    [Export(typeof(IPageViewModel))]
    public class MainFormPageViewModel : Screen, IPageViewModel, IHandle<List<IComponent>>, IHandle<PlcIOMode>
    {
        #region Field
        private readonly IEventAggregator _eventAggregator;
        private IBaseBiz _baseBiz;
        public VpsSolution DcckVpsSolution = null;
        public PLC_Component plc_Component;
        #endregion

        #region Property
        public ushort OrderID { get; set; } = 0;

        public RuntimeLogs RunTimeLogs { get; } = IoC.Get<RuntimeLogs>();

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

        /// <summary>
        /// 显示结果图像
        /// </summary>
        public UserControl _visionBindingResult;
        public UserControl VisionBindingResult
        {
            get { return _visionBindingResult; }
            set { _visionBindingResult = value; NotifyOfPropertyChange(() => VisionBindingResult); }
        }

        /// <summary>
        /// 显示上相机实时图像
        /// </summary>
        public UserControl _visionBindingUpCamera;
        public UserControl VisionBindingUpCamera
        {
            get { return _visionBindingUpCamera; }
            set { _visionBindingUpCamera = value; NotifyOfPropertyChange(() => VisionBindingUpCamera); }
        }

        /// <summary>
        /// 显示下相机实时图像
        /// </summary>
        public UserControl _visionBindingBottomCamera;
        public UserControl VisionBindingBottomCamera
        {
            get { return _visionBindingBottomCamera; }
            set { _visionBindingBottomCamera = value; NotifyOfPropertyChange(() => VisionBindingBottomCamera); }
        }


        /// <summary>
        /// PLC IO
        /// </summary>
        public PlcIOMode plcIOMode;
        public PlcIOMode PlcIOMode
        {
            get { return plcIOMode; }
            set { plcIOMode = value; NotifyOfPropertyChange(() => PlcIOMode); }
        }
        #endregion

        #region UI
        [Import("NormalToolsViewModel")]
        public NormalToolsViewModel ShowNormalToolsViewModel { get; set; }

        [Import("FunctionIsEnabledViewModel")]
        public FunctionIsEnabledViewModel ShowFunctionIsEnabledViewModel { get; set; }
        [Import("RunStep3ViewModel")]
        public RunStep3ViewModel ShowRunStep3ViewModel { get; set; }

        [Import("RunStep2ViewModel")]
        public RunStep2ViewModel ShowRunStep2ViewModel { get; set; }

        [Import("RunStep1ViewModel")]
        public RunStep1ViewModel ShowRunStep1ViewModel { get; set; }

        [Import("CarrierInfoPanelViewModel")]
        public CarrierInfoPanelViewModel CarrierInfoPanelViewModel { get; set; }

        [Import("HiveChartViewModel")]
        public HiveChartViewModel HiveChartViewModel { get; set; }

        [Import("StatisticViewModel")]
        public StatisticViewModel StatisticViewModel { get; set; }

        [Import("MesInfoViewModel")]
        public MesInfoViewModel MesInfoViewModel { get; set; }
        #endregion

        #region Constructor
        public MainFormPageViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _baseBiz = IoC.Get<IBaseBiz>();
            _baseBiz.Initial();
            _baseBiz.Start();
        }
        #endregion

        #region Handle
        public void Handle(List<IComponent> message)
        {
            EnableButtons = !(_baseBiz as BaseBiz).AutoRun;
        }
        #endregion

        #region Method
        public void ClearLogs()
        {
            if (MessageBox.Show("确定清空运行日志吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                RunTimeLogs.ClearLogs();
            }
        }

        public void ClearAlarmLogs()
        {
            if (MessageBox.Show("确定清空报警信息吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                RunTimeLogs.ClearAlarmLogs();
            }
        }

        /// <summary>
        /// 加载德创视觉
        /// </summary>
        public void VisionAdd()
        {
            try
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "(*.vps)|*.vps";
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == true)
                {
                    DcckVpsSolution.Dispose();
                    VisionBindingResult = DcckVpsSolution.HMIControl;
                    VisionBindingUpCamera = DcckVpsSolution.GetWindowControl("上相机实时图像");
                    VisionBindingBottomCamera = DcckVpsSolution.GetWindowControl("下相机实时图像");
                    DcckVpsSolution.Load(openFileDialog.FileName, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 关闭德创视觉
        /// </summary>
        public void VisionClose()
        {
            DcckVpsSolution.Dispose();
            VisionBindingResult = null;
            VisionBindingUpCamera = null;
            VisionBindingBottomCamera = null;
        }

        public void Handle(PlcIOMode message)
        {
            PlcIOMode = message;
        }

        public void GetTape()
        {
            plc_Component.TrigFeederConveyType();
        }
        //改变吸废料状态
        public void ChangeSuctionFilm()
        {
            bool suctionFilm = plc_Component.GetSuctionFilm();
            if (suctionFilm)
            {
                //初始为吹风，关风
                plc_Component.SetSuctionFilm(0);
            }
            else
            {
                //初始为关风,吹风
                plc_Component.SetSuctionFilm(1);

            }
        }

        #endregion
    }
}
