using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Wpf;
using QA.Business;
using QA.Business.CacheParam;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Converts;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace QA.Pages.OtherViews.ViewModels
{
    [Export("HiveChartViewModel")]
    public class HiveChartViewModel : Screen, IHandle<ObservableCollection<HiveParamDashBoard>>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly CacheParamManager _cacheParamManager;
        private readonly ParamManager _paramManager;
        private readonly GlobalVariable _globalVariable;
        private StepStatus _stepStatus;
        private Hive_Component _hive_Component;
        private MES_Component _mes_Component;
        private HiveStatusToStringConvert hiveStatusToStringConvert = new HiveStatusToStringConvert();
        private HiveStatusToColorConvert hiveStatusToColorConvert = new HiveStatusToColorConvert();

        private ObservableCollection<HiveParamDashBoard> _paramDashBoard = new ObservableCollection<HiveParamDashBoard>();
        public ObservableCollection<HiveParamDashBoard> ParamDashBoard
        {
            get { return _paramDashBoard; }
            set
            {
                _paramDashBoard = value;
                NotifyOfPropertyChange(() => ParamDashBoard);
            }
        }
        public HomeUiParam_Statistic Statistic { get; private set; }  //长期统计信息，比如ok数目等等
        public HomeUiStatus_CT CT { get; private set; }  //临时统计信息，如CT等临时变量
        public SeriesCollection ToDayMachineStatusCollection { get; set; }
        public SeriesCollection ChoiceTimeSpanErrorStatistics { get; set; }
        public Func<ChartPoint, string> PointLabel { get; set; }
        public Func<double, string> Formatter { get; set; }
        public Func<int, string> XFormatter { get; set; }

        private DateTime _startDate = DateTime.Now.Date;
        public DateTime StartDate
        {
            get { return _startDate; }
            set
            {
                _startDate = value;
                NotifyOfPropertyChange(() => StartDate);
            }
        }

        private DateTime _endDate = DateTime.Now.Date.AddDays(1);
        public DateTime EndDate
        {
            get { return _endDate; }
            set
            {
                _endDate = value;
                NotifyOfPropertyChange(() => EndDate);
            }
        }

        private string[] _timeLabels = new string[7];
        public string[] TimeLabels
        {
            get
            {
                return _timeLabels;
            }
            set
            {
                _timeLabels = value;
                NotifyOfPropertyChange(() => TimeLabels);
            }
        }

        //纵坐标错误类型
        private string[] _errorCodeTypeLabels = new string[6];
        public string[] ErrorCodeTypeLabels
        {
            get { return _errorCodeTypeLabels; }
            set
            {
                _errorCodeTypeLabels = value;
                NotifyOfPropertyChange(() => ErrorCodeTypeLabels);
            }
        }

        public string InputOutput => $"{Statistic.OkCount + Statistic.NgCount} / {Statistic.OkCount}";
        public float OkRate => Statistic.OkRate;
        public string PassFail => $"{Statistic.OkCount} / {Statistic.NgCount}";
        public int UPH => _stepStatus.OutputPerHour.ProductionQuantitys[DateTime.Now.Hour].TotalCount;
        public float CTCircle => CT.CycleCT;
        public string Site => _paramManager.HiveParam.Site;
        public string Vendor => _paramManager.HiveParam.Vendor;
        public string SWVersion => _paramManager.OtherSettingParam.SoftwareVersion.Replace("_", "__");
        public string MainSWPath => Process.GetCurrentProcess().MainModule.FileName;
        public string MSHash => _hive_Component.GetMainSoftwareSHA1();
        public Brush MSHashBG => (DateTime.Now - _paramManager.HiveParam.LastUpdateTime).TotalDays > _paramManager.HiveParam.UpdateRedDays ? Brushes.Black : Brushes.Red;
        public bool IsHiveConnected => _hive_Component.IsConnected;
        public bool IsMesConnected => _mes_Component.IsConnected;

        public HiveChartViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _stepStatus = IoC.Get<StepStatus>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _paramManager = IoC.Get<ParamManager>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _mes_Component = (MES_Component)IoC.Get<IMES>();
            Statistic = _cacheParamManager.HomeUiParam.Statistic;
            CT = _globalVariable.CT;
            Initialization();
            UpdateUI();
        }

        public void Initialization()
        {
            ToDayMachineStatusCollection = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title = hiveStatusToStringConvert.Convert(EN_HiveStatus.Running, null, null, null) as string,
                    Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0 },
                    StackMode = StackMode.Values,
                    Fill = hiveStatusToColorConvert.Convert(EN_HiveStatus.Running, null, null, null) as Brush,
                    DataLabels = true,
                    MaxColumnWidth = 80,
                },
                new StackedColumnSeries
                {
                    Title = hiveStatusToStringConvert.Convert(EN_HiveStatus.Idle, null, null, null) as string,
                    Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0 },
                    StackMode = StackMode.Values,
                    Fill = hiveStatusToColorConvert.Convert(EN_HiveStatus.Idle, null, null, null) as Brush,
                    DataLabels = true,
                    MaxColumnWidth = 80,
                },
                new StackedColumnSeries
                {
                    Title = hiveStatusToStringConvert.Convert(EN_HiveStatus.Downtime, null, null, null) as string,
                    Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0 },
                    StackMode = StackMode.Values,
                    Fill = hiveStatusToColorConvert.Convert(EN_HiveStatus.Downtime, null, null, null) as Brush,
                    DataLabels = true,
                    MaxColumnWidth = 80,
                }
            };
            ChoiceTimeSpanErrorStatistics = new SeriesCollection
            {
                new StackedRowSeries
                {
                    Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0 },
                    StackMode = StackMode.Values,
                    Fill = new BrushConverter().ConvertFromString("#e55953") as Brush,
                    DataLabels = true,
                }
            };
            Formatter = value => (value * 100.0).ToString("F2") + "%";
            XFormatter = value => value.ToString();
            PointLabel = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);
            QueryHistoryError();
        }

        /// <summary>
        /// 实时刷新Hive界面的机器状态
        /// </summary>
        public void UpdateUI()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(500);
                    double[,] data = new double[3, 7];
                    try
                    {
                        DateTime time = DateTime.Now;
                        for (int i = 0; i < 7; i++)
                        {
                            //i=0表示6天前，i=6表示今日
                            TimeLabels[i] = time.AddDays(-6 + i).ToString("yyyyMMdd");
                            for (int j = 1; j <= 5; j++)
                            {
                                data[Math.Min(j - 1, 2), i] += _hive_Component.hiveStatusTime[i].machineStatusTime[j] / 86400;
                            }
                        }
                        data[Math.Min((int)_hive_Component.HiveStatus - 1, 2), 6]
                        += _hive_Component.hiveStatusTime[6].machineStatusTime[(int)_hive_Component.HiveStatus] / 86400
                        + (time - _hive_Component.lastChangeStatusTime).TotalSeconds / 86400;
                    }
                    catch (Exception ex)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 7; j++)
                        {
                            ToDayMachineStatusCollection[i].Values[j] = data[i, j];
                        }
                    }
                    NotifyOfPropertyChange(() => InputOutput);
                    NotifyOfPropertyChange(() => OkRate);
                    NotifyOfPropertyChange(() => PassFail);
                    NotifyOfPropertyChange(() => UPH);
                    NotifyOfPropertyChange(() => CTCircle);
                    NotifyOfPropertyChange(() => CTCircle);
                    NotifyOfPropertyChange(() => SWVersion);
                    NotifyOfPropertyChange(() => MainSWPath);
                    NotifyOfPropertyChange(() => MSHash);
                    NotifyOfPropertyChange(() => IsHiveConnected);
                    NotifyOfPropertyChange(() => IsMesConnected);
                }
            });
        }

        public void QueryHistoryError()
        {
            try
            {
                //从文件读取指定时间段的所有报警
                var dicErrorCollections = _stepStatus.HiveMachineStatusStatistic.ReadErrorStatistic(StartDate, EndDate);
                //报警个数显示到页面
                ChoiceTimeSpanErrorStatistics[0].Values.Clear();
                int i = 0;
                foreach (var item in dicErrorCollections)
                {
                    if (i >= dicErrorCollections.Count)
                    {
                        break;
                    }
                    ErrorCodeTypeLabels[i] = item.Key;
                    i++;
                    ChoiceTimeSpanErrorStatistics[0].Values.Add(item.Value);
                }
                for (; i < ErrorCodeTypeLabels.Length; i++)
                {
                    ErrorCodeTypeLabels[i] = "";
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
            }
        }

        /// <summary>
        /// 打开文件夹
        /// </summary>
        /// <param name="message"></param>
        public void OpenDialog(string message)
        {
            string path = "";
            if (message == "HiveLog")
            {
                path = @"D:\QKProject\Data\Hive";
            }
            if (message == "MachineLog")
            {
                //path = $@"{AppDomain.CurrentDomain.BaseDirectory}/Logs/";
                path = @"D:\QKProject\Logs";
            }
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                Process.Start(path);
            }
            catch (Exception ex)
            {
                //do nothing
            }
        }

        public void Handle(ObservableCollection<HiveParamDashBoard> message)
        {
            ParamDashBoard = message;
        }
    }
}
