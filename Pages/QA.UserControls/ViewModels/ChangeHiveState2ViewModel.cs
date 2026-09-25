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
    public class ChangeHiveState2ViewModel : Screen
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private CacheParamManager _cacheParamManager;
        private Hive_Component _hive_Component;
        private IBaseBiz _baseBiz;
        private readonly IWindowManager _windowManager;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "状态切换";

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
        public ChangeHiveState2ViewModel()
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
        }
        #endregion

        #region Method
        public void ChangeHiveStatePlannedDT(object obj)
        {
            int idx = Convert.ToInt32(obj);
            switch (idx)
            {
                case 1:
                    _hive_Component.ErrCode = "PD-01";
                    _hive_Component.ErrMessage = "Plan downtime";
                    _hive_Component.ErrDetail = "Daily Maintenance";
                    break;
                case 2:
                    _hive_Component.ErrCode = "PD-02";
                    _hive_Component.ErrMessage = "Plan downtime";
                    _hive_Component.ErrDetail = "Glue Change";
                    break;
                case 5:
                    _hive_Component.ErrCode = "PD-05";
                    _hive_Component.ErrMessage = "Plan downtime";
                    _hive_Component.ErrDetail = "Stress Test";
                    break;
                case 7:
                    _hive_Component.ErrCode = "PD-07";
                    _hive_Component.ErrMessage = "Plan downtime";
                    _hive_Component.ErrDetail = "Consumable Part ReplaceMent";
                    break;
                case 8:
                    _hive_Component.ErrCode = "PD-08";
                    _hive_Component.ErrMessage = "Plan downtime";
                    _hive_Component.ErrDetail = "Material ReplaceMent";
                    break;
                case 101:
                    _hive_Component.ErrCode = "PD-101";
                    _hive_Component.ErrMessage = "Plan downtime";
                    _hive_Component.ErrDetail = "Others";
                    break;
                default:
                    break;
            }
            _hive_Component.ManualState = EN_HiveStatus.Downtime;
            TryClose(true);
        }

        public void ChangeHiveStateDowntime(object obj)
        {
            string errcode = Convert.ToString(obj);
            _hive_Component.ErrCode = errcode;
            _hive_Component.ManualState = EN_HiveStatus.Downtime;
            TryClose(true);
        }
        #endregion
    }
}
