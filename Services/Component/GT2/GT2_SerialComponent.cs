/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-11
 * 说明：（GT2测高仪逻辑）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.ObjectModel;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls;
using QA_Infrastructure.BaseCtrls.ParamEnum;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Component.GT2
{
    public class GT2_SerialComponent : SerialPortCtrl, IGT2
    {
        private GT2Param _gT2Param;
        private SerialPortParam _serialPortParam;
        public float receive = 0;
        public IParam Param { get; set; }
        public string ComponentName { get; set; }

        public bool IsConnected { get; set; }

        public GT2_SerialComponent()
        {
            Param = new GT2Param();
            _serialPortParam = new SerialPortParam();
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (_gT2Param.BUse)
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

        public bool GetLaserHighStatus(ref float high)
        {
            string result = string.Empty;
            ClearReadBuffer();
            SerialPort.Write("M0\r");
            WhileReadExisting(ref result, 10, _serialPortParam.OrderTimeOut);
            result = result.Replace("M0,", "");
            result = result.Replace("\r\n", "");
            if (result.Contains("0"))
                high = Convert.ToSingle(result);
            else
                return false;
            return true;
        }

        public bool Initial(IParam param)
        {
            try
            {
                Param = _gT2Param = param as GT2Param;
                _serialPortParam.Com = _gT2Param.PortName;
                _serialPortParam.BaudRate = _gT2Param.Baudrate;
                _serialPortParam.DataBit = _gT2Param.DataBit;
                _serialPortParam.StopBits = _gT2Param.StopBits;
                _serialPortParam.Parity = _gT2Param.Parity;
                _serialPortParam.OrderTimeOut = _gT2Param.TimeOut;
                _serialPortParam.ReadTimeOut = _gT2Param.TimeOut;
                _serialPortParam.WriteTimeOut = _gT2Param.TimeOut;
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
            try
            {
                if (_gT2Param.BUse)
                {
                    if (!IsConnected)
                    {
                        if (IsOpen())
                        {
                            ClosePort();
                        }

                        IsConnected = OpenPort(_serialPortParam);
                    }
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return true;
        }
        public bool Stop()
        {
            try
            {
                ClosePort();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return true;
        }
    }
}
