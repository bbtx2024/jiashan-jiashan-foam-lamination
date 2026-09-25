using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Component.PLC;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.SpotCheckPages.Models;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.ViewModels
{
    [Export("SpotCheckPlcPageViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckPlcPageViewModel : Screen, INotifyPropertyChanged, ISpotPageViewModel
    {
        private IEventAggregator _eventAggregator = null;
        private IWindowManager _windowManager = null;

        //private ComponentManager _componentManager = null;
        private PLC_Component _plc_Component;
        private ParamManager _paramManager = null;

        private Task _curTask = null;       //当前正在执行的耗时操作

        #region Property
        public override string DisplayName { get; set; } = "Plc点检";

        private ushort _OrderID = 4;
        public ushort OrderID
        {
            get { return _OrderID; }
            set { _OrderID = value; }
        }
        private ManualPlcModel _model { get; set; } = new ManualPlcModel();
        public ManualPlcModel Model
        {
            get { return _model; }
            set { _model = value; NotifyOfPropertyChange(() => Model); }
        }
        #endregion

        public SpotCheckPlcPageViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _windowManager = IoC.Get<WindowManager>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            //_componentManager = IoC.Get<ComponentManager>();
            _paramManager = IoC.Get<ParamManager>();
        }

        public void UpdateUI()
        {

        }

        /// <summary>
        /// 刷新PLC所有读取地址状态
        /// </summary>
        public void RefreshReadAddr()
        {
            ushort startAddr = _paramManager.PLCParam.FromPLC_StartAddr;            //读取起始地址
            ushort addrNum = _paramManager.PLCParam.FromPLC_AddrNum;                //读取地址个数
            ushort[] values = new ushort[addrNum];
            _plc_Component.GetAddrMutiValue(0, startAddr, addrNum, ref values);

            Model.ReadAddrValues.Clear();
            for (int i = 0; i < addrNum; i++)
            {
                Model.ReadAddrValues.Add(new ManualPlcAddrModel()
                {
                    Addr = (ushort)(startAddr + i),
                    Value = values[i],
                });
            }
        }

        /// <summary>
        /// 刷新PLC所有写入地址状态
        /// </summary>
        public void RefreshWriteAddr()
        {
            ushort startAddr = _paramManager.PLCParam.ToPLC_StartAddr;              //写入起始地址
            ushort addrNum = _paramManager.PLCParam.ToPLC_AddrNum;                  //写入地址个数
            ushort[] values = new ushort[addrNum];
            _plc_Component.GetAddrMutiValue(0, startAddr, addrNum, ref values);

            Model.WriteAddrValues.Clear();
            for (int i = 0; i < addrNum; i++)
            {
                Model.WriteAddrValues.Add(new ManualPlcAddrModel()
                {
                    Addr = (ushort)(startAddr + i),
                    Value = values[i],
                });
            }
        }

        /// <summary>
        /// 重新连接PLC
        /// </summary>
        public void ReconnectPLC()
        {
            MessageBoxResult rs = MessageBox.Ask("确认是否执行重新连接PLC？", "ASK");
            if (rs == MessageBoxResult.Yes || rs == MessageBoxResult.OK)
            {
                if (_curTask != null && !_curTask.IsCompleted)
                {
                    MessageBox.Warning("当前还有未完成的任务，请稍后重试", "WARNING");
                    return;
                }
                new TaskFactory().StartNew(() =>
                {
                    _plc_Component.DisConnect();
                    _plc_Component.Connect();
                });
            }
        }

        /// <summary>
        /// 当前地址数据写入PLC
        /// </summary>
        public void WritePlcCurAddr()
        {
            MessageBoxResult rs = MessageBox.Ask("确认是否执行写入PLC地址操作？", "ASK");
            if (rs == MessageBoxResult.Yes || rs == MessageBoxResult.OK)
            {
                ushort addr = Model.SltWriteAddrValue.Addr;
                ushort value = Model.SltWriteAddrValue.Value;

                //LogHelper.Info(LogType.Manual, $"写入PLC当前地址 addr:{addr} value:{value}");
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"写入PLC当前地址 addr:{addr} value:{value}");
                if (!_plc_Component.SetAddrValue(0, addr, value))
                {
                    MessageBox.Error($"写入PLC当前地址失败！ Addr:{addr} Value:{value}", "ERROR");
                }
            }
        }
    }
}
