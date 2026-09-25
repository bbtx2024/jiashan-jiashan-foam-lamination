using Caliburn.Micro;

namespace QA.IntelligentEquipment.ViewModels
{
    public class LoadingWindowViewModel : PropertyChangedBase
    {
        #region Field

        #endregion

        #region Property

        private string _title;
        public string Title
        {
            get => _title;
            //set => SetProperty(ref _title, value);
        }

        private string _company;
        public string Company
        {
            get => _company;
            //set => SetProperty(ref _company, value);
        }
        #endregion

        public LoadingWindowViewModel()
        {
            //             Title = "贴合机";
            //             Company = "快克智能装备股份有限公司";
        }
    }
}
