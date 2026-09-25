using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;

namespace QA.SpotCheckPages.ViewModels
{
    [Export("SpotCheckCalibPageViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckCalibPageViewModel : Conductor<ICalibrationViewModel>.Collection.OneActive, INotifyPropertyChanged, ISpotPageViewModel
    {
        #region Field
        private List<ICalibrationViewModel> viewModels;
        private static int count;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "标定";

        private ushort _OrderID = 1;
        public ushort OrderID
        {
            get { return _OrderID; }
            set { _OrderID = value; }
        }

        private bool _isNextStepEnabled;
        public bool IsNextStepEnabled
        {
            get => _isNextStepEnabled;
            set
            {
                _isNextStepEnabled = value;
                NotifyOfPropertyChange(() => IsNextStepEnabled);
            }
        }

        private bool _isUpStepEnabled;
        public bool IsUpStepEnabled
        {
            get => _isUpStepEnabled;
            set
            {
                _isUpStepEnabled = value;
                NotifyOfPropertyChange(() => IsUpStepEnabled);
            }
        }

        #endregion

        public SpotCheckCalibPageViewModel()
        {
            IsNextStepEnabled = true;
            IsUpStepEnabled = false;

            viewModels = IoC.GetAll<ICalibrationViewModel>().OrderBy(item => item.OrderID).ToList();

            foreach (var item in viewModels)
            {
                EnsureItem(item);
            }
            if (viewModels.Any())
            {
                ActivateItem(viewModels[0]);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
        }

        protected override void OnInitialize()
        {

            base.OnInitialize();
        }

        public void NextStep()
        {

            count++;
            if (count >= 0 && count < viewModels.Count)
            {
                IsNextStepEnabled = true;
                IsUpStepEnabled = true;
                ActivateItem(viewModels[count]);
            }

            if (count == viewModels.Count - 1)
            {
                IsNextStepEnabled = false;
                IsUpStepEnabled = true;
                return;
            }

        }
        public void UpStep()
        {
            count--;
            if (count >= 0 && count < viewModels.Count)
            {
                IsUpStepEnabled = true;
                IsNextStepEnabled = true;
                ActivateItem(viewModels[count]);
            }
            if (count == 0)
            {
                IsUpStepEnabled = false;
                IsNextStepEnabled = true;
                return;
            }
        }
    }
}
