using System.ComponentModel.Composition;
using Caliburn.Micro;
using QA.Business.Component.Motion.Googol;
using QA.Business.Station;

namespace QA.Pages.OtherViews.ViewModels
{
    [Export("RunStep3ViewModel")]
    public class RunStep3ViewModel : Screen, IHandle<RunStepInfo>
    {
        #region field
        private readonly IEventAggregator _eventAggregator;
        #endregion

        #region property
        private string _runStep;
        public string RunStep
        {
            get => _runStep;
            set
            {
                _runStep = value;
                NotifyOfPropertyChange(() => RunStep);
            }
        }
        #endregion

        public RunStep3ViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
        }

        public void Handle(RunStepInfo message)
        {
            if (message.Station == En_StationNo.StationNo3)
            {
                RunStep = message.RunStep.ToString();
            }
        }
    }
}

