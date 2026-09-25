using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.Motion.Robot9075;
using QA.Business.Component.PLC;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Station;
using QA.Business.Steps;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Pages.OtherViews.ViewModels
{
    [Export("NormalToolsViewModel")]
    public class NormalToolsViewModel : Screen, IHandle<List<IComponent>>
    {
        #region Field

        private readonly IEventAggregator _eventAggregator;
        private readonly ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        private Motion9075_Component[] _robot9075s;
        private protected PLC_Component _plc_Component;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private StepStatus _stepStatus;
        private readonly MotionGoogol_Component _mGoogol_Component;
        private IBaseBiz _baseBiz;

        #endregion

        #region Property

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

        public string Press1 => "吸嘴1压力：" + (_cacheParamManager.PressParam.SglParam[0].GetCalibedPress(_mGoogol_Component.AInput[0]) / 10).ToString("F2") + "kg (0.4kg-0.6kg)";
        public string Press2 => "吸嘴2压力：" + (_cacheParamManager.PressParam.SglParam[1].GetCalibedPress(_mGoogol_Component.AInput[1]) / 10).ToString("F2") + "kg (0.4kg-0.6kg)";

        #endregion

        #region Constructor

        public NormalToolsViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _stepStatus = IoC.Get<StepStatus>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _baseBiz = IoC.Get<IBaseBiz>();
            Start();
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var delayTime = 1000;
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        //PlcReady = _plc_Component.GetReadySignal() == true ? "1" : "0";
                    }
                    catch (Exception e)
                    {
                        //NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
                    }
                    await Task.Delay(delayTime, _cancellationToken);
                }
            }, _cancellationToken);
            return true;
        }

        #endregion

        #region Handle

        public void Handle(List<IComponent> message)
        {
            EnableButtons = !(_baseBiz as BaseBiz).AutoRun;
            NotifyOfPropertyChange(() => Press1);
            NotifyOfPropertyChange(() => Press2);
        }

        #endregion

        #region Override

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
        }

        #endregion

        #region Method

        public async void ThrowTapes()
        {
            if (MessageBox.Show("确定清除所有吸嘴吸取的物料吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetCurPos() || _mGoogol_Component.Exit())
                    {
                        return;
                    }
                    if (Math.Abs(_mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) > 1
                        || Math.Abs(_mGoogol_Component.CurPos[(byte)En_AxisNum.Y1]) > 1
                        || Math.Abs(_mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) > 1
                        || Math.Abs(_mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) > 1
                        || Math.Abs(_mGoogol_Component.CurPos[(byte)En_AxisNum.Z2]) > 1)
                    {
                        MessageBox.Warning("当前位置不是原点位置！请复位后再清料", "操作提示");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid) || _mGoogol_Component.Exit())
                    {
                        MessageBox.Error("设置轴速失败", "操作提示");
                        return;
                    }
                    if (!_stepStatus.ThrowAllTapes() || _mGoogol_Component.Exit())
                    {
                        MessageBox.Error("清料过程异常！", "操作提示");
                        return;
                    }
                    MessageBox.Success("已全部清料！", "操作提示");
                });
            }
        }

        #endregion
    }
}
