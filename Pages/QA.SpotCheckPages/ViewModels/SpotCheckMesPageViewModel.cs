using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Steps;
using static QA.Business.Define.HiveAlarmDefine;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.ViewModels
{
    [Export("SpotCheckMesPageViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckMesPageViewModel : Screen, INotifyPropertyChanged, ISpotPageViewModel, IHandle<LoginSuccessMessage>, IHandle<TapeUseCountChangeMessage>
    {
        #region Field
        private readonly IEventAggregator _eventAggregator;
        private StepStatus _stepStatus;
        private MyUserManager _myUserManager;
        private CacheParamManager _cacheParamManager;
        private Hive_Component _hive_Component;
        #endregion

        #region Property

        public ushort OrderID { get; set; } = 0;
        public override string DisplayName { get; set; } = "MES设定";

        private bool _change1 = false;
        public bool Change1
        {
            get => _change1;
            set
            {
                _change1 = value;
                NotifyOfPropertyChange(() => Change1);
            }
        }
        private bool _allow1 = false;
        public bool Allow1
        {
            get => _allow1;
            set
            {
                _allow1 = value;
                NotifyOfPropertyChange(() => Allow1);
            }
        }
        private bool _save1 = false;
        public bool Save1
        {
            get => _save1;
            set
            {
                _save1 = value;
                NotifyOfPropertyChange(() => Save1);
            }
        }
        private bool _change2 = false;
        public bool Change2
        {
            get => _change2;
            set
            {
                _change2 = value;
                NotifyOfPropertyChange(() => Change2);
            }
        }
        private bool _allow2 = false;
        public bool Allow2
        {
            get => _allow2;
            set
            {
                _allow2 = value;
                NotifyOfPropertyChange(() => Allow2);
            }
        }
        private bool _save2 = false;
        public bool Save2
        {
            get => _save2;
            set
            {
                _save2 = value;
                NotifyOfPropertyChange(() => Save2);
            }
        }

        private string _lineName;
        public string LineName
        {
            get => _lineName;
            set
            {
                _lineName = value;
                NotifyOfPropertyChange(() => LineName);
            }
        }
        private string _stationName;
        public string StationName
        {
            get => _stationName;
            set
            {
                _stationName = value;
                NotifyOfPropertyChange(() => StationName);
            }
        }
        private string _fixid;
        public string Fixid
        {
            get => _fixid;
            set
            {
                _fixid = value;
                NotifyOfPropertyChange(() => Fixid);
            }
        }

        //private int _tapeLength;
        //public int TapeLength
        //{
        //    get => _tapeLength;
        //    set
        //    {
        //        _tapeLength = value;
        //        NotifyOfPropertyChange(() => TapeLength);
        //    }
        //}
        public List<int> TapeLenKinds { get; set; } = new List<int>();
        private int _tapeLenKind;
        public int TapeLenKind
        {
            get => _tapeLenKind;
            set
            {
                _tapeLenKind = value;
                NotifyOfPropertyChange(() => TapeLenKind);
            }
        }
        private string _tapeIPNs;
        public string TapeIPNs
        {
            get => _tapeIPNs;
            set
            {
                _tapeIPNs = value;
                NotifyOfPropertyChange(() => TapeIPNs);
            }
        }
        private string _feeder1TapeSN;
        public string Feeder1TapeSN
        {
            get => _feeder1TapeSN;
            set
            {
                _feeder1TapeSN = value;
                NotifyOfPropertyChange(() => Feeder1TapeSN);
                Handle(new TapeUseCountChangeMessage() {ID = FeederId.左飞达 });
            }
        }
        private string _feeder2TapeSN;
        public string Feeder2TapeSN
        {
            get => _feeder2TapeSN;
            set
            {
                _feeder2TapeSN = value;
                NotifyOfPropertyChange(() => Feeder2TapeSN);
                Handle(new TapeUseCountChangeMessage() { ID = FeederId.右飞达 });
            }
        }
        public List<string> Vendors { get; set; } = new List<string>();
        private string _vendor;
        public string Vendor
        {
            get => _vendor;
            set
            {
                _vendor = value;
                NotifyOfPropertyChange(() => Vendor);
            }
        }
        private int _tapeAlarmMaxUseCount;
        public int TapeAlarmMaxUseCount
        {
            get => _tapeAlarmMaxUseCount;
            set
            {
                _tapeAlarmMaxUseCount = value;
                NotifyOfPropertyChange(() => TapeAlarmMaxUseCount);
            }
        }
        private int _tapeAlarmTipLeftCount;
        public int TapeAlarmTipLeftCount
        {
            get => _tapeAlarmTipLeftCount;
            set
            {
                _tapeAlarmTipLeftCount = value;
                NotifyOfPropertyChange(() => TapeAlarmTipLeftCount);
            }
        }
        private int _feeder1tapeCurrCount;
        public int Feeder1TapeCurrCount
        {
            get => _feeder1tapeCurrCount;
            set
            {
                _feeder1tapeCurrCount = value;
                NotifyOfPropertyChange(() => Feeder1TapeCurrCount);
            }
        }
        private int _feeder2tapeCurrCount;
        public int Feeder2TapeCurrCount
        {
            get => _feeder2tapeCurrCount;
            set
            {
                _feeder2tapeCurrCount = value;
                NotifyOfPropertyChange(() => Feeder2TapeCurrCount);
            }
        }

        private string _tapeName;
        public string TapeName
        {
            get => _tapeName;
            set
            {
                _tapeName = value;
                NotifyOfPropertyChange(() => TapeName);
            }
        }

        #endregion

        public SpotCheckMesPageViewModel()
        {
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _stepStatus = IoC.Get<StepStatus>();
            _myUserManager = IoC.Get<MyUserManager>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            foreach (var value in Enum.GetNames(typeof(EN_Vendor)))
            {
                Vendors.Add(value);
            }
            foreach (var value in Enum.GetValues(typeof(EN_TapeLenKind)))
            {
                TapeLenKinds.Add((int)value);
            }
            ReadParam();
        }

        #region Method

        public void ReadParam()
        {
            LineName = _stepStatus.ParamManager.MESParam.Line;
            StationName = _stepStatus.ParamManager.MESParam.Station;
            Fixid = _stepStatus.ParamManager.MESParam.Fixid;
            //TapeLength = _stepStatus.ParamManager.MESParam.TapeLength;
            TapeLenKind = (int)_stepStatus.ParamManager.MESParam.TapeLenKind;
            TapeIPNs = _stepStatus.ParamManager.MESParam.TapeIPNs;
            FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
            Feeder1TapeSN = _stepStatus.ParamManager.MESParam.Feeder1TapeSN;
            Feeder2TapeSN = _stepStatus.ParamManager.MESParam.Feeder2TapeSN;
            TapeAlarmMaxUseCount = _stepStatus.ParamManager.MESParam.TapeAlarmMaxUseCount;
            TapeAlarmTipLeftCount = _stepStatus.ParamManager.MESParam.TapeAlarmTipLeftCount;
            Feeder1TapeCurrCount = _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder1TapeSN);
            Feeder2TapeCurrCount = _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder2TapeSN);
            TapeName = _stepStatus.ParamManager.MESParam.TapeName;
        }

        public void AllowModifyLeft()
        {
            ReadParam();
            Change1 = true;
            Allow1 = false;
            Save1 = true;
        }

        public void SaveLeftParam()
        {
            _stepStatus.ParamManager.MESParam.Line = LineName;
            //_stepStatus.ParamManager.MESParam.Station = StationName;
            _stepStatus.ParamManager.MESParam.Fixid = Fixid;
            _stepStatus.ParamManager.SaveParam(_stepStatus.ParamManager.MESParam);
            Change1 = false;
            Allow1 = true;
            Save1 = false;
        }

        public void AllowModifyRight()
        {
            ReadParam();
            Change2 = true;
            Allow2 = false;
            Save2 = true;
        }

        public void SaveRightParam()
        {
            //_stepStatus.ParamManager.MESParam.TapeLength = TapeLength;
            _stepStatus.ParamManager.MESParam.TapeLenKind = (EN_TapeLenKind)TapeLenKind;
            _stepStatus.ParamManager.MESParam.TapeIPNs = TapeIPNs;
            _stepStatus.ParamManager.MESParam.TapeAlarmMaxUseCount = TapeAlarmMaxUseCount;
            _stepStatus.ParamManager.MESParam.TapeAlarmTipLeftCount = TapeAlarmTipLeftCount;
            Feeder1TapeCurrCount = _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder1TapeSN);
            Feeder2TapeCurrCount = _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder2TapeSN);
            _stepStatus.ParamManager.MESParam.TapeName = TapeName;
            _stepStatus.ParamManager.SaveParam(_stepStatus.ParamManager.MESParam);
            Change2 = false;
            Allow2 = true;
            Save2 = false;
        }


        public void ResetFeeder1TapeUseCount()
        {
            if (MessageBox.Show($"确定清除卷料{Feeder1TapeSN}的使用次数吗？\n当前使用次数为{Feeder1TapeCurrCount}", "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.ResetCountBySn(Feeder1TapeSN);
                MessageBox.Info($"已清除卷料{Feeder1TapeSN}的使用次数！");
            }
        }
        public void ResetFeeder2TapeUseCount()
        {
            if (MessageBox.Show($"确定清除卷料{Feeder2TapeSN}的使用次数吗？\n当前使用次数为{Feeder2TapeCurrCount}", "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.ResetCountBySn(Feeder2TapeSN);
                MessageBox.Info($"已清除卷料{Feeder2TapeSN}的使用次数！");
            }
        }

        public void Handle(TapeUseCountChangeMessage message)
        {
            //Upade
            switch (message.ID)
            {
                case FeederId.左飞达:
                    Feeder1TapeCurrCount = _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder1TapeSN);
                    break;
                case FeederId.右飞达:
                    Feeder2TapeCurrCount = _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(Feeder2TapeSN);
                    break;
                default:
                    break;
            }

        }

        public void Handle(LoginSuccessMessage message)
        {
            ReadParam();
            Change1 = false;
            Allow1 = _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeManager;
            Save1 = false;
            Change2 = false;
            Allow2 = _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeEngineer;
            Save2 = false;
        }

        #endregion
    }
}
