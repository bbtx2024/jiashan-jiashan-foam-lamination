using System.Collections.Generic;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Pages.OtherViews.ViewModels
{
    [Export("FunctionIsEnabledViewModel")]
    public class FunctionIsEnabledViewModel : Screen, IHandle<List<IComponent>>, IHandle<FunctionIsEnabledMessage>
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private CacheParamManager _cacheParamManager;
        private IBaseBiz _baseBiz;
        public PLC_Component plc_Component;
        #endregion

        #region Property
        public HomeUiParam_Enable Enable { get => _cacheParamManager.HomeUiParam.Enable; }

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
        public FunctionIsEnabledViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _cacheParamManager = IoC.Get<CacheParamManager>();
            plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _baseBiz = IoC.Get<IBaseBiz>();
        }
        #endregion

        #region Handle
        public void Handle(List<IComponent> message)
        {
            EnableButtons = !(_baseBiz as BaseBiz).AutoRun;
        }
        public void Handle(FunctionIsEnabledMessage message)
        {
            SwitchFeederId();
        }

        #endregion

        #region Method
        public void SwitchFeederId()
        {
            FeederId feederId;
            switch (Enable.FeederId)
            {
                case FeederId.左飞达:
                    feederId = FeederId.右飞达;
                    break;
                case FeederId.右飞达:
                    feederId = FeederId.左飞达;
                    break;
                default:
                    return;
            }
            plc_Component.FeederCDDCylinderSS(feederId);
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"取料Feeder {Enable.FeederId}->{feederId}", En_Logout_Type.SystemParam, false);
            Enable.FeederId = feederId;
            _cacheParamManager.SaveHomeUiParam();
        }
        public void SwitchPickOpportunity()
        {
            EN_PickOpportunity pickOpportunity;
            switch (Enable.PickOpportunity)
            {
                case EN_PickOpportunity.AfterStart:
                    pickOpportunity = EN_PickOpportunity.AfterUpCam;
                    break;
                case EN_PickOpportunity.AfterUpCam:
                    pickOpportunity = EN_PickOpportunity.AfterStart;
                    break;
                default:
                    return;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"取料时机 {Enable.PickOpportunity}->{pickOpportunity}", En_Logout_Type.SystemParam, false);
            Enable.PickOpportunity = pickOpportunity;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchOncePickNum()
        {
            EN_OncePickNum pickNum;
            switch (Enable.OncePickNum)
            {
                case EN_OncePickNum.One:
                    pickNum = EN_OncePickNum.Two;
                    break;
                case EN_OncePickNum.Two:
                    pickNum = EN_OncePickNum.One;
                    break;
                default:
                    return;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"同时取料个数 {Enable.OncePickNum}->{pickNum}", En_Logout_Type.SystemParam, false);
            Enable.OncePickNum = pickNum;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchKeepNoTapeOnFeeder()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用保持平台无料 {Enable.KeepNoTapeOnFeeder}->{!Enable.KeepNoTapeOnFeeder}", En_Logout_Type.SystemParam, false);
            Enable.KeepNoTapeOnFeeder = !Enable.KeepNoTapeOnFeeder;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchAutoThrowTapesAfterReset()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用复位自动清料 {Enable.AutoThrowTapesAfterReset}->{!Enable.AutoThrowTapesAfterReset}", En_Logout_Type.SystemParam, false);
            Enable.AutoThrowTapesAfterReset = !Enable.AutoThrowTapesAfterReset;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchUseNozzle1()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用1号吸嘴 {Enable.UseNozzle1}->{!Enable.UseNozzle1}", En_Logout_Type.SystemParam, false);
            Enable.UseNozzle1 = !Enable.UseNozzle1;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchUseNozzle2()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用2号吸嘴 {Enable.UseNozzle2}->{!Enable.UseNozzle2}", En_Logout_Type.SystemParam, false);
            Enable.UseNozzle2 = !Enable.UseNozzle2;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchUseNozzle3()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用3号吸嘴 {Enable.UseNozzle3}->{!Enable.UseNozzle3}", En_Logout_Type.SystemParam, false);
            Enable.UseNozzle3 = !Enable.UseNozzle3;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchUseNozzle4()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用4号吸嘴 {Enable.UseNozzle4}->{!Enable.UseNozzle4}", En_Logout_Type.SystemParam, false);
            Enable.UseNozzle4 = !Enable.UseNozzle4;
            _cacheParamManager.SaveHomeUiParam();
        }

        public void SwitchUseVacSucCheck()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用检测真空吸 {Enable.UseVacSucCheck}->{!Enable.UseVacSucCheck}", En_Logout_Type.SystemParam, false);
            Enable.UseVacSucCheck = !Enable.UseVacSucCheck;
            _cacheParamManager.SaveHomeUiParam();
        }
        public void SwitchFeederGetCheck()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用Feeder盲取 {Enable.IsFeederCheck}->{!Enable.IsFeederCheck}", En_Logout_Type.SystemParam, false);
            Enable.IsFeederCheck = !Enable.IsFeederCheck;
            _cacheParamManager.SaveHomeUiParam();
        }
        public void SwitchCDDGetCheck()
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, $"启用相机只定位 {Enable.IsCDDCheck}->{!Enable.IsCDDCheck}", En_Logout_Type.SystemParam, false);
            Enable.IsCDDCheck = !Enable.IsCDDCheck;
            _cacheParamManager.SaveHomeUiParam();
        }
        
        #endregion
    }
}
