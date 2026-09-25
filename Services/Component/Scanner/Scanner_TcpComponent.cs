using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls;
using QA_Infrastructure.BaseCtrls.ParamEnum;
using QA_Infrastructure.NLogOut;
using IScanner = QA.Business.Interfaces.IScanner;

namespace QA.Business.Component.Scanner
{
    public class Scanner_TcpComponent : TcpCtrl, IScanner
    {
        private SocketParam _socketParam = new SocketParam();
        private ScannerParam _scannerParam;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        public IParam Param { get; set; }
        public string ComponentName { get; set; }
        public new bool IsConnected { get; set; }

        public Scanner_TcpComponent()
        {
            Param = new ScannerParam();
            _scannerParam = IoC.Get<ScannerParam>();
        }

        public bool Initial(IParam param)
        {
            try
            {
                Param = _scannerParam = param as ScannerParam;
                _socketParam.Ip = _scannerParam.IP;
                _socketParam.Port = _scannerParam.Port;
                _socketParam.SendTimeout = _scannerParam.SendTimeout;
                _socketParam.ReceiveTimeout = _scannerParam.ReceiveTimeout;
                _socketParam.SendBuffSize = 8192;
                _socketParam.ReceiveBuffSize = 8192;
                base.SetParam(_socketParam);
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
                    await Task.Delay(Param.BUse ? 10 : 1000, _cancellationToken);
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        if (!Param.BUse)
                        {
                            continue;
                        }
                        if (!IsConnected)
                        {
                            IsConnected = Connect(_socketParam, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
                    }
                    await Task.Delay(Param.BUse ? 10 : 1000, _cancellationToken);
                }
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

        public bool DataManManualTriger(out string barcode)
        {
            barcode = "";
            try
            {
                string sendStr = _scannerParam.StartCommand;
                if (_scannerParam.Bechocmd)
                {
                    sendStr += "\r\n";
                }
                string receiveStr = "";
                if (!SendData(sendStr, ref receiveStr))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"扫码枪通信失败");
                    return false;
                }
                barcode = receiveStr.Trim('\0').Trim('\n').Trim('\r').Replace(" ", "");
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"DataManManualTriger={barcode}");
                if (string.IsNullOrEmpty(barcode) || barcode == _scannerParam.ErrorStr)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"未扫到码");
                    return false;
                }
                if (barcode.Length < _scannerParam.Minlen || barcode.Length > _scannerParam.Maxlen)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"码{barcode}长度{barcode.Length}不是{_scannerParam.Minlen}至{_scannerParam.Maxlen}", En_Logout_Type.Default, true);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString());
                return false;
            }
            finally
            {
                try
                {
                    //结束时要发终止指令，避免扫码枪一直触发扫码
                    string sendStr = _scannerParam.StopCommand;
                    if (_scannerParam.Bechocmd)
                    {
                        sendStr += "\r\n";
                    }
                    string receiveStr = "";
                    SendData(sendStr, ref receiveStr, false);
                }
                catch (Exception ex)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString());
                }
            }
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (_scannerParam.BUse)
            {
                if (!IsConnected)
                {
                    var count = alarmInfos.Count;
                    alarmInfos.Add(new AlarmInfoModel()
                    {
                        AlarmLevel = EN_WARN_LEVEL.Error,
                        AlarmModule = EN_WarnModules.DataMan,
                        AlarmMsg = "无连接",
                        Datetime = DateTime.Now,
                        ErrorCode = 0,
                        Index = count
                    });
                }
            }
        }
    }
}
