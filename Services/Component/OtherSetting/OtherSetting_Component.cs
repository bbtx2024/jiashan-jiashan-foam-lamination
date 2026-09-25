using System;
using System.Collections.ObjectModel;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;

namespace QA.Business.Component.OtherSetting
{
    public class OtherSetting_Component : IOtherSetting
    {
        public IParam Param { get; set; }
        public string ComponentName { get; set; }
        public bool IsConnected { get; set; }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            throw new NotImplementedException();
        }

        public bool Initial(IParam param)
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public bool Stop()
        {
            throw new NotImplementedException();
        }

        public OtherSetting_Component()
        {
            Param = new OtherSettingParam();
        }

    }
}
