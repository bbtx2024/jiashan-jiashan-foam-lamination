using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using Caliburn.Micro;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.Scanner;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Steps;

namespace QA.SpotCheckPages.ViewModels
{
    [Export("SpotCheckScannerPageViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckScannerPageViewModel : Screen, INotifyPropertyChanged, ISpotPageViewModel
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private Scanner_TcpComponent _scanner_Component;
        private MES_Component _mes_Component;
        private MotionGoogol_Component _mGoogol_Component;
        private Hive_Component _hive_Component;
        private StepStatus _stepStatus;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "扫码点检";

        private ushort _OrderID = 2;
        public ushort OrderID
        {
            get { return _OrderID; }
            set { _OrderID = value; }
        }

        private string _carrierSN = "";
        public string CarrierSN
        {
            get => _carrierSN;
            set
            {
                //DateTime time = DateTime.Now;
                //if ((time - inputTime).TotalMilliseconds <= 20 || value.Length <= 1)
                //{
                //    _carrierSN = value;
                //}
                //else if (value.Length - _carrierSN.Length == 1)
                //{
                //    //找到增加的位置
                //    char[] oldChars = _carrierSN.ToCharArray();
                //    char[] currChars = value.ToCharArray();
                //    bool find = false;
                //    for (int i = 0; i < oldChars.Length; i++)
                //    {
                //        if (oldChars[i] != currChars[i])
                //        {
                //            _carrierSN = new string(currChars[i], 1);
                //            find = true;
                //            break;
                //        }
                //    }
                //    if (!find)
                //    {
                //        _carrierSN = new string(currChars[currChars.Length - 1], 1);
                //    }
                //    //100ms后，如果长度小于5，自动重置，防止很快的按键盘
                //    _ = Task.Run(() =>
                //    {
                //        Thread.Sleep(100);
                //        if (_carrierSN.Length <= 5)
                //        {
                //            _carrierSN = "";
                //            NotifyOfPropertyChange(() => CarrierSN);
                //        }
                //    });
                //}
                //else
                //{
                //    _carrierSN = "!!!Error!!!";
                //}
                //inputTime = time;
                //NotifyOfPropertyChange(() => CarrierSN);
                _carrierSN = value;
                NotifyOfPropertyChange(() => CarrierSN);
            }
        }

        private string[] _sipSNs = new string[12];
        public string[] SipSNs
        {
            get => _sipSNs;
            set
            {
                _sipSNs = value;
                NotifyOfPropertyChange(() => SipSNs);
            }
        }

        private string _alert_Bumper_SN = "";
        public string Alert_Bumper_SN
        {
            get => _alert_Bumper_SN;
            set
            {
                _alert_Bumper_SN = value;
                NotifyOfPropertyChange(() => Alert_Bumper_SN);
            }
        }

        private string _flex_GND_Tape_SN = "";
        public string Flex_GND_Tape_SN
        {
            get => _flex_GND_Tape_SN;
            set
            {
                _flex_GND_Tape_SN = value;
                NotifyOfPropertyChange(() => Flex_GND_Tape_SN);
            }
        }

        private string[] _routings = new string[12];
        public string[] Routings
        {
            get => _routings;
            set
            {
                _routings = value;
                NotifyOfPropertyChange(() => Routings);
            }
        }

        private string[] _results = new string[12];
        public string[] Results
        {
            get => _results;
            set
            {
                _results = value;
                NotifyOfPropertyChange(() => Results);
            }
        }

        private string _queryTapeSN = "";
        public string QueryTapeSN
        {
            get => _queryTapeSN;
            set
            {
                _queryTapeSN = value;
                NotifyOfPropertyChange(() => QueryTapeSN);
            }
        }

        private string _tapeUseCount = "";
        public string TapeUseCount
        {
            get => _tapeUseCount;
            set
            {
                _tapeUseCount = value;
                NotifyOfPropertyChange(() => TapeUseCount);
            }
        }
        #endregion

        public SpotCheckScannerPageViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _scanner_Component = (Scanner_TcpComponent)IoC.Get<IScanner>();
            _mes_Component = (MES_Component)IoC.Get<IMES>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _stepStatus = IoC.Get<StepStatus>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
        }

        #region Method

        /// <summary>
        /// 载具扫码
        /// </summary>
        public void ScanCarrierSN()
        {
            _mGoogol_Component.SetUpLightTZ(true);
            Thread.Sleep(100);
            if (!_scanner_Component.DataManManualTriger(out string sn))
            {
                _carrierSN = "扫载具码异常";
                NotifyOfPropertyChange(() => CarrierSN);
                _mGoogol_Component.SetUpLightTZ(false);
                return;
            }
            _carrierSN = sn;
            NotifyOfPropertyChange(() => CarrierSN);
            _mGoogol_Component.SetUpLightTZ(false);
        }

        public bool GetSipSNs()
        {
            SipSNs = new string[12];
            Routings = new string[12];
            Results = new string[12];
            if (!_mes_Component.GetSipSNs(CarrierSN, out string[] sipsns))
            {
                _sipSNs[0] = "获取所有Sip码通信异常";
                NotifyOfPropertyChange(() => SipSNs);
                return false;
            }
            for (int i = 0; i < sipsns.Length; i++)
            {
                SipSNs[i] = sipsns[i];
            }
            NotifyOfPropertyChange(() => SipSNs);
            return true;
        }

        public bool GetRoutings()
        {
            Routings = new string[12];
            Results = new string[12];
            //向Routings和cavArr写入数据
            int[] cavArr = new int[12];
            for (int i = 0; i < 12; i++)
            {
                if (string.IsNullOrEmpty(SipSNs[i]))
                {
                    continue;
                }
                if (!_mes_Component.GetRoutingAndCav(SipSNs[i], out EN_Routing_Result routing, out cavArr[i]))
                {
                    Routings[i] = "获取路由及穴位通信异常";
                    NotifyOfPropertyChange(() => Routings);
                    return false;
                }
                if (routing == EN_Routing_Result.OK)
                {
                    Routings[i] = "路由检查OK";
                }
                else if (routing == EN_Routing_Result.HasUploaded)
                {
                    Routings[i] = "SIP板已经绑定，无需再绑";
                }
                else if (routing == EN_Routing_Result.NotThisStation)
                {
                    Routings[i] = "SIP板当前工序不是本站";
                }
                else
                {
                    Routings[i] = "其他错误，请查看日志";
                }
            }
            NotifyOfPropertyChange(() => Routings);
            //调整次序
            string[] tempSip = new string[12];
            string[] tempRouting = new string[12];
            bool cavErr = false;
            for (int i = 0; i < 12; i++)
            {
                if (string.IsNullOrEmpty(SipSNs[i]))
                {
                    continue;
                }
                int realCav = cavArr[i];
                if (realCav <= 0 || realCav > 12)
                {
                    cavErr = true;
                    Routings[i] += "；无穴位信息";
                    continue;
                }
                tempSip[realCav - 1] = SipSNs[i];
                tempRouting[realCav - 1] = Routings[i];
            }
            if (cavErr)
            {
                return false;
            }
            SipSNs = tempSip;
            Routings = tempRouting;
            return true;
        }

        public void ManualBindAll()
        {
            if (string.IsNullOrEmpty(Alert_Bumper_SN))
            {
                Results[0] = $"未输入Alert Bumper SN";
                NotifyOfPropertyChange(() => Results);
                return;
            }
            if (string.IsNullOrEmpty(Flex_GND_Tape_SN))
            {
                Results[0] = $"未输入Flex GND Tape SN";
                NotifyOfPropertyChange(() => Results);
                return;
            }
            Results = new string[12];
            for (int i = 0; i < 12; i++)
            {
                if (string.IsNullOrEmpty(SipSNs[i]) || string.IsNullOrEmpty(Routings[i]) || !Routings[i].Contains("OK"))
                {
                    continue;
                }
                else if (!_mes_Component.BindOne(CarrierSN, i + 1, SipSNs[i], Alert_Bumper_SN, Flex_GND_Tape_SN))
                {
                    Results[i] = "绑定失败";
                }
                else
                {
                    Results[i] = "绑定成功";
                }
            }
            NotifyOfPropertyChange(() => Results);
        }

        public void OnceClickQueryBind()
        {
            if (string.IsNullOrEmpty(CarrierSN))
            {
                CarrierSN = "需先获取载具SN";
                return;
            }
            if (GetSipSNs() && GetRoutings())
            {
                ManualBindAll();
            }
        }

        public void QueryTapeUseCount()
        {
            if (QueryTapeSN == "")
            {
                TapeUseCount = "需先输入SN";
            }
            else
            {
                TapeUseCount = _stepStatus.CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(QueryTapeSN).ToString();
            }
        }

        public void HivecTest()
        {
            _hive_Component.SendMachineData("1234567890", true, DateTime.Now.AddMinutes(-1), DateTime.Now);
            _hive_Component.SendTossingInfo(DateTime.Now.AddMinutes(-1), DateTime.Now, 1, EN_TossingCode.BreakVacuum);
        }

        #endregion
    }
}
