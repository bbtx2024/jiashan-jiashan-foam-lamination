using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Component.Camera;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.Motion.Robot9075;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.UserControls.ViewModels
{
    public class CheckRegisterViewModel : Screen
    {
        #region field
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private Motion9075_Component _robot9075;
        private readonly ParamManager _paramManager;
        private IBaseBiz _baseBiz;
        private StepStatus _stepStatus;
        private Camera_Component _camera;
        private MotionGoogol_Component _mGoogol;
        private PLC_Component _plc_Component;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        #endregion

        #region property
        //private string _computerInfo;
        //public string ComputerInfo
        //{
        //    get => _computerInfo;
        //    set
        //    {
        //        _computerInfo = value;
        //        NotifyOfPropertyChange(() => ComputerInfo);
        //    }
        //}

        //private string _registerInfo;
        //public string RegisterInfo
        //{
        //    get => _registerInfo;
        //    set
        //    {
        //        _registerInfo = value;
        //        NotifyOfPropertyChange(() => RegisterInfo);
        //    }
        //}
        private bool _canOperate = false;
        public bool CanOperate
        {
            get => _canOperate;
            set
            {
                _canOperate = value;
                NotifyOfPropertyChange(() => CanOperate);
            }
        }
        #endregion

        [ImportingConstructor]
        public CheckRegisterViewModel()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _baseBiz = IoC.Get<IBaseBiz>();
            _camera = (Camera_Component)IoC.Get<ICamera>();
            _stepStatus = IoC.Get<StepStatus>();
            _mGoogol = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            Start();
        }

        #region method

        public async void FirstReset()
        {
            if (!_mGoogol.GetServoEnabledStatus())
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "控制器获取伺服使能失败", En_Logout_Type.Run, true);
                return;
            }
            try
            {
                _plc_Component.TrigPlcButton(EN_Plc_TrigButton.Reset);
                await Task.Run(() =>
                {
                    DateTime starttime = DateTime.Now;
                    while (true)
                    {
                        if ((DateTime.Now - starttime).TotalSeconds > 20)
                        {
                            break;
                        }
                        Task.Delay(200);
                        if (_mGoogol.IsResetCompleted)
                        {
                            Stop();
                            TryClose(true);
                            return;
                        }
                    }
                });
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"软件复位失败:{e.Message}{Environment.NewLine}{e.StackTrace}", En_Logout_Type.Exception, true);
            }
        }

        public void CloseWindow()
        {
            if (MessageBox.Show("退出软件？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            _mGoogol.SetServeOnOff(false);
            TryClose();
            Environment.Exit(0);
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                DateTime dt = DateTime.Now;
                while (true)
                {
                    var delayTime = 300;
                    try
                    {
                        CanOperate = _mGoogol.IsConnected;
                    }
                    catch (Exception e)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
                    }
                    //if (CanOperate == false)
                    //{
                    //    this.TryClose(true);
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "检测到固高板卡未连接！", En_Logout_Type.Default, true);
                    //    return;
                    //}
                    await Task.Delay(delayTime, _cancellationToken);
                }
            }, _cancellationToken);
            return true;
        }
        public bool Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
            }
            return true;
        }
        protected override void OnActivate()
        {
            Start();
            base.OnActivate();
        }
        protected override void OnDeactivate(bool close)
        {
            Stop();
            base.OnDeactivate(close);

        }
        #endregion
    }
}
