using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls;
using QA_Infrastructure.BaseCtrls.ParamEnum;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Component.Monitor
{
    public class Monitor_SerialComponent : SerialPortCtrl, IMonitor
    {
        #region Field
        private MonitorParam _monitorParam;
        private SerialPortParam _serialPortParam;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private int _supplyMaterialCount = 0;
        //private ManualResetEvent resetEvent = new ManualResetEvent(false);
        #endregion
        #region Property
        public IParam Param { get; set; }
        public string ComponentName { get; set; }
        public bool IsConnected { get; set; }
        #endregion

        public event Action<string, float[]> ReadValueRefresh;
        public event Action<string, string[]> ReadStrValueRefresh;

        public Monitor_SerialComponent()
        {
            _monitorParam = IoC.Get<MonitorParam>();
            _serialPortParam = new SerialPortParam();
        }

        public bool Initial(IParam param)
        {
            try
            {
                Param = _monitorParam = param as MonitorParam;
                _serialPortParam.Com = _monitorParam.PortName;
                _serialPortParam.BaudRate = _monitorParam.Baudrate;
                _serialPortParam.DataBit = _monitorParam.DataBit;
                _serialPortParam.StopBits = _monitorParam.StopBits;
                _serialPortParam.Parity = _monitorParam.Parity;
                _serialPortParam.OrderTimeOut = _monitorParam.TimeOut;
                _serialPortParam.ReadTimeOut = _monitorParam.TimeOut;
                _serialPortParam.WriteTimeOut = _monitorParam.TimeOut;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e + e.StackTrace);
                return false;
            }
            return true;
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var delayTime = _monitorParam.BUse ? 100 : 1000;
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (_monitorParam.BUse)
                        {
                            if (!IsConnected)
                            {
                                if (IsOpen())
                                    ClosePort();
                                IsConnected = OpenPort(_serialPortParam);
                            }

                            if (!GetMonitorValue(out float[] floatVals, out string[] stringVals))
                            {
                                IsConnected = false;
                            }
                            else
                            {
                                //OnReadValueRefresh(floatVals);
                                OnReadValueRefresh(stringVals);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                    }

                    await Task.Delay(delayTime, _cancellationToken);
                }
            }, _cancellationToken);

            return true;
        }

        public bool Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return true;
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (_monitorParam.BUse)
            {
                if (!IsConnected)
                {
                    var count = alarmInfos.Count;
                    alarmInfos.Add(new AlarmInfoModel()
                    {
                        AlarmLevel = EN_WARN_LEVEL.Error,
                        AlarmModule = EN_WarnModules.Temp378,
                        AlarmMsg = "无连接",
                        Datetime = DateTime.Now,
                        ErrorCode = 0,
                        Index = count
                    });
                }
            }
        }

        public bool GetMonitorValue(out float[] floatVals, out string[] stringVals)
        {
            floatVals = new float[] { };
            stringVals = new string[_monitorParam.ReadLength];
            for (int i = 0; i < stringVals.Length; i++)
            {
                stringVals[i] = "0";
            }

            for (int i = 0; i < _monitorParam.ReadLength; i++)
            {
                var cmd = $"{Convert.ToString(_monitorParam.StartAddress + i, 16)} 03 00 01 00 01";
                var splitchars = cmd.Split(' ').ToList();
                List<byte> byteLists = new List<byte>();
                splitchars.ForEach(t => byteLists.Add(Convert.ToByte(t, 16)));
                var modbuscrc16 = CRC16.GETCRC16(byteLists.ToArray(), byteLists.Count);
                var modbuscrc16Bytes = BitConverter.GetBytes(modbuscrc16).Reverse();
                byteLists.AddRange(modbuscrc16Bytes);

                ClearReadBuffer();
                SendComBytesWhileEnd(byteLists.ToArray(), 100);
                var getBytes = new byte[100];
                int getLength = GetComByteArray(getBytes, 15, 100);
                if (getLength > 0)
                {
                    var getHexStrs = BitConverter.ToString(getBytes);
                    List<byte> getByteLists = new List<byte>();
                    getHexStrs.Split('-').Skip(3).Take(2).ToList().ForEach(t => getByteLists.Add(Convert.ToByte(t, 16)));
                    getByteLists.Reverse();
                    stringVals[i] = BitConverter.ToInt16(getByteLists.ToArray(), 0).ToString();
                }
            }
            return true;
        }
        /// <summary>
        /// 获取补给区域物料的个数
        /// </summary>
        /// <returns></returns>
        public int GetSupplyMaterialCount()
        {
            return _supplyMaterialCount;
        }
        protected virtual void OnReadValueRefresh(float[] obj)
        {
            ReadValueRefresh?.Invoke(ComponentName, obj);
        }

        protected virtual void OnReadValueRefresh(string[] obj)
        {
            ReadStrValueRefresh?.Invoke(ComponentName, obj);
        }
    }
}
