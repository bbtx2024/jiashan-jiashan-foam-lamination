using System.Collections.ObjectModel;
using QA.Business.Model.Alarm;

namespace QA.Business.Interfaces
{
    public interface IComponent
    {
        IParam Param { get; set; }
        string ComponentName { get; set; }
        bool IsConnected { get; }
        bool Initial(IParam param);
        bool Start();
        bool Stop();
        void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos);
    }
}
