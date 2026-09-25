using System.Collections.Generic;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;

namespace QA.Pages.OtherViews.ViewModels
{
    [Export("MesInfoViewModel")]
    public class MesInfoViewModel : Screen, IHandle<List<IComponent>>
    {
        #region Field
        private readonly IEventAggregator _eventAggregator;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        #endregion

        #region Property
        public string Line => _paramManager.MESParam.Line;
        public string Station => _paramManager.MESParam.Station;
        public string Fixid => _paramManager.MESParam.Fixid;
        public string Feeder1TapeSN => _paramManager.MESParam.Feeder1TapeSN;
        public string Feeder2TapeSN => _paramManager.MESParam.Feeder2TapeSN;

        public int TapeAlarmMaxUseCount => _paramManager.MESParam.TapeAlarmMaxUseCount;
        public int TapeAlarmTipLeftCount => _paramManager.MESParam.TapeAlarmTipLeftCount;

        public int Feeder1CurrentUseCount => _cacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder1TapeSN);
        public int Feeder2CurrentUseCount => _cacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder2TapeSN);

        public int Feeder1CurrentLeftCount => TapeAlarmMaxUseCount - Feeder1CurrentUseCount;
        public int Feeder2CurrentLeftCount => TapeAlarmMaxUseCount - Feeder2CurrentUseCount;

        #endregion

        public MesInfoViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }

        public void Handle(List<IComponent> message)
        {
            NotifyOfPropertyChange(() => Line);
            NotifyOfPropertyChange(() => Station);
            NotifyOfPropertyChange(() => Fixid);
            NotifyOfPropertyChange(() => Feeder1TapeSN);
            NotifyOfPropertyChange(() => Feeder2TapeSN);
            NotifyOfPropertyChange(() => TapeAlarmMaxUseCount);
            NotifyOfPropertyChange(() => TapeAlarmTipLeftCount);
            NotifyOfPropertyChange(() => Feeder1CurrentUseCount);
            NotifyOfPropertyChange(() => Feeder2CurrentUseCount);
            NotifyOfPropertyChange(() => Feeder1CurrentLeftCount);
            NotifyOfPropertyChange(() => Feeder2CurrentLeftCount);
        }
    }
}
