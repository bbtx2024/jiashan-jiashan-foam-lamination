using Caliburn.Micro;

namespace QA.Pages.Models
{
    public class ComponentShowModel : PropertyChangedBase
    {
        private string _componentName;
        public string ComponentName
        {
            get => _componentName;
            set
            {
                _componentName = value;
                NotifyOfPropertyChange(() => ComponentName);
            }
        }

        private bool _componentEnable;
        public bool ComponentEnable
        {
            get => _componentEnable;
            set
            {
                _componentEnable = value;
                NotifyOfPropertyChange(() => ComponentEnable);
            }

        }

        private bool _componentConnectState;

        public bool ComponentConnectState
        {
            get => _componentConnectState;
            set
            {
                _componentConnectState = value;
                NotifyOfPropertyChange(() => ComponentConnectState);
            }
        }
    }
}
