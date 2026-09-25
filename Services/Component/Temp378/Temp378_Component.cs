using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls;
using QA_Infrastructure.BaseCtrls.ParamEnum;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Component.Temp378
{
    public class Temp378_Component : SerialPortCtrl, ITemp378
    {
        //SerialPortCtrl
        #region Field
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private SerialPortParam _serialPortParam = new SerialPortParam();
        private Temp378Param _temp378Param;
        private byte _errorCode = 0;
        //private bool _isAlarmTemp = false;
        //private bool _isCurAlarmTemp = false;
        private ushort _curSetTemp = 0;
        private ushort _curTemp = 0;
        private bool _isSleepState = false;
        #endregion

        #region Property
        public string ComponentName { get; set; } = "Temp378";
        public bool IsConnected { get; set; }
        public virtual IParam Param { get; set; }
        public ushort CurTemp { get; internal set; }
        public ushort CurSetTemp { get; internal set; }
        public bool SleepState { get; internal set; }
        public bool Pause { get; set; }
        #endregion

        //public event Action TrigTempAlarm;
        public event Action TrigTempSignalPause;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool Initial(IParam param)
        {
            Param = _temp378Param = (param as Temp378Param).DeepCopy();
            if (_temp378Param != null)
            {
                _serialPortParam.Com = _temp378Param.PortName;
                _serialPortParam.BaudRate = _temp378Param.Baudrate;
                _serialPortParam.DataBit = 8;
                _serialPortParam.StopBits = System.IO.Ports.StopBits.One;
                _serialPortParam.Parity = System.IO.Ports.Parity.None;
                _serialPortParam.OrderTimeOut = 500;
                _serialPortParam.ReadTimeOut = 500;
                _serialPortParam.WriteTimeOut = 500;
                _serialPortParam.ReTryCount = 3;
            }
            return true;
        }
        public bool Connect()
        {
            ClosePort();
            if (OpenPort(_serialPortParam))
            {
                IsConnected = true;
            }
            else
            {
                ClosePort();
                IsConnected = false;
            }
            return IsConnected;
        }
        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                Connect();
                while (true)
                {
                    var delayTime = _temp378Param.BUse ? 10 : 1000;
                    await Task.Delay(delayTime, _cancellationToken);
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (Pause)
                            continue;

                        if (!Param.BUse)
                        {
                            continue;
                        }

                        IsConnected = GetSleepState(ref _isSleepState);
                        SleepState = _isSleepState;

                        if (!IsConnected)
                        {
                            Connect();
                            Thread.Sleep(1000);
                            CurTemp = 0;
                            CurSetTemp = 0;
                            continue;
                        }
                        IsConnected = GetIronTemp(ref _curTemp);
                        CurTemp = _curTemp;

                        GetSetIronTemp(ref _curSetTemp);
                        CurSetTemp = _curSetTemp;

                        GetAlarmState(ref _errorCode);
                    }
                    catch (Exception e)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
                    }

                }
            }, _cancellationToken);
            return true;
        }
        public bool Stop()
        {
            try
            {
                //base.Close();
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
            }
            return true;
        }
        public void ThreadPause()
        {
            _manualResetEvent?.Set();
        }
        public void ThreadResume()
        {
            _manualResetEvent?.Reset();
        }
        private bool CheckTmpAlarm1()
        {
            if (!Param.BUse) return false;
            return (_errorCode & 0x01) > 0 ? true : false;
        }
        private bool CheckTmpAlarm2()
        {
            if (!Param.BUse) return false;
            return (_errorCode & 0x04) > 0 ? true : false;
        }
        private bool CheckTmpAlarm3()
        {
            if (!Param.BUse) return false;
            return (_errorCode & 0x40) > 0 ? true : false;
        }
        public bool CheckAllAlarm()
        {
            if (CheckTmpAlarm1() || CheckTmpAlarm2() || CheckTmpAlarm3())
            {
                return true;
            }
            return false;
        }
        public bool SetIronTemperature(ushort temp)
        {
            if (!IsOpen())
                return false;

            bool ret = false;
            byte[] retvalue = new byte[8];
            int retrycount = 0;
            Byte[] senddata = new Byte[8] { 0x14, 0x68, 0x06, 0x02, 0x00, 0x00, 0x00, 0x00 };

            byte tmpHigher = (byte)(temp >> 8);
            byte tmpLower = (byte)temp;

            senddata[4] = tmpLower;
            senddata[5] = tmpHigher;

            ushort crc16 = /*QA_Infrastructure.*/CRC16.GETCRC16(senddata, 6);

            senddata[6] = (byte)(crc16 >> 8);
            senddata[7] = (byte)(crc16);

        retry:
            lock (obj)
            {
                ClearReadBuffer();
                SendComBytesWhileEnd(senddata, 500);
                GetComByteArray(retvalue, retvalue.Length, 500);
            }
            try
            {
                if ((retvalue[0] == senddata[0])
                    && (retvalue[1] == senddata[1])
                    && (retvalue[2] == senddata[2])
                    && (retvalue[3] == senddata[3])
                    && (retvalue[4] == senddata[4])
                    && (retvalue[5] == senddata[5])
                    && (retvalue[6] == senddata[6])
                    && (retvalue[7] == senddata[7])
                    )
                {
                    ret = true;
                }
                else if (retrycount < serialport_retrycount)
                {
                    retrycount++;
                    goto retry;
                }

                if (!ret)
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetIronTemperature= {Encoding.Default.GetString(retvalue).Trim('\0')}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetIronTemperature {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return ret;
        }
        public bool SetSleepState(bool state)
        {
            if (!IsOpen())
                return false;

            bool ret = false;
            byte[] retvalue = new byte[7];
            int retrycount = 0;
            Byte[] senddata = new Byte[7] { 0x14, 0x68, 0x02, 0x01, 0x00, 0x00, 0x00 };

            if (state == true)
            {
                senddata = new Byte[7] { 0x14, 0x68, 0x02, 0x01, 0x01, 0x00, 0x00 };
            }
            else
            {
                senddata = new Byte[7] { 0x14, 0x68, 0x02, 0x01, 0x03, 0x00, 0x00 };
            }


            ushort crc16 = /*QA_Infrastructure.*/CRC16.GETCRC16(senddata, 5);

            senddata[5] = (byte)(crc16 >> 8);
            senddata[6] = (byte)(crc16);

        retry:
            lock (obj)
            {
                ClearReadBuffer();
                SendComBytesWhileEnd(senddata, 500);
                GetComByteArray(retvalue, retvalue.Length, 500);
            }
            try
            {
                if ((retvalue[0] == senddata[0])
                    && (retvalue[1] == senddata[1])
                    && (retvalue[2] == senddata[2])
                    && (retvalue[3] == senddata[3])
                    && (retvalue[4] == senddata[4])
                    && (retvalue[5] == senddata[5])
                    && (retvalue[6] == senddata[6])
                    )
                {
                    ret = true;
                }
                else if (retrycount < serialport_retrycount)
                {
                    retrycount++;
                    goto retry;
                }

                if (!ret)
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSleepTemperature= {Encoding.Default.GetString(retvalue).Trim('\0')}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSleepTemperature {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return ret;
        }
        public bool GetIronTemp(ref ushort temp)
        {
            if (!IsOpen())
                return false;

            bool ret = false;
            byte[] retvalue = new byte[8]; ;
            int retrycount = 0;
            Byte[] senddata = new Byte[6] { 0x14, 0x67, 0x04, 0x02, 0x00, 0x00 };

            ushort crc16 = /*QA_Infrastructure.*/CRC16.GETCRC16(senddata, 4);

            senddata[4] = (byte)(crc16 >> 8);
            senddata[5] = (byte)(crc16);

        retry:
            lock (obj)
            {
                ClearReadBuffer();
                SendComBytesWhileEnd(senddata, 500);
                GetComByteArray(retvalue, retvalue.Length, 1000);
            }
            try
            {
                if ((retvalue[0] == senddata[0])
                    && (retvalue[1] == senddata[1])
                    && (retvalue[2] == senddata[2])
                    && (retvalue[3] == senddata[3])
                    )
                {
                    if (CRC16.CheckCRC16(retvalue))
                    {
                        ushort l = (ushort)(retvalue[5] << 8);
                        ushort h = retvalue[4];
                        temp = (ushort)(l + h);
                        ret = true;
                    }
                    else if (retrycount < serialport_retrycount)
                    {
                        retrycount++;
                        goto retry;
                    }

                }
                else if (retrycount < serialport_retrycount)
                {
                    retrycount++;
                    goto retry;
                }

                if (!ret)
                {
                    TrigTempSignalPause.BeginInvoke(null, null);
                    //NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetIronTemperature= {Encoding.Default.GetString(retvalue).Trim('\0')}");
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetIronTemperature {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return ret;
        }
        public bool GetSetIronTemp(ref ushort temp)
        {
            if (!IsOpen())
                return false;

            bool ret = false;
            byte[] retvalue = new byte[8];
            int retrycount = 0;
            Byte[] senddata = new Byte[6] { 0x14, 0x67, 0x06, 0x02, 0x00, 0x00 };

            ushort crc16 = /*QA_Infrastructure.*/CRC16.GETCRC16(senddata, 4);

            senddata[4] = (byte)(crc16 >> 8);
            senddata[5] = (byte)(crc16);

        retry:
            lock (obj)
            {
                ClearReadBuffer();
                SendComBytesWhileEnd(senddata, 500);
                GetComByteArray(retvalue, retvalue.Length, 500);
            }
            try
            {
                if ((retvalue[0] == senddata[0])
                    && (retvalue[1] == senddata[1])
                    && (retvalue[2] == senddata[2])
                    && (retvalue[3] == senddata[3])
                    )
                {
                    if (CRC16.CheckCRC16(retvalue))
                    {
                        ushort l = (ushort)(retvalue[5] << 8);
                        ushort h = retvalue[4];
                        temp = (ushort)(l + h);
                        ret = true;
                    }
                    else if (retrycount < serialport_retrycount)
                    {
                        retrycount++;
                        goto retry;
                    }

                }
                else if (retrycount < serialport_retrycount)
                {
                    retrycount++;
                    goto retry;
                }

                if (!ret)
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetSetIronTemperature= {Encoding.Default.GetString(retvalue).Trim('\0')}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetSetIronTemperature {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return ret;
        }
        public bool GetSleepState(ref bool state)
        {
            if (!IsOpen())
                return false;

            bool ret = false;
            byte[] retvalue = new byte[7];
            int retrycount = 0;
            Byte[] senddata = new Byte[6] { 0x14, 0x67, 0x02, 0x01, 0x00, 0x00 };

            ushort crc16 = /*QA_Infrastructure.*/CRC16.GETCRC16(senddata, 4);

            senddata[4] = (byte)(crc16 >> 8);
            senddata[5] = (byte)(crc16);

        retry:
            lock (obj)
            {
                ClearReadBuffer();
                SendComBytesWhileEnd(senddata, 500);
                GetComByteArray(retvalue, retvalue.Length, 500);
            }
            try
            {
                if ((retvalue[0] == senddata[0])
                    && (retvalue[1] == senddata[1])
                    && (retvalue[2] == senddata[2])
                    && (retvalue[3] == senddata[3])
                    )
                {
                    if (CRC16.CheckCRC16(retvalue))
                    {
                        if ((retvalue[4] & 0x02) > 0)
                        {
                            state = false;
                        }
                        else
                        {
                            state = true;
                        }
                        ret = true;
                    }
                    else if (retrycount < serialport_retrycount)
                    {
                        retrycount++;
                        goto retry;
                    }
                }
                else if (retrycount < serialport_retrycount)
                {
                    retrycount++;
                    goto retry;
                }

                //if (!ret)
                //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetSleepState= {Encoding.Default.GetString(retvalue).Trim('\0')}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetSleepState {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return ret;
        }
        public bool GetAlarmState(ref byte errorcode)
        {
            if (!Param.BUse)
            {
                errorcode = 0;
                return true;
            }
            if (!IsOpen())
                return false;

            bool ret = false;
            byte[] retvalue = new byte[7];
            int retrycount = 0;
            Byte[] senddata = new Byte[6] { 0x14, 0x67, 0x03, 0x01, 0x00, 0x00 };

            ushort crc16 = /*QA_Infrastructure.*/CRC16.GETCRC16(senddata, 4);

            senddata[4] = (byte)(crc16 >> 8);
            senddata[5] = (byte)(crc16);

        retry:
            lock (obj)
            {
                ClearReadBuffer();
                SendComBytesWhileEnd(senddata, 500);
                GetComByteArray(retvalue, retvalue.Length, 500);
            }
            try
            {
                if ((retvalue[0] == senddata[0])
                    && (retvalue[1] == senddata[1])
                    && (retvalue[2] == senddata[2])
                    && (retvalue[3] == senddata[3])
                    )
                {
                    if (CRC16.CheckCRC16(retvalue))
                    {
                        errorcode = retvalue[4];
                        ret = true;
                    }
                    else if (retrycount < serialport_retrycount)
                    {
                        retrycount++;
                        goto retry;
                    }
                }
                else if (retrycount < serialport_retrycount)
                {
                    retrycount++;
                    goto retry;
                }
                if (!ret)
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetErrorcode {Encoding.Default.GetString(retvalue)}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetErrorcode {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return ret;
        }
        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> infos)
        {
            if (Param.BUse)
            {
                infos.Add(new AlarmInfoModel()
                {
                    Datetime = DateTime.Now,
                    Index = 0,
                    ErrorCode = 0,
                    AlarmMsg = "未连接",
                    AlarmModule = EN_WarnModules.Robot,
                });
                if ((_errorCode & 0x01) > 0)
                {
                    infos.Add(new AlarmInfoModel()
                    {
                        Datetime = DateTime.Now,
                        Index = 0,
                        ErrorCode = 0,
                        AlarmMsg = "堵料报警",
                        AlarmModule = EN_WarnModules.Temp378,
                    });
                }
                if ((_errorCode & 0x04) > 0)
                {
                    infos.Add(new AlarmInfoModel()
                    {
                        Datetime = DateTime.Now,
                        Index = 0,
                        ErrorCode = 0,
                        AlarmMsg = "缺料报警",
                        AlarmModule = EN_WarnModules.Temp378,
                    });
                }
                if ((_errorCode & 0x40) > 0)
                {
                    infos.Add(new AlarmInfoModel()
                    {
                        Datetime = DateTime.Now,
                        Index = 0,
                        ErrorCode = 0,
                        AlarmMsg = "温度报警",
                        AlarmModule = EN_WarnModules.Temp378,
                    });
                }
            }
        }
    }
}
