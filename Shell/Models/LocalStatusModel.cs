using Caliburn.Micro;

namespace QA.IntelligentEquipment.Models
{
    public class LocalStatusModel : PropertyChangedBase
    {
        private string _version = string.Empty;
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                NotifyOfPropertyChange(() => Version);
            }
        }

        private string _currentTime = string.Empty;
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                NotifyOfPropertyChange(() => CurrentTime);
            }
        }
        public override string ToString()
        {
            return $"{Version}   {CurrentTime}";
        }
    }
}
