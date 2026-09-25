/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：luanliang
 * 创建日期：2022-04-28
 * 说明：（螺丝电批通讯逻辑）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using CSSIModbus;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls;
using QA_Infrastructure.NLogOut;


namespace QA.Business.Component.ScrewDriver
{
    public class ScrewDriver_Component : IScrewDriver
    {
        private ScrewDriverParam _screwDriverParam;
        private volatile CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private int _clientId;
        private ushort[] _readValue = new ushort[256];
        private ushort[] _writeValue = new ushort[256];
        private bool[] _readWriteBools = new bool[256];
        public IParam Param { get; set; }

        public ushort[] ReadUshorts
        {
            get { return _readValue; }
            set { _readValue = value; }
        }

        public ushort[] WriteUshorts
        {
            get { return _writeValue; }
            set { _writeValue = value; }
        }

        public event Action<ushort[]> ReadValueRefresh;
        //public event Action<ushort[]> WriteValueRefresh;

        public string ComponentName { get; set; }
        public bool IsConnected { get; set; }

        public ScrewDriver_Component()
        {
            _screwDriverParam = IoC.Get<ScrewDriverParam>();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool Initial(IParam param)
        {
            Param = _screwDriverParam = param as ScrewDriverParam;
            return true;
        }

        private bool Connect(ref int clientId)
        {
            try
            {
                if (!_screwDriverParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_screwDriverParam.ComPort) ||
                        !SerialPortCtrl.GetPortNames().Contains(_screwDriverParam.ComPort))
                    {
                        return false;
                    }
                    return CSSIModbusRTUInterface.CSSIModbusRTU_InitSerialPort(_screwDriverParam.ComPort);
                }
                clientId = CSSIModbusTCPInterface.CSSIModbusTCP_InitTcpClient(_screwDriverParam.Ip, _screwDriverParam.Port, 500);
                return clientId != -1;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            Task.Factory.StartNew(() =>
              {
                  while (true)
                  {
                      //var delayTime = _screwDriverParam.BUse ? 10 : 1000;
                      //try
                      //{
                      //    if (_cancellationToken.IsCancellationRequested)
                      //    {
                      //        return;
                      //    }
                      //    if (_screwDriverParam.BUse)
                      //    {
                      //        if (!IsConnected)
                      //        {
                      //            IsConnected = Connect(ref _clientId);
                      //        }
                      //        //Parallel.For(0, _readWriteBools.Length, t =>
                      //        //    {
                      //        //        if (t == 0)
                      //        //        {
                      //        //            _readWriteBools[t] = GetAddrMutiValue((byte)_screwDriverParam.StationId, _screwDriverParam.FromPLC_StartAddr, _screwDriverParam.FromPLC_AddrNum, ref _readValue);
                      //        //        }
                      //        //        if (t == 1)
                      //        //        {
                      //        //            _readWriteBools[t] = GetAddrMutiValue((byte)_screwDriverParam.StationId, _screwDriverParam.ToPLC_StartAddr, _screwDriverParam.ToPLC_AddrNum, ref _writeValue);
                      //        //        }
                      //        //    });
                      //        if (_readWriteBools.Any(t => !t))
                      //        {
                      //            IsConnected = false;
                      //        }
                      //        else
                      //        {
                      //            OnReadValueRefresh(_readValue);
                      //        }
                      //    }
                      //    }
                      //    catch (Exception e)
                      //    {
                      //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                      //    }
                      //    Thread.Sleep(delayTime);
                  }
              }, _cancellationToken);

            return true;
        }
        public bool Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                if (!_screwDriverParam.BUseModbusTcp)
                {
                    CSSIModbus.CSSIModbusRTUInterface.CSSIModbusRTU_Exit();
                }
                else
                {
                    CSSIModbus.CSSIModbusTCPInterface.CSSIModbusTCP_Exit();
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return true;
        }

        #region Modbus基础指令、地址读写、字符读写       
        public bool GetAddrValue(byte id, ushort addr, ref ushort value)
        {
            try
            {
                if (!_screwDriverParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_screwDriverParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_screwDriverParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_GetAddrValue(_screwDriverParam.ComPort, id, addr, true, ref value);
                }

                return CSSIModbusTCPInterface.CSSIModbusTCP_GetAddrValue(_clientId, id, addr, true, ref value); ;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }
        public bool GetAddrMutiValue(byte id, ushort addr, ushort num, ref ushort[] valueUshorts)
        {
            try
            {
                if (!_screwDriverParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_screwDriverParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_screwDriverParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_GetAddrMutiValue(_screwDriverParam.ComPort, id, addr, true, num, valueUshorts);
                }
                return CSSIModbusTCPInterface.CSSIModbusTCP_GetAddrMutiValue(_clientId, id, addr, true, num, valueUshorts);
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }
        public bool GetAddrStringValue(byte id, ushort addr, ushort num, ref string str)
        {
            bool ret = false;
            ushort[] value = new ushort[50];
            try
            {
                if (!_screwDriverParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_screwDriverParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_screwDriverParam.ComPort))
                    {
                        return false;
                    }
                    ret = CSSIModbus.CSSIModbusRTUInterface.CSSIModbusRTU_GetAddrMutiValue(_screwDriverParam.ComPort, id, addr, true, num, value);
                }
                else
                {
                    ret = CSSIModbus.CSSIModbusTCPInterface.CSSIModbusTCP_GetAddrMutiValue(_clientId, id, addr, true, num, value);
                }
                byte[] bytes = new byte[value.Length * 2];
                System.Buffer.BlockCopy(value, 0, bytes, 0, value.Length);
                str = Encoding.ASCII.GetString(bytes, 0, bytes.Length).Trim('\0');
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return ret;
        }
        public bool SetAddrValue(byte id, ushort addr, ushort value)
        {
            try
            {
                if (!_screwDriverParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_screwDriverParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_screwDriverParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_SetAddrValue(_screwDriverParam.ComPort, id, addr, value);
                }

                return CSSIModbusTCPInterface.CSSIModbusTCP_SetAddrValue(_clientId, id, addr, value);
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }
        public bool SetAddrMultiValue(byte id, ushort addr, ushort[] value)
        {
            try
            {
                if (!_screwDriverParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_screwDriverParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_screwDriverParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_SetAddrMutiValue(_screwDriverParam.ComPort, id, addr, value);
                }

                return CSSIModbusTCPInterface.CSSIModbusTCP_SetAddrMutiValue(_clientId, id, addr, value);
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }
        public bool SetAddrStringValue(byte id, ushort addr, ushort num, string str)
        {
            try
            {
                byte[] sndata = Encoding.ASCII.GetBytes(str);
                ushort[] value = new ushort[sndata.Length / 2 + 2];
                System.Buffer.BlockCopy(sndata, 0, value, 0, sndata.Length);

                if (!_screwDriverParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_screwDriverParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_screwDriverParam.ComPort))
                    {
                        return false;
                    }
                    return CSSIModbus.CSSIModbusRTUInterface.CSSIModbusRTU_SetAddrMutiValue(_screwDriverParam.ComPort, id, addr, value);
                }
                else
                {
                    return CSSIModbus.CSSIModbusTCPInterface.CSSIModbusTCP_SetAddrMutiValue(_clientId, id, addr, value);
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }
        #endregion

        protected virtual void OnReadValueRefresh(ushort[] obj)
        {
            ReadValueRefresh?.Invoke(obj);
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (_screwDriverParam.BUse)
            {
                if (!IsConnected)
                {
                    var count = alarmInfos.Count;
                    alarmInfos.Add(new AlarmInfoModel()
                    {
                        AlarmLevel = EN_WARN_LEVEL.Error,
                        AlarmModule = EN_WarnModules.Plc,
                        AlarmMsg = "无连接",
                        Datetime = DateTime.Now,
                        ErrorCode = 0,
                        Index = count
                    });
                }
            }
        }

        /// <summary>
        /// 向PLC中写入报警
        /// </summary>
        /// <param name="stationindex">工站</param>
        /// <param name="addr">站号</param>
        /// <param name="error">报警信息</param>
        /// <returns></returns>
        public bool SetAlarm(En_Plc_MasterError error)
        {
            return SetAddrValue(_screwDriverParam.StationId, 2000, (ushort)error);
        }
        public bool SetScrewBatchStartRotate()
        {
            return SetAddrValue(0x01, 2150, 1);
        }
        public bool SetScrewBatchStopRotate()
        {
            return SetAddrValue(0x01, 2150, 1);
        }
        public bool SetScrewBatchResetToZero()
        {
            return SetAddrValue(0x01, 2150, 1);
        }

    }
}
