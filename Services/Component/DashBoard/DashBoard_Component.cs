using System;
using System.Collections.ObjectModel;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;

namespace QA.Business.Component.DashBoard
{
    public class DashBoard_Component : IDashBoard
    {
        public IParam Param { get; set; } = null;
        public string ComponentName { get; set; } = "DashBoard";

        public bool IsConnected { get; set; }

        public DashBoard_Component()
        {
            Param = new DashBoardParam();  //DashBoard参数放在组件类中
        }

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
    }
}
