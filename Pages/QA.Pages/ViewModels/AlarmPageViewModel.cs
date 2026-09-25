using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Manager;
using QA.Business.Model.Alarm;
using QA.Pages.Interfaces;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Pages.ViewModels
{
    [Export(typeof(IPageViewModel))]
    public class AlarmPageViewModel : Screen, INotifyPropertyChanged, IPageViewModel, IHandle<ObservableCollection<AlarmInfoModel>>
    {
        #region Field        
        private static readonly object lockObj = new object();
        private readonly IEventAggregator _eventAggregator;
        private bool clearHistoryAlarm = false;
        private DateTime lastFreshTime = DateTime.Now;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "报警页面";

        public ushort OrderID { get; set; } = 3;

        private ObservableCollection<AlarmInfoModel> _currentAlarmInfoModels = new ObservableCollection<AlarmInfoModel>();
        public ObservableCollection<AlarmInfoModel> CurrentAlarmInfoModels
        {
            get => _currentAlarmInfoModels;
            set
            {
                _currentAlarmInfoModels = value;
                NotifyOfPropertyChange(() => CurrentAlarmInfoModels);
            }
        }

        private ObservableCollection<AlarmInfoModel> _historyAlarmInfoModels = new ObservableCollection<AlarmInfoModel>();
        public ObservableCollection<AlarmInfoModel> HistoryAlarmInfoModels
        {
            get => _historyAlarmInfoModels;
            set
            {
                _historyAlarmInfoModels = value;
                NotifyOfPropertyChange(() => HistoryAlarmInfoModels);
            }
        }

        public HomeUiParam_Statistic Statistic { get; set; }
        #endregion

        #region Constructor
        public AlarmPageViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            var _cacheParamManager = IoC.Get<CacheParamManager>();
            Statistic = _cacheParamManager.HomeUiParam.Statistic;
        }
        #endregion

        #region Caliburn.Micro
        public void Handle(ObservableCollection<AlarmInfoModel> alarms)
        {
            ObservableCollection<AlarmInfoModel> newAlarms = alarms.DeepCopy();
            DateTime time = DateTime.Now;
            lock (lockObj)
            {
                try
                {
                    var currentAlarms = CurrentAlarmInfoModels.DeepCopy();
                    if (clearHistoryAlarm)
                    {
                        HistoryAlarmInfoModels = new ObservableCollection<AlarmInfoModel>();
                        clearHistoryAlarm = false;
                    }
                    bool isAlarmChanged = newAlarms.Count != currentAlarms.Count;//数目不一样肯定变了
                    if (!isAlarmChanged)
                    {
                        foreach (var newModel in newAlarms)
                        {
                            bool contains = false;
                            foreach (var oldModel in currentAlarms)
                            {
                                if (newModel.AlarmLevel == oldModel.AlarmLevel
                                    && newModel.AlarmModule == oldModel.AlarmModule
                                    && newModel.AlarmMsg == oldModel.AlarmMsg)
                                {
                                    contains = true;
                                    break;
                                }
                            }
                            if (!contains)
                            {
                                isAlarmChanged = true;
                            }
                        }
                    }
                    if (isAlarmChanged)
                    {
                        var historyAlarms = HistoryAlarmInfoModels.DeepCopy();
                        //将新增的报警信息添加到历史报警
                        foreach (var newModel in newAlarms)
                        {
                            bool isNewAlarm = true;
                            foreach (var oldModel in currentAlarms)
                            {
                                if (newModel.AlarmLevel == oldModel.AlarmLevel
                                    && newModel.AlarmModule == oldModel.AlarmModule
                                    && newModel.AlarmMsg == oldModel.AlarmMsg)
                                {
                                    isNewAlarm = false;
                                    break;
                                }
                            }
                            if (isNewAlarm)
                            {
                                AlarmInfoModel model = newModel.DeepCopy();
                                model.Index = historyAlarms.Count + 1;
                                historyAlarms.Add(model);
                                //报警存储至csv，用于后续读取
                                SaveToAlarmCsv(time, model);
                            }
                        }
                        //修改报警信息
                        HistoryAlarmInfoModels = historyAlarms;
                        CurrentAlarmInfoModels = newAlarms;
                    }
                    //刷新报警次数显示
                    if ((time - lastFreshTime).TotalMilliseconds > 3000)
                    {
                        Statistic.AlarmCount = ReadAlarmList(time, true).Count;
                        lastFreshTime = time;
                    }
                }
                catch (Exception e)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception);
                }
            }
        }
        #endregion

        #region Method
        public void ClearHistoryAlarm()
        {
            clearHistoryAlarm = true;
        }
        #endregion

        #region 添加一个新报警到csv中
        struct AlarmInfo
        {
            public DateTime time;
            public string msg;
        }

        private void SaveToAlarmCsv(DateTime time, AlarmInfoModel model)
        {
            bool isErrorAlarm = model.AlarmLevel >= EN_WARN_LEVEL.Error;
            List<AlarmInfo> alarmList = ReadAlarmList(time, isErrorAlarm);
            string alarmStr = model.AlarmModule.ToString() + "_" + model.AlarmMsg;
            alarmList.Add(new AlarmInfo { time = time, msg = alarmStr });
            WriteAlarmList(time, isErrorAlarm, alarmList);
        }

        private string GetAlarmFileByTime(DateTime time, bool isErrorAlarm)
        {
            DateTime yesterday20H = time.Date.AddHours(-4);
            DateTime today8H = time.Date.AddHours(8);
            DateTime today20H = time.Date.AddHours(20);
            DateTime targetTime;
            if (time < today8H)
            {
                targetTime = yesterday20H;
            }
            else if (time < today20H)
            {
                targetTime = today8H;
            }
            else
            {
                targetTime = today20H;
            }
            //string dir = AppDomain.CurrentDomain.BaseDirectory + "AlarmData//" + targetTime.ToString("yyyyMM");
            string dir = @"D:\QKProject\Data\AlarmData\" + targetTime.ToString("yyyyMM");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir + (isErrorAlarm ? "//ErrAlarm_" : "//TipAlarm_") + targetTime.ToString("yyyyMMdd-HH") + ".csv";
        }

        private List<AlarmInfo> ReadAlarmList(DateTime time, bool isErrorAlarm)
        {
            List<AlarmInfo> alarmList = new List<AlarmInfo>();
            try
            {
                string file = GetAlarmFileByTime(time, isErrorAlarm);
                if (File.Exists(file))
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string s = sr.ReadLine();
                        while ((s = sr.ReadLine()) != null)
                        {
                            string[] data = s.Split(',');
                            alarmList.Add(new AlarmInfo()
                            {
                                time = DateTime.Parse(data[0]),
                                msg = data[2],
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception, false);
            }
            return alarmList;
        }

        private void WriteAlarmList(DateTime time, bool isErrorAlarm, List<AlarmInfo> alarmList)
        {
            try
            {
                string file = GetAlarmFileByTime(time, isErrorAlarm);
                using (StreamWriter sw = new StreamWriter(file, false))
                {
                    sw.WriteLine("报警时间,报警次数,报警内容");
                    Dictionary<string, int> alarmCountDic = new Dictionary<string, int>();
                    foreach (AlarmInfo alarmInfo in alarmList)
                    {
                        if (alarmCountDic.ContainsKey(alarmInfo.msg))
                        {
                            alarmCountDic[alarmInfo.msg]++;
                        }
                        else
                        {
                            alarmCountDic[alarmInfo.msg] = 1;
                        }
                        sw.WriteLine(alarmInfo.time + "," + alarmCountDic[alarmInfo.msg] + "," + alarmInfo.msg);
                    }
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception, false);
            }
        }
        #endregion
    }
}
