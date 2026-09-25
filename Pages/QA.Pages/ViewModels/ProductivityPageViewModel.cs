using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Wpf;
using QA.Business;
using QA.Business.CacheParam;
using QA.Business.Component.HIVE;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model;
using QA.Business.Model.RunTimeInfo;
using QA.Business.Station;
using QA.Business.Steps;
using QA.Pages.Interfaces;
using QA.Pages.Models;
using QA.Pages.OtherViews.ViewModels;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Pages.ViewModels
{
    [Export(typeof(IPageViewModel))]
    public class ProductivityPageViewModel : Screen, IPageViewModel, IHandle<List<IComponent>>
    {
        #region Field
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly GlobalVariable _globalVariable;
        private readonly ParamManager _paramManager;
        private readonly CacheParamManager _cacheParamManager;
        private readonly StepStatus _stepStatus;
        private IBaseBiz _baseBiz;
        private Hive_Component _hive_Component;
        #endregion

        #region Property

        public ushort OrderID { get; set; } = 4;

        private ObservableCollection<ComponentShowModel> _componentObservableCollection = new ObservableCollection<ComponentShowModel>();
        public ObservableCollection<ComponentShowModel> ComponentObservableCollection
        {
            get => _componentObservableCollection;
            set
            {
                _componentObservableCollection = value;
                NotifyOfPropertyChange(() => ComponentObservableCollection);
            }
        }

        private bool _enableButtons = false;
        public bool EnableButtons
        {
            get => _enableButtons;
            set
            {
                _enableButtons = value;
                NotifyOfPropertyChange(() => EnableButtons);
            }
        }

        public HomeUiParam_Statistic Statistic { get; private set; }  //长期统计信息，比如ok数目等等
        public HomeUiStatus_CT CT { get; private set; }  //临时统计信息，如CT等临时变量
        public ConnectState ConnectState { get; set; }  //连接状态
        #endregion

        #region Property2
        public Func<ChartPoint, string> PointLabel { get; set; }
        public SeriesCollection ToDayHistogramSeriesCollection { get; set; }
        public SeriesCollection ToDayPieSeriesCollection { get; set; }
        public SeriesCollection ChoiceTimeSlotHistogramSeriesCollection { get; set; }
        public SeriesCollection ChoiceTimeSlotPieSeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<int, string> Formatter { get; set; }
        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public OutputPerHour outputPerHour { get; set; }
        private DateTime _choicetData = DateTime.Now.Date;
        public DateTime ChoicetData
        {
            get
            {
                return _choicetData;
            }
            set
            {
                _choicetData = value;
                NotifyOfPropertyChange(() => ChoicetData);
                QueryHistoryOneDay();
            }
        }
        private DateTime _startData = DateTime.Now.Date;
        public DateTime StartData
        {
            get
            {
                return _startData;
            }
            set
            {
                _startData = value;
                NotifyOfPropertyChange(() => StartData);
                QueryHistoryDays();
            }
        }
        private DateTime _endData = DateTime.Now.Date;
        public DateTime EndData
        {
            get
            {
                return _endData;
            }
            set
            {
                _endData = value;
                NotifyOfPropertyChange(() => EndData);
                QueryHistoryDays();
            }
        }
        private string[] _choiceTimeSlotLabels;
        public string[] ChoiceTimeSlotLabels
        {
            get
            {
                return _choiceTimeSlotLabels;
            }
            set
            {
                _choiceTimeSlotLabels = value;
                NotifyOfPropertyChange(() => ChoiceTimeSlotLabels);
            }
        }
        #endregion

        public ProductivityPageViewModel()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);

            _globalVariable = IoC.Get<GlobalVariable>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _baseBiz = IoC.Get<IBaseBiz>();
            _stepStatus = IoC.Get<StepStatus>();

            Statistic = _cacheParamManager.HomeUiParam.Statistic;
            CT = _globalVariable.CT;
            ConnectState = _globalVariable.ConnectState;
            outputPerHour = _stepStatus.OutputPerHour;
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            Initialization();
            QueryHistoryOneDay();
            QueryHistoryDays();
        }

        #region Caliburn.Micro
        public void Handle(List<IComponent> message)
        {
            EnableButtons = !(_baseBiz as BaseBiz).AutoRun;
            foreach (var item in message)
            {
                if (item is IPLC)
                {
                    if (_paramManager.PLCParam.BUse)
                    {
                        _globalVariable.ConnectState.IsPlcConnected = (item as IPLC).IsConnected ? En_DeviceStatus.Connected : En_DeviceStatus.DisConnected;
                    }
                    else
                    {
                        _globalVariable.ConnectState.IsPlcConnected = En_DeviceStatus.Disabled;
                    }
                }
                else if (item is IMES)
                {
                    if (_paramManager.MESParam.BUse)
                    {
                        _globalVariable.ConnectState.IsMesConnected = (item as IMES).IsConnected ? En_DeviceStatus.Connected : En_DeviceStatus.DisConnected;
                    }
                    else
                    {
                        _globalVariable.ConnectState.IsMesConnected = En_DeviceStatus.Disabled;
                    }
                }
                else if (item is IScanner)
                {
                    if (_paramManager.ScannerParam.BUse)
                    {
                        _globalVariable.ConnectState.IsScannerConnected = (item as IScanner).IsConnected ? En_DeviceStatus.Connected : En_DeviceStatus.DisConnected;
                    }
                    else
                    {
                        _globalVariable.ConnectState.IsScannerConnected = En_DeviceStatus.Disabled;
                    }
                }
                //else if (item is ILaserHeightSensor)
                //{
                //    if (_paramManager.LaserHeightSensorParam.BUse)
                //    {
                //        _globalVariable.ConnectState.IsLaserHeightSensorConnected = (item as ILaserHeightSensor).IsConnected ? En_DeviceStatus.Connected : En_DeviceStatus.DisConnected;
                //    }
                //    else
                //    {
                //        _globalVariable.ConnectState.IsLaserHeightSensorConnected = En_DeviceStatus.Disabled;
                //    }
                //}
                else if (item is ICamera)
                {
                    if (_paramManager.CameraParam.BUse)
                    {
                        _globalVariable.ConnectState.IsVisionConnected = (item as ICamera).IsConnected ? En_DeviceStatus.Connected : En_DeviceStatus.DisConnected;
                    }
                    else
                    {
                        _globalVariable.ConnectState.IsVisionConnected = En_DeviceStatus.Disabled;
                    }
                }
                else if (item is IMGoogol)
                {
                    if (_paramManager.MotionGoogolParam.BUse)
                    {
                        _globalVariable.ConnectState.IsMotionConnected = (item as IMGoogol).IsConnected ? En_DeviceStatus.Connected : En_DeviceStatus.DisConnected;
                    }
                    else
                    {
                        _globalVariable.ConnectState.IsMotionConnected = En_DeviceStatus.Disabled;
                    }
                }
            }
        }
        #endregion

        #region Method
        public void ResetCurrentNum()
        {
            if (MessageBox.Show("确定重置当前产量吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                {
                    Statistic.CurrentNum = 0;
                }
                else
                {
                    Statistic.CurrentdebugNum = 0;
                }
                _cacheParamManager.SaveHomeUiParam();
            }
        }

        public void ResetNozzleCount(int nozzleNo)
        {
            if (nozzleNo == 0)
            {
                if (MessageBox.Show("确定清零所有吸嘴数据吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                    {
                        Statistic.ResetNozzleCount(nozzleNo);
                    }
                    else
                    {
                        Statistic.ResetNozzledebugCount(nozzleNo);
                    }
                    _cacheParamManager.SaveHomeUiParam();
                }
            }
            else
            {
                if (MessageBox.Show("确定清零" + nozzleNo + "号吸嘴数据吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                    {
                        Statistic.ResetNozzleCount(nozzleNo);
                    }
                    else
                    {
                        Statistic.ResetNozzledebugCount(nozzleNo);
                    }
                    _cacheParamManager.SaveHomeUiParam();
                }
            }
        }

        public void Initialization()
        {
            System.Windows.Media.BrushConverter brushConverter = new System.Windows.Media.BrushConverter();
            ToDayHistogramSeriesCollection = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title="OK产量",
                    Values = new ChartValues<int> {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                    StackMode = StackMode.Values, // this is not necessary, values is the default stack mode
                   // Fill=System.Windows.Media.Brushes.Green,Fill="#008D86"
                    Fill =(System.Windows.Media.Brush)brushConverter.ConvertFromString("#008D86"),
                    DataLabels = true
                },
                new StackedColumnSeries
                {
                    Title="NG产量",
                    Values = new ChartValues<int> {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                    StackMode = StackMode.Values,
                   // Fill=System.Windows.Media.Brushes.Red,Fill="#FF6A6A"
                    Fill =(System.Windows.Media.Brush)brushConverter.ConvertFromString("#FF6A6A"),
                    DataLabels = true
                }
            };

            ToDayPieSeriesCollection = new SeriesCollection
            {
                new PieSeries()
                {
                    Title = "OK产量",
                    Values = new ChartValues<int>() { 0 },
                    DataLabels = true,
                    Fill = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#008D86"),
                    LabelPoint = new Func<ChartPoint, string>((chartPoint) =>
                    {
                        return string.Format("{0}{1} ({2:P})", chartPoint.SeriesView.Title, chartPoint.Y, chartPoint.Participation);
                    })
                },
                new PieSeries()
                {
                    Title = "NG产量",
                    Values = new ChartValues<int>() { 0 },
                    DataLabels = true,
                    Fill = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#FF6A6A"),
                    LabelPoint = new Func<ChartPoint, string>((chartPoint) =>
                    {
                        return string.Format("{0}{1} ({2:P})", chartPoint.SeriesView.Title, chartPoint.Y, chartPoint.Participation);
                    })
                }
            };

            ChoiceTimeSlotHistogramSeriesCollection = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title="OK产量",
                    Values = new ChartValues<int>(),
                    StackMode = StackMode.Values, // this is not necessary, values is the default stack mode
                   // Fill=System.Windows.Media.Brushes.Green,Fill="#008D86"
                    Fill =(System.Windows.Media.Brush)brushConverter.ConvertFromString("#008D86"),
                    DataLabels = true
                },
                new StackedColumnSeries
                {
                    Title="NG产量",
                    Values = new ChartValues<int>(),
                    StackMode = StackMode.Values,
                   // Fill=System.Windows.Media.Brushes.Red,Fill="#FF6A6A"
                    Fill =(System.Windows.Media.Brush)brushConverter.ConvertFromString("#FF6A6A"),
                    DataLabels = true
                }
            };

            ChoiceTimeSlotPieSeriesCollection = new SeriesCollection
            {
                new PieSeries()
                {
                    Title = "OK产量",
                    Values = new ChartValues<int>() { 0 },
                    DataLabels = true,
                    Fill = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#008D86"),
                    LabelPoint = new Func<ChartPoint, string>((chartPoint) =>
                    {
                        return string.Format("{0}{1} ({2:P})", chartPoint.SeriesView.Title, chartPoint.Y, chartPoint.Participation);
                    })
                },
                new PieSeries()
                {
                    Title = "NG产量",
                    Values = new ChartValues<int>() { 0 },
                    DataLabels = true,
                    Fill = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#FF6A6A"),
                    LabelPoint = new Func<ChartPoint, string>((chartPoint) =>
                    {
                        return string.Format("{0}{1} ({2:P})", chartPoint.SeriesView.Title, chartPoint.Y, chartPoint.Participation);
                    })
                }
            };
            Labels = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24" };
            Formatter = value => value.ToString("N");
            PointLabel = chartPoint =>
            string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);
        }

        public void QueryHistoryOneDay()
        {
            if (ChoicetData.ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                for (int i = 0; i < 24; i++)
                {
                    ToDayHistogramSeriesCollection[0].Values[i] = _stepStatus.OutputPerHour.ProductionQuantitys[i].OkCount;
                    ToDayHistogramSeriesCollection[1].Values[i] = _stepStatus.OutputPerHour.ProductionQuantitys[i].NgCount;
                }
                ToDayPieSeriesCollection[0].Values[0] = _stepStatus.OutputPerHour.OkCount;
                ToDayPieSeriesCollection[1].Values[0] = _stepStatus.OutputPerHour.NgCount;
            }
            else
            {
                if (outputPerHour.ReadOutputPerHour(_choicetData))
                {
                    for (int i = 0; i < 24; i++)
                    {
                        ToDayHistogramSeriesCollection[0].Values[i] = outputPerHour.ProductionQuantitys[i].OkCount;
                        ToDayHistogramSeriesCollection[1].Values[i] = outputPerHour.ProductionQuantitys[i].NgCount;
                    }
                    ToDayPieSeriesCollection[0].Values[0] = outputPerHour.OkCount;
                    ToDayPieSeriesCollection[1].Values[0] = outputPerHour.NgCount;
                }
                else
                {
                    for (int i = 0; i < 24; i++)
                    {
                        ToDayHistogramSeriesCollection[0].Values[i] = 0;
                        ToDayHistogramSeriesCollection[1].Values[i] = 0;
                    }
                    ToDayPieSeriesCollection[0].Values[0] = 0;
                    ToDayPieSeriesCollection[1].Values[0] = 0;
                }

            }
        }

        public void QueryHistoryDays()
        {
            TimeSpan timeSpan = EndData - StartData;
            if (timeSpan.TotalMilliseconds < 0)
            {
                return;
            }
            ChoiceTimeSlotLabels = new string[timeSpan.Days + 1];
            if (timeSpan.Days + 1 > 90)
            {
                return;
            }
            ChoiceTimeSlotPieSeriesCollection[0].Values[0] = 0;
            ChoiceTimeSlotPieSeriesCollection[1].Values[0] = 0;
            ChoiceTimeSlotHistogramSeriesCollection[0].Values.Clear();
            ChoiceTimeSlotHistogramSeriesCollection[1].Values.Clear();
            int allOKCount = 0;
            int allNGCount = 0;
            for (int i = 0; i < timeSpan.Days + 1; i++)
            {
                ChoiceTimeSlotLabels[i] = StartData.AddDays(i).ToString("MM-dd");
                ChoiceTimeSlotHistogramSeriesCollection[0].Values.Add(0);
                ChoiceTimeSlotHistogramSeriesCollection[1].Values.Add(0);
                if (outputPerHour.ReadOutputPerHour(StartData.AddDays(i)))
                {
                    ChoiceTimeSlotHistogramSeriesCollection[0].Values[i] = outputPerHour.OkCount;
                    ChoiceTimeSlotHistogramSeriesCollection[1].Values[i] = outputPerHour.NgCount;
                    allOKCount += outputPerHour.OkCount;
                    allNGCount += outputPerHour.NgCount;
                }
                else
                {
                    ChoiceTimeSlotHistogramSeriesCollection[0].Values[i] = 0;
                    ChoiceTimeSlotHistogramSeriesCollection[1].Values[i] = 0;
                }
            }
            ChoiceTimeSlotPieSeriesCollection[0].Values[0] = allOKCount;
            ChoiceTimeSlotPieSeriesCollection[1].Values[0] = allNGCount;
        }

        public void OpenFolder()
        {
            System.Diagnostics.Process.Start(_stepStatus.OutputPerHour.BasePath);
        }
        #endregion
    }
}
