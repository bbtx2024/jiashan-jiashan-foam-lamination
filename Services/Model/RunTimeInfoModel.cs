using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using Caliburn.Micro;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Model.RunTimeInfo
{
    public enum LogModule
    {
        En_NormalRunTimeInfo,
        En_VisualRunTimeInfo,
    }

    public class RunTimeInfoModel : PropertyChangedBase
    {
        private EN_WARN_LEVEL _runLevel;
        public EN_WARN_LEVEL RunLevel
        {
            get => _runLevel;
            set
            {
                _runLevel = value;
                NotifyOfPropertyChange(() => RunLevel);
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

        private string _msg;
        public string Msg
        {
            get => _msg;
            set
            {
                _msg = value;
                NotifyOfPropertyChange(() => Msg);
            }
        }

        private string _detailMsg;
        public string DetailMsg
        {
            get => _detailMsg;
            set
            {
                _detailMsg = value;
                NotifyOfPropertyChange(() => DetailMsg);
            }
        }

        private string _dispMsg;
        public string DispMsg
        {
            get { return $"{Datetime?.ToString("HH:mm:ss.fff")}: {Msg}"; }
        }
    }

    public class RuntimeLogs
    {
        private object _lockobj = new object();
        public ObservableCollection<RunTimeInfoModel> Logs { get; set; } = new ObservableCollection<RunTimeInfoModel>();
        public ObservableCollection<RunTimeInfoModel> AlarmLogs { get; set; } = new ObservableCollection<RunTimeInfoModel>();

        public RuntimeLogs()
        {
            NLogTrace.CallBackInfo += AddNLogInfo;
        }

        private void AddNLogInfo(EN_WARN_LEVEL level, string msg)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(Pl =>
                {
                    lock (_lockobj)
                    {
                        var model = new RunTimeInfoModel()
                        {
                            RunLevel = level,
                            Datetime = DateTime.Now,
                            Msg = msg,
                            DetailMsg = "",
                        };
                        Logs.Add(model);
                        if (Logs.Count >= 1000)
                        {
                            Logs.RemoveAt(0);
                        }
                        if (level == EN_WARN_LEVEL.CriticalError || level == EN_WARN_LEVEL.Error || level == EN_WARN_LEVEL.Warn)
                        {
                            AlarmLogs.Add(model);
                            while (AlarmLogs.Count >= 1000)
                            {
                                AlarmLogs.RemoveAt(0);
                            }
                        }
                    }
                }, null);
            });
        }

        public void ClearLogs()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(Pl =>
                    {
                        lock (_lockobj)
                        {
                            Logs.Clear();
                        }
                    }, null);
                });
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"添加运行日志异常:{ex.Message},{ex.StackTrace}");
            }
        }

        public void ClearAlarmLogs()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(Pl =>
                    {
                        lock (_lockobj)
                        {
                            AlarmLogs.Clear();
                        }
                    }, null);
                });
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"添加运行日志异常:{ex.Message},{ex.StackTrace}");
            }
        }
    }
}
