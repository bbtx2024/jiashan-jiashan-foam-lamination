/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-12
 * 说明：（激光测高仪逻辑）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ComCtrl;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Component.LaserHeightSensor
{
    public class LaserHeightSensor_Component : SerialPortCtrl, ILaserHeightSensor
    {
        private LaserHeightSensorParam _laserHeightSensorParam;
        private SerialPortParam _serialPortParam;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        public IParam Param { get; set; }
        public string ComponentName { get; set; }

        public bool IsConnected { get; set; }

        public LaserHeightSensor_Component()
        {
            Param = new LaserHeightSensorParam();
            _serialPortParam = new SerialPortParam();
        }

        public bool Initial(IParam param)
        {
            try
            {
                serialport_retrycount = 3;
                Param = _laserHeightSensorParam = param as LaserHeightSensorParam;
                _serialPortParam.Com = _laserHeightSensorParam.COMPort;
                _serialPortParam.BaudRate = _laserHeightSensorParam.Baudrate;
                _serialPortParam.DataBit = 8;
                _serialPortParam.StopBits = System.IO.Ports.StopBits.One;
                _serialPortParam.Parity = System.IO.Ports.Parity.None;
                _serialPortParam.OrderTimeOut = _laserHeightSensorParam.ReceiveTimeout;
                _serialPortParam.ReadTimeOut = _laserHeightSensorParam.ReceiveTimeout;
                _serialPortParam.WriteTimeOut = _laserHeightSensorParam.SendTimeout;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e + e.StackTrace);
                return false;
            }
            return true;
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (_laserHeightSensorParam.BUse)
            {
                if (!IsConnected)
                {
                    var count = alarmInfos.Count;
                    alarmInfos.Add(new AlarmInfoModel()
                    {
                        AlarmLevel = EN_WARN_LEVEL.Error,
                        AlarmModule = EN_WarnModules.System,//添加异常报警模块
                        AlarmMsg = "无连接",
                        Datetime = DateTime.Now,
                        ErrorCode = 0,
                        Index = count
                    });
                }
            }
        }

        public bool GetLaserHightStatus(ref float value)
        {
            float temp = 0;
            if (MeasureOnce(ref temp))
            {
                value = temp;
                return true;
            }
            return false;
        }



        public bool MeasureOnce(ref float value)
        {
            if (!IsOpen())
                return false;

            bool ret = false;
            string retvalue = "";
            int retrycount = 0;
        retry:
            lock (obj)
            {
                ClearReadBuffer();
                Send("MEASURE");// SerialPortCtrl.
                WhileReadExisting(ref retvalue, 8, OrderTimeOut);
                Byte[] data = Encoding.Default.GetBytes(retvalue);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0x02)
                        data[i] = (byte)(':');
                    else if (data[i] == 0x03)
                        data[i] = (byte)(';');
                }
                retvalue = Encoding.Default.GetString(data).Replace(":", "").Replace(";", "");
            }
            try
            {
                if (retvalue.Contains("."))
                    ret = true;
                else if (retrycount < serialport_retrycount)
                {
                    retrycount++;
                    goto retry;
                }
                if (ret)
                    value = float.Parse(retvalue);
                else
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"MeasureOnce {retvalue}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"MeasureOnce {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return ret;
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                var delayTime = _laserHeightSensorParam.BUse ? 100 : 1000;
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (_laserHeightSensorParam.BUse)
                    {
                        if (!IsConnected)
                        {
                            if (IsOpen())
                                Close();
                            IsConnected = OpenPort(_serialPortParam);
                        }
                    }
                }
                catch (Exception e)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                }

                await Task.Delay(delayTime, _cancellationToken);
            }, _cancellationToken);

            return true;
        }

        public bool Stop()
        {
            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return true;
        }
    }
}
