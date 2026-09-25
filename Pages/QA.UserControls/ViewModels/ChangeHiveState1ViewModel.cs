using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.HIVE;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.UserControls.ViewModels
{
    public class ChangeHiveState1ViewModel : Screen, IHandle<List<IComponent>>
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private CacheParamManager _cacheParamManager;
        private Hive_Component _hive_Component;
        private IBaseBiz _baseBiz;
        private readonly IWindowManager _windowManager;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "HIVE状态切换";

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

        private bool _canChangeToRunning = false;
        public bool CanChangeToRunning
        {
            get => _canChangeToRunning;
            set
            {
                _canChangeToRunning = value;
                NotifyOfPropertyChange(() => CanChangeToRunning);
            }
        }

        private bool _canChangeToIdle = false;
        public bool CanChangeToIdle
        {
            get => _canChangeToIdle;
            set
            {
                _canChangeToIdle = value;
                NotifyOfPropertyChange(() => CanChangeToIdle);
            }
        }

        private bool _canChangeToDowntime = false;
        public bool CanChangeToDowntime
        {
            get => _canChangeToDowntime;
            set
            {
                _canChangeToDowntime = value;
                NotifyOfPropertyChange(() => CanChangeToDowntime);
            }
        }
        #endregion

        #region Constructor
        public ChangeHiveState1ViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _baseBiz = IoC.Get<IBaseBiz>();
            _windowManager = IoC.Get<IWindowManager>();
        }
        #endregion

        #region Handle
        public void Handle(List<IComponent> message)
        {
            EnableButtons = !(_baseBiz as BaseBiz).AutoRun;
            switch (_hive_Component.HiveStatus)
            {
                case EN_HiveStatus.Running:
                    CanChangeToRunning = false;
                    CanChangeToIdle = true;
                    CanChangeToDowntime = true;
                    break;
                case EN_HiveStatus.Idle:
                    CanChangeToRunning = true;
                    CanChangeToIdle = false;
                    CanChangeToDowntime = true;
                    break;
                case EN_HiveStatus.Downtime:
                    CanChangeToRunning = true;
                    CanChangeToIdle = false;
                    CanChangeToDowntime = false;
                    break;
            }
        }
        #endregion

        #region Method
        public void ChangeHiveState(object obj)
        {
            int stateInt = Convert.ToInt32(obj);
            EN_HiveStatus ManualState = (EN_HiveStatus)Enum.Parse(typeof(EN_HiveStatus), stateInt.ToString());
            if (ManualState == EN_HiveStatus.Downtime)
            {
                var settings = new Dictionary<string, object> { { "ResizeMode", ResizeMode.NoResize } };
                bool? result = _windowManager.ShowDialog(new ChangeHiveState2ViewModel(), null, settings);
                if (result != true)
                {
                    return;
                }
            }
            else
            {
                _hive_Component.ManualState = ManualState;
                _hive_Component.ErrCode = "";
                _hive_Component.ErrMessage = "";
                _hive_Component.ErrDetail = "";
            }
            TryClose(true);
        }
        #endregion
    }
}
