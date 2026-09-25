using System;
using Caliburn.Micro;
using QA_Infrastructure;

namespace QA.Business.Model.Alarm
{
    public class AlarmInfoModel : PropertyChangedBase
    {
        private int? _index;
        public int? Index
        {
            get => _index;
            set
            {
                _index = value;
                NotifyOfPropertyChange(() => Index);
            }
        }

        private EN_WARN_LEVEL _alarmLevel;
        public EN_WARN_LEVEL AlarmLevel
        {
            get => _alarmLevel;
            set
            {
                _alarmLevel = value;
                NotifyOfPropertyChange(() => AlarmLevel);
            }
        }

        private EN_WarnModules _alarmModule;
        public EN_WarnModules AlarmModule
        {
            get => _alarmModule;
            set
            {
                _alarmModule = value;
                NotifyOfPropertyChange(() => AlarmModule);
            }
        }

        private int? _errorCode;
        public int? ErrorCode
        {
            get => _errorCode;
            set
            {
                _errorCode = value;
                NotifyOfPropertyChange(() => ErrorCode);
            }
        }

        private string _alarmMsg;
        public string AlarmMsg
        {
            get => _alarmMsg;
            set
            {
                _alarmMsg = value;
                NotifyOfPropertyChange(() => AlarmMsg);
            }
        }

        private DateTime? _datetime;

        public DateTime? Datetime
        {
            get => _datetime;
            set
            {
                _datetime = value;
                NotifyOfPropertyChange(() => Datetime);
            }
        }
    }
}
