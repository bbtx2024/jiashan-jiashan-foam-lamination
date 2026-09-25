using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Manager;

namespace QA.Business
{
    public class GlobalVariable : PropertyChangedBase
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        public ParamManager ParamManager { get; set; }
        public TaskManager TaskManager { get; set; }
        public RecipeManager RecipeManager { get; set; }
        public CacheParamManager CacheParamManager { get; set; }

        private bool _isPlcScanReady = false;  //PLC扫码到位信号
        public bool IsPlcScanReady
        {
            get { return _isPlcScanReady; }
            set { _isPlcScanReady = value; NotifyOfPropertyChange(() => IsPlcScanReady); }
        }

        private bool _isPlcVisionReady = false;  //PLC视觉到位信号
        public bool IsPlcVisionReady
        {
            get { return _isPlcVisionReady; }
            set { _isPlcVisionReady = value; NotifyOfPropertyChange(() => IsPlcVisionReady); }
        }

        private bool _isPlcSolderReady = false;  //PLC焊接到位信号
        public bool IsPlcSolderReady
        {
            get { return _isPlcSolderReady; }
            set { _isPlcSolderReady = value; NotifyOfPropertyChange(() => IsPlcSolderReady); }
        }

        public HomeUiStatus_CT CT { get; set; } = new HomeUiStatus_CT();
        public ConnectState ConnectState { get; set; } = new ConnectState();

        public GlobalVariable()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            ParamManager = IoC.Get<ParamManager>();
            TaskManager = IoC.Get<TaskManager>();
            RecipeManager = IoC.Get<RecipeManager>();
            CacheParamManager = IoC.Get<CacheParamManager>();
        }
    }

    /// <summary>
    /// 存储临时统计数据，如ct等
    /// </summary>
    public class HomeUiStatus_CT : INotifyPropertyChanged
    {
        #region NotifyOfPropertyChange
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            MemberExpression member = (MemberExpression)property.Body;
            string propName = member.Member.Name;
            NotifyOfPropertyChange(propName);
        }
        #endregion

        #region 没用的
        private float _visionCT;
        public float VisionCT
        {
            get { return _visionCT; }
            set { _visionCT = value; NotifyOfPropertyChange(() => VisionCT); }
        }

        private float _heightCT;
        public float HeightCT
        {
            get { return _heightCT; }
            set { _heightCT = value; NotifyOfPropertyChange(() => HeightCT); }
        }

        private float _solderCT;
        public float SolderCT
        {
            get { return _solderCT; }
            set { _solderCT = value; NotifyOfPropertyChange(() => SolderCT); }
        }
        #endregion

        #region ct相关
        private float _totalCT;
        public float TotalCT
        {
            get { return _totalCT; }
            set { _totalCT = value; NotifyOfPropertyChange(() => TotalCT); }
        }

        private float _cycleCT;
        public float CycleCT
        {
            get { return _cycleCT; }
            set { _cycleCT = value; NotifyOfPropertyChange(() => CycleCT); }
        }

        //进料时间点
        public DateTime startTime = DateTime.Now;
        //放料时间点
        public DateTime endTime = DateTime.Now;

        public void SetStartTime()
        {
            DateTime startTime0 = DateTime.Now;
            CycleCT = (float)(startTime0 - startTime).TotalSeconds;
            startTime = startTime0;
        }

        public void SetEndTime()
        {
            endTime = DateTime.Now;
            TotalCT = (float)(endTime - startTime).TotalSeconds;
        }
        #endregion
    }

    public class ConnectState : PropertyChangedBase
    {
        public En_DeviceStatus _isPlcConnected = En_DeviceStatus.DisConnected; //Plc连接
        public En_DeviceStatus IsPlcConnected
        {
            get { return _isPlcConnected; }
            set { _isPlcConnected = value; NotifyOfPropertyChange(() => IsPlcConnected); }
        }

        private En_DeviceStatus _isMotionConnected; //Motoin连接
        public En_DeviceStatus IsMotionConnected
        {
            get { return _isMotionConnected; }
            set { _isMotionConnected = value; NotifyOfPropertyChange(() => IsMotionConnected); }
        }

        private En_DeviceStatus _isVisionConnected; //视觉连接
        public En_DeviceStatus IsVisionConnected
        {
            get { return _isVisionConnected; }
            set { _isVisionConnected = value; NotifyOfPropertyChange(() => IsVisionConnected); }
        }


        private En_DeviceStatus _isPdcaConnected = En_DeviceStatus.DisConnected; //PDCA连接
        public En_DeviceStatus IsPdcaConnected
        {
            get { return _isPdcaConnected; }
            set { _isPdcaConnected = value; NotifyOfPropertyChange(() => IsPdcaConnected); }
        }

        private En_DeviceStatus _isMesConnected; //MES连接
        public En_DeviceStatus IsMesConnected
        {
            get { return _isMesConnected; }
            set { _isMesConnected = value; NotifyOfPropertyChange(() => IsMesConnected); }
        }

        private En_DeviceStatus _isScannerConnected; //扫码枪连接
        public En_DeviceStatus IsScannerConnected
        {
            get { return _isScannerConnected; }
            set { _isScannerConnected = value; NotifyOfPropertyChange(() => IsScannerConnected); }
        }

    }
}
