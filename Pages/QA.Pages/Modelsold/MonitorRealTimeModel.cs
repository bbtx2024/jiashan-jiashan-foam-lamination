using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using FontAwesome5;

namespace QA.Pages.Models
{
    public class MonitorRealTimeModel : PropertyChangedBase
    {
        private string _monitorName;
        public string MonitorName
        {
            get => _monitorName;
            set {
                _monitorName = value;
                NotifyOfPropertyChange(() => MonitorName);
            }           
        }

        private ObservableCollection<string> _realtimeDataCollection = new ObservableCollection<string>();
        public ObservableCollection<string> RealtimeDataCollection
        {
            get => _realtimeDataCollection;
            set {
                _realtimeDataCollection = value;
                NotifyOfPropertyChange(() => RealtimeDataCollection);
            }
            
        }
    }
}
