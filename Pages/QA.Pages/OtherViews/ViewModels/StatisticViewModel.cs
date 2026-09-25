using System.ComponentModel.Composition;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Manager;

namespace QA.Pages.OtherViews.ViewModels
{
    [Export("StatisticViewModel")]
    public class StatisticViewModel : Screen
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private CacheParamManager _cacheParamManager;
        #endregion

        #region Property

        public HomeUiParam_Statistic Statistic { get; private set; }

        #endregion

        public StatisticViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _cacheParamManager = IoC.Get<CacheParamManager>();
            Statistic = _cacheParamManager.HomeUiParam.Statistic;
        }
    }
}
