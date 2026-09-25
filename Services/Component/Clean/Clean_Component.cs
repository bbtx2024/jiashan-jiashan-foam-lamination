using System.Collections.ObjectModel;
using Caliburn.Micro;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;

namespace QA.Business.Component.Clean
{
    public class Clean_Component : IClean
    {
        private CleanParam _cleanParam;
        public IParam Param { get; set; }
        public string ComponentName { get; set; }

        public bool IsConnected { get; }

        public Clean_Component()
        {
            _cleanParam = IoC.Get<CleanParam>();
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            //throw new NotImplementedException();
        }

        public bool Initial(IParam param)
        {
            Param = _cleanParam = (param as CleanParam).DeepCopy();
            return true;
        }

        public bool Start()
        {
            return true;
            //throw new NotImplementedException();
        }

        public bool Stop()
        {
            return true;
            //throw new NotImplementedException();
        }
    }
}
