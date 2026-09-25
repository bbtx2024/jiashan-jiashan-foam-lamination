using Caliburn.Micro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QA.Business.Converts;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model;
using QA.Business.Model.Alarm;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using static QA.Business.Define.HiveAlarmDefine;

namespace QA.Business.Component.HIVE
{
    public class HiveErrorData
    {
        public string message;
        public string code;
        public string severity;
        public DateTime occurrence_time;
        public DateTime resolved_time;
        public string error_detail;
    }

    public class Hive_Component : IHive, IHandle<ObservableCollection<AlarmInfoModel>>
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private volatile CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private HiveParam _hiveParam;
        private MyUserManager _myUserManager;
        private static HiveStatusToStringConvert hiveStatusToStringConvert = new HiveStatusToStringConvert();
        private DateTime lastHaveCarrierTime = DateTime.Now;//用于running90s无物料自动切换idle
        private ObservableCollection<AlarmInfoModel> currAlarms = new ObservableCollection<AlarmInfoModel>();//用于有报警时自动切换downtime
        private HiveErrorData temphiveErrorData = null;//用于跳出downtime时上传报警信息
        public HiveMachineStatus[] hiveStatusTime = new HiveMachineStatus[7];//近7天的hive时间统计，最后一项为当天
        public DateTime lastChangeStatusTime;//用于确保软件关闭后仍能正确统计hive时间
        private object obj = new object();
        #endregion

        #region Property
        public IParam Param { get; set; } = null;
        public string ComponentName { get; set; } = "HIVE";
        public bool IsConnected { get; set; } = false;
        private StepStatus _stepStatus => IoC.Get<StepStatus>();

        public EN_HiveStatus HiveStatus { get; private set; } = EN_HiveStatus.Idle;//当前hive状态
        public EN_HiveStatus ManualState { get; set; } = EN_HiveStatus.Idle;//手动切换hive状态的目标
        public string HiveStatusStr => (string)hiveStatusToStringConvert.Convert(HiveStatus, null, null, null);
        public string Badge { get; private set; } = "";
        public bool InSpotCheckPage { get; set; } = false;//处于点检页面时，自动尝试切换到plannedDT
        public bool InSettingPage { get; set; } = false;//处于设置页面时，自动尝试切换到plannedDT
        public bool ChangeMaterial { get; set; } = false;//处于换料状态时，自动尝试切换到plannedDT
        public string ErrCode { get; set; } = "";
        public string ErrMessage { get; set; } = "";
        public string ErrDetail { get; set; } = "";
        public string SoftwareVersion => _stepStatus.ParamManager.OtherSettingParam.SoftwareVersion;
        public string MainSoftwareSHA1 => GetMainSoftwareSHA1();
        public string VisionSoftwareSHA1 => GetVisionSoftwareSHA1();
        public string ConfigFileSHA1 => GetConfigFileSHA1();
        #endregion

        public Hive_Component()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _myUserManager = IoC.Get<MyUserManager>();
            Param = IoC.Get<ParamManager>().HiveParam;
            for (int i = 0; i < hiveStatusTime.Length; i++)
            {
                hiveStatusTime[i] = new HiveMachineStatus();
            }
        }

        public bool Initial(IParam param)
        {
            Param = _hiveParam = param as HiveParam;
            return Param != null && _hiveParam != null;
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                PLC_Component _plc_Component = (PLC_Component)IoC.Get<IPLC>();
                ReadHiveStatusTime();
                while (true)
                {
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        DateTime current = DateTime.Now;
                        //天数改变时，需要刷新hive各天状态时间
                        //需要注意长时间不开启软件的情况
                        while (lastChangeStatusTime.Date != current.Date)
                        {
                            var time = lastChangeStatusTime.Date.AddDays(1);
                            var ts = time - lastChangeStatusTime;
                            hiveStatusTime[hiveStatusTime.Length - 1].AddTime(HiveStatus, ts.TotalSeconds);
                            for (int i = 1; i < hiveStatusTime.Length; i++)
                            {
                                hiveStatusTime[i - 1] = hiveStatusTime[i].DeepCopy();
                            }
                            hiveStatusTime[hiveStatusTime.Length - 1].Clear();
                            lastChangeStatusTime = time;
                            WriteHiveStatusTime();
                        }
                        switch (HiveStatus)
                        {
                            case EN_HiveStatus.Downtime:
                                if (ManualState != EN_HiveStatus.Downtime
                                && !_plc_Component.IsEStop() && currAlarms.Count == 0
                                && !InSpotCheckPage && !InSettingPage && !ChangeMaterial)
                                {
                                    if (temphiveErrorData != null)
                                    {
                                        temphiveErrorData.resolved_time = DateTime.Now;
                                        SendAlarmInfo(temphiveErrorData);
                                        temphiveErrorData = null;
                                    }
                                    ChangeHiveState(EN_HiveStatus.Running);
                                    lastHaveCarrierTime = DateTime.Now;
                                    continue;
                                }
                                break;
                            case EN_HiveStatus.Idle:
                            case EN_HiveStatus.Running:
                                if (InSpotCheckPage || InSettingPage)
                                {
                                    Badge = _myUserManager.CurrUserID;
                                    ErrCode = "PD-01";
                                    ErrMessage = "Plan downtime";
                                    ErrDetail = "Daily Maintenance";
                                    ChangeHiveState(EN_HiveStatus.Downtime);
                                    continue;
                                }
                                if (ChangeMaterial)
                                {
                                    Badge = _myUserManager.CurrUserID;
                                    ErrCode = "PD-08";
                                    ErrMessage = "Plan downtime";
                                    ErrDetail = "Material Replacement";
                                    ChangeHiveState(EN_HiveStatus.Downtime);
                                    continue;
                                }
                                if (ErrCode.StartsWith("PD-"))
                                {
                                    Badge = _myUserManager.CurrUserID;
                                    ChangeHiveState(EN_HiveStatus.Downtime);
                                    continue;
                                }
                                if (temphiveErrorData == null)
                                {
                                    foreach (var alarm in currAlarms)
                                    {
                                        HiveAlarmDefine.AlarmList.TryGetValue((int)alarm.ErrorCode, out string errstr);
                                        if (errstr != null)
                                        {
                                            temphiveErrorData = new HiveErrorData();
                                            temphiveErrorData.occurrence_time = DateTime.Now;
                                            string[] str = errstr.Split(',');
                                            temphiveErrorData.code = str[0].Trim().Trim('\"');
                                            ErrCode = temphiveErrorData.code;
                                            temphiveErrorData.message = str[1].Trim().Trim('\"');
                                            ErrMessage = temphiveErrorData.message;
                                            temphiveErrorData.error_detail = str[2].Trim().Trim('\"');
                                            ErrDetail = temphiveErrorData.error_detail;
                                            break;
                                        }
                                    }
                                }
                                if (temphiveErrorData != null)
                                {
                                    ChangeHiveState(EN_HiveStatus.Downtime);
                                    continue;
                                }
                                if (ManualState == EN_HiveStatus.Downtime && !string.IsNullOrEmpty(ErrCode))
                                {
                                    foreach (var x in HiveAlarmDefine.AlarmList.Keys)
                                    {
                                        string errstr = HiveAlarmDefine.AlarmList[x];
                                        if (errstr.StartsWith(ErrCode))
                                        {
                                            temphiveErrorData = new HiveErrorData();
                                            temphiveErrorData.occurrence_time = DateTime.Now;
                                            string[] str = errstr.Split(',');
                                            temphiveErrorData.code = str[0].Trim().Trim('\"');
                                            ErrCode = temphiveErrorData.code;
                                            temphiveErrorData.message = str[1].Trim().Trim('\"');
                                            ErrMessage = temphiveErrorData.message;
                                            temphiveErrorData.error_detail = str[2].Trim().Trim('\"');
                                            ErrDetail = temphiveErrorData.error_detail;
                                            break;
                                        }
                                    }
                                }
                                if (temphiveErrorData != null)
                                {
                                    ChangeHiveState(EN_HiveStatus.Downtime);
                                    continue;
                                }
                                if (_plc_Component.IsCurReset || _plc_Component.IsCurStop)
                                {
                                    ChangeHiveState(EN_HiveStatus.Idle);
                                    continue;
                                }
                                if (_stepStatus.GetCurCarrier() != null)
                                {
                                    lastHaveCarrierTime = DateTime.Now;
                                    ManualState = EN_HiveStatus.Running;
                                    ChangeHiveState(EN_HiveStatus.Running);
                                    continue;
                                }
                                //if (ManualState == EN_HiveStatus.Running && HiveStatus != EN_HiveStatus.Running)
                                //{
                                //    lastHaveCarrierTime = DateTime.Now;
                                //}
                                //if (ManualState == EN_HiveStatus.Idle && HiveStatus != EN_HiveStatus.Idle)
                                //{
                                //    lastHaveCarrierTime = DateTime.Now.AddSeconds(-90);
                                //}

                                if ((DateTime.Now - lastHaveCarrierTime).TotalSeconds <= 90)
                                {
                                    if (ManualState == EN_HiveStatus.Idle)
                                    {
                                        ChangeHiveState(EN_HiveStatus.Idle);
                                    }
                                    else
                                    {
                                        ChangeHiveState(EN_HiveStatus.Running);
                                    }
                                }
                                else
                                {
                                    ChangeHiveState(EN_HiveStatus.Idle);
                                }
                                break;
                            default:
                                ChangeHiveState(EN_HiveStatus.Running);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
                    }
                    await Task.Delay(10, _cancellationToken);
                }
            }, _cancellationToken);
            return true;
        }

        private void ReadHiveStatusTime()
        {
            lock (obj)
            {
                try
                {
                    //string dir = $@"D:\CsvData\Hive";
                    string dir = @"D:\QKProject\Data\Hive\HiveStateTime";
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string filePath = $@"{dir}\StateTotalTime.csv";
                    if (!File.Exists(filePath))
                    {
                        ChangeHiveState(EN_HiveStatus.Idle);
                        for (int i = 0; i < hiveStatusTime.Length - 2; i++)
                        {
                            hiveStatusTime[i].Clear();
                            hiveStatusTime[i].AddTime(EN_HiveStatus.Idle, 86400);
                        }
                        hiveStatusTime[hiveStatusTime.Length - 1].AddTime(EN_HiveStatus.Idle, (lastChangeStatusTime - DateTime.Now.Date).TotalSeconds);
                    }
                    else
                    {
                        using (StreamReader sr = new StreamReader(filePath))
                        {
                            string[] data = sr.ReadLine().Split(',');
                            //最后一次切换时间,最后hive状态
                            lastChangeStatusTime = new DateTime(long.Parse(data[0]));
                            HiveStatus = (EN_HiveStatus)int.Parse(data[1]);
                            for (int i = 0; i < hiveStatusTime.Length; i++)
                            {
                                data = sr.ReadLine().Split(',');
                                //0,running时间,idle时间,...
                                for (int j = 1; j <= 5; j++)
                                {
                                    hiveStatusTime[i].AddTime((EN_HiveStatus)j, double.Parse(data[j]));
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                { }
            }
        }

        private void WriteHiveStatusTime()
        {
            lock (obj)
            {
                try
                {
                    //string dir = $@"D:\CsvData\Hive";
                    string dir = @"D:\QKProject\Data\Hive\HiveStateTime";
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string filePath = $@"{dir}\StateTotalTime.csv";
                    using (StreamWriter sw = new StreamWriter(filePath, false))
                    {
                        //最后一次切换时间,最后hive状态
                        sw.WriteLine($"{lastChangeStatusTime.Ticks},{(int)HiveStatus}");
                        for (int i = 0; i < hiveStatusTime.Length; i++)
                        {
                            //0,running时间,idle时间,...
                            for (int j = 0; j < 6; j++)
                            {
                                sw.Write(hiveStatusTime[i].machineStatusTime[j]);
                                if (j != 5)
                                {
                                    sw.Write(",");
                                }
                            }
                            sw.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                { }
            }
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

        public void Handle(ObservableCollection<AlarmInfoModel> alarms)
        {
            currAlarms = alarms.DeepCopy();
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
        }

        private bool PostToServer(string type, string sendStr)
        {
            if (!Param.BUse)
            {
                IsConnected = false;
                return true;
            }
            try
            {
                string strURL = $"http://{_hiveParam.Hive_IP}:{_hiveParam.Hive_Port}/v5/capture/{type}";
                byte[] datas = Encoding.UTF8.GetBytes(sendStr);
                WebRequest webRequest = (HttpWebRequest)WebRequest.Create(strURL);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = datas.Length;
                using (var stream = webRequest.GetRequestStream())
                {
                    stream.Write(datas, 0, datas.Length);
                }
                var response = (HttpWebResponse)webRequest.GetResponse();
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string receiveStr = sr.ReadToEnd().TrimEnd('\n');
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, "\n发送：" + sendStr + "\n接收：" + receiveStr, En_Logout_Type.Sfc, true);
                    //NLog.LogManager.LogFactory.GetLogger("hive").Info("\n发送：" + sendStr + "\n接收：" + receiveStr);
                }
                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "\n发送：" + sendStr + "\n接收失败：" + ex.ToString(), En_Logout_Type.Sfc, true);
                IsConnected = false;
                return false;
            }
        }

        private bool SendState(DateTime stateChangeTime, EN_HiveStatus state)
        {
            JObject obj = new JObject();
            obj.Add("machine_state", (int)state);
            obj.Add("state_change_time", stateChangeTime.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));
            JObject data = new JObject();
            obj.Add("data", data);
            data.Add("previous state", (int)HiveStatus);
            data.Add("SW_Version", SoftwareVersion);
            data.Add("VS_Version", "QK-FoamAuto_S1-VS-LE4-L-V1.0.1-NPI");
            data.Add("CD_Version", "QK-FoamAuto_S1-CD-LE4-L-V1.0.1-NPI");
            data.Add("MS_SHA1", MainSoftwareSHA1);
            data.Add("VS_SHA1", VisionSoftwareSHA1);
            data.Add("CD_SHA1", ConfigFileSHA1);
            if (state == EN_HiveStatus.Engineering || state == EN_HiveStatus.PlannedDowntime)
            {
                data.Add("badge", Badge);
                Badge = "";
            }
            if (state == EN_HiveStatus.PlannedDowntime || state == EN_HiveStatus.Downtime)
            {
                data.Add("error_message", ErrMessage);
                data.Add("error_detail", ErrDetail);
                data.Add("code", ErrCode);
                //ErrMessage = "";
                //ErrDetail = "";
                //ErrCode = "";
            }
            else
            {
                data.Add("error_message", "");
                data.Add("error_detail", "");
                data.Add("code", "");
            }
            bool postOK = PostToServer("machinestate", obj.ToString(Formatting.None));
            try
            {
                //string dir = $@"{AppDomain.CurrentDomain.BaseDirectory}/Datas/Hive";
                string dir = @"D:\QKProject\Data\Hive\HiveStateChange";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string fileName = $@"{dir}/StateChange_{stateChangeTime.ToString("yyyy-MM-dd")}.csv";
                bool isFileExist = File.Exists(fileName);
                using (StreamWriter sw = new StreamWriter(fileName, true))
                {
                    if (!isFileExist)
                    {
                        sw.WriteLine("时间,旧状态,新状态,通信状态");
                    }
                    sw.WriteLine($"{stateChangeTime},{(int)HiveStatus},{(int)state},{(postOK ? "" : "通信失败")}");
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception, false);
            }
            return postOK;
        }

        private bool SendAlarmInfo(HiveErrorData hiveErrorData)
        {
            JObject obj = new JObject();
            obj.Add("message", hiveErrorData.message);
            obj.Add("code", hiveErrorData.code);
            obj.Add("severity", "error");
            obj.Add("occurrence_time", hiveErrorData.occurrence_time.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));
            obj.Add("resolved_time", hiveErrorData.resolved_time.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));
            JObject data = new JObject();
            obj.Add("data", data);
            data.Add("hive_state", 5);
            data.Add("previous_state", 1);//先写死，如果后续需要使用实际值，则在HiveErrorData里面新增一项，再传递过来
            data.Add("error_detail", hiveErrorData.error_detail);
            bool postOK = PostToServer("errordata", obj.ToString(Formatting.None));
            try
            {
                //string dir = $@"{AppDomain.CurrentDomain.BaseDirectory}/Datas/Hive";
                string dir = @"D:\QKProject\Data\Hive\ErrorData";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string fileName = $@"{dir}/ErrorData_{hiveErrorData.resolved_time.ToString("yyyy-MM-dd")}.csv";
                bool isFileExist = File.Exists(fileName);
                using (StreamWriter sw = new StreamWriter(fileName, true))
                {
                    if (!isFileExist)
                    {
                        sw.WriteLine("开始时间,结束时间,错误类型,错误代码,详细信息,通信状态");
                    }
                    sw.WriteLine($"{hiveErrorData.occurrence_time},{hiveErrorData.resolved_time},{hiveErrorData.message},{hiveErrorData.code},{hiveErrorData.error_detail},{(postOK ? "" : "通信失败")}");
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception, false);
            }
            return postOK;
        }

        /// <summary>
        /// sip板流出时调用，上传产品处理情况
        /// </summary>
        /// <param name="sipSN"></param>
        /// <param name="pass"></param>
        /// <param name="inputTime"></param>
        /// <param name="outputTime"></param>
        /// <returns></returns>
        public bool SendMachineData(string sipSN, bool pass, DateTime inputTime, DateTime outputTime)
        {
            JObject obj = new JObject();
            obj.Add("unit_sn", sipSN);
            obj.Add("pass", pass.ToString());
            obj.Add("input_time", inputTime.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));
            obj.Add("output_time", outputTime.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));
            JObject serials = new JObject();
            obj.Add("serials", serials);
            JObject data = new JObject();
            obj.Add("data", data);
            data.Add("SW_Version", SoftwareVersion);
            //0603
            data.Add("MS_SHA1", MainSoftwareSHA1);
            data.Add("VS_SHA1", VisionSoftwareSHA1);
            data.Add("hive_state", HiveStatus.ToString());
            data.Add("Bay", "1");
            //0603
            data.Add("d.badge", Badge);
            data.Add("limits_version", "QK_214.1.0.1_250524_POR");
            data.Add("output_time_CH", outputTime.ToString("yyyy-MM-ddTHH:mm:ss.ff"));
            float cycleCT = IoC.Get<GlobalVariable>().CT.CycleCT;
            float totalCT = IoC.Get<GlobalVariable>().CT.TotalCT;
            data.Add("cycle_time", (cycleCT > 60 ? totalCT : cycleCT).ToString("F2"));
            //20250603增加上传信息
            data.Add("site", "ITJS");
            data.Add("building", "A01");
            data.Add("SF line ID", _hiveParam.SF_line_ID);
            data.Add("line_type", "SIP Sub");
            data.Add("station_type", _hiveParam.Station_type);
            data.Add("station_instance", "1");
            data.Add("vendor", _hiveParam.Vendor);
            data.Add("level", "1");
            data.Add("rfid", "QUICK20230323A005");
            data.Add("build_type", "NPI");
            data.Add("Press_force", "1000g");
            data.Add("Press_time", "5s");
            JObject parametric_data = new JObject();
            data.Add("parametric_data", parametric_data);
            parametric_data.Add("Safty position_X1", _hiveParam.Safty_position_X1);//
            parametric_data.Add("Safty position_Y1", _hiveParam.Safty_position_Y1);
            parametric_data.Add("Safty position_X2", _hiveParam.Safty_position_X2);
            parametric_data.Add("Safty position_Y2", _hiveParam.Safty_position_Y2);
            parametric_data.Add("Safty position_Z2", _hiveParam.Safty_position_Z2);
            parametric_data.Add("Pick position_X1", _hiveParam.Pick_position_X1);
            parametric_data.Add("Pick position_Y1", _hiveParam.Pick_position_Y1);
            parametric_data.Add("Pick position_Z1", _hiveParam.Pick_position_Z1);
            parametric_data.Add("Pick position_R1", _hiveParam.Pick_position_R1);
            parametric_data.Add("Pick position_X2", _hiveParam.Pick_position_X2);
            parametric_data.Add("Pick position_Y2", _hiveParam.Pick_position_Y2);
            parametric_data.Add("Pick position_Z2", _hiveParam.Pick_position_Z2);
            parametric_data.Add("Pick position_R2", _hiveParam.Pick_position_R2);
            parametric_data.Add("Vision position_X1", _hiveParam.Vision_position_X1);
            parametric_data.Add("Vision position_Y1", _hiveParam.Vision_position_Y1);
            parametric_data.Add("Vision position_X4", _hiveParam.Vision_position_X4);
            parametric_data.Add("Vision position_Y4", _hiveParam.Vision_position_Y4);
            parametric_data.Add("Vision position_X7", _hiveParam.Vision_position_X7);
            parametric_data.Add("Vision position_Y7", _hiveParam.Vision_position_Y7);
            parametric_data.Add("Vision position_X10", _hiveParam.Vision_position_X10);
            parametric_data.Add("Vision position_Y10", _hiveParam.Vision_position_Y10);
            parametric_data.Add("Vision position_X2", _hiveParam.Vision_position_X2);
            parametric_data.Add("Vision position_Y2", _hiveParam.Vision_position_Y2);
            parametric_data.Add("Vision position_X5", _hiveParam.Vision_position_X5);
            parametric_data.Add("Vision position_Y5", _hiveParam.Vision_position_Y5);
            parametric_data.Add("Vision position_X8", _hiveParam.Vision_position_X8);
            parametric_data.Add("Vision position_Y8", _hiveParam.Vision_position_Y8);
            parametric_data.Add("Vision position_X11", _hiveParam.Vision_position_X11);
            parametric_data.Add("Vision position_Y11", _hiveParam.Vision_position_Y11);
            parametric_data.Add("Vision position_X3", _hiveParam.Vision_position_X3);
            parametric_data.Add("Vision position_Y3", _hiveParam.Vision_position_Y3);
            parametric_data.Add("Vision position_X6", _hiveParam.Vision_position_X6);
            parametric_data.Add("Vision position_Y6", _hiveParam.Vision_position_Y6);
            parametric_data.Add("Vision position_X9", _hiveParam.Vision_position_X9);
            parametric_data.Add("Vision position_Y9", _hiveParam.Vision_position_Y9);
            parametric_data.Add("Vision position_X12", _hiveParam.Vision_position_X12);
            parametric_data.Add("Vision position_Y12", _hiveParam.Vision_position_Y12);
            parametric_data.Add("Vision position_X13", _hiveParam.Vision_position_X13);
            parametric_data.Add("Vision position_Y13", _hiveParam.Vision_position_Y13);
            parametric_data.Add("Vision position_Z13", _hiveParam.Vision_position_Z13);
            parametric_data.Add("Vision position_R13", _hiveParam.Vision_position_R13);
            parametric_data.Add("Vision position_X14", _hiveParam.Vision_position_X14);
            parametric_data.Add("Vision position_Y14", _hiveParam.Vision_position_Y14);
            parametric_data.Add("Vision position_Z14", _hiveParam.Vision_position_Z14);
            parametric_data.Add("Vision position_R14", _hiveParam.Vision_position_R14);
            parametric_data.Add("Assy position_X1", _hiveParam.Assy_position_X1);
            parametric_data.Add("Assy position_Y1", _hiveParam.Assy_position_Y1);
            parametric_data.Add("Assy position_Z1", _hiveParam.Assy_position_Z1);
            parametric_data.Add("Assy position_X4", _hiveParam.Assy_position_X4);
            parametric_data.Add("Assy position_Y4", _hiveParam.Assy_position_Y4);
            parametric_data.Add("Assy position_Z4", _hiveParam.Assy_position_Z4);
            parametric_data.Add("Assy position_X7", _hiveParam.Assy_position_X7);
            parametric_data.Add("Assy position_Y7", _hiveParam.Assy_position_Y7);
            parametric_data.Add("Assy position_Z7", _hiveParam.Assy_position_Z7);
            parametric_data.Add("Assy position_X10", _hiveParam.Assy_position_X10);
            parametric_data.Add("Assy position_Y10", _hiveParam.Assy_position_Y10);
            parametric_data.Add("Assy position_Z10", _hiveParam.Assy_position_Z10);
            parametric_data.Add("Assy position_X2", _hiveParam.Assy_position_X2);
            parametric_data.Add("Assy position_Y2", _hiveParam.Assy_position_Y2);
            parametric_data.Add("Assy position_Z2", _hiveParam.Assy_position_Z2);
            parametric_data.Add("Assy position_X5", _hiveParam.Assy_position_X5);
            parametric_data.Add("Assy position_Y5", _hiveParam.Assy_position_Y5);
            parametric_data.Add("Assy position_Z5", _hiveParam.Assy_position_Z5);
            parametric_data.Add("Assy position_X8", _hiveParam.Assy_position_X8);
            parametric_data.Add("Assy position_Y8", _hiveParam.Assy_position_Y8);
            parametric_data.Add("Assy position_Z8", _hiveParam.Assy_position_Z8);
            parametric_data.Add("Assy position_X11", _hiveParam.Assy_position_X11);
            parametric_data.Add("Assy position_Y11", _hiveParam.Assy_position_Y11);
            parametric_data.Add("Assy position_Z11", _hiveParam.Assy_position_Z11);
            parametric_data.Add("Assy position_X3", _hiveParam.Assy_position_X3);
            parametric_data.Add("Assy position_Y3", _hiveParam.Assy_position_Y3);
            parametric_data.Add("Assy position_Z3", _hiveParam.Assy_position_Z3);
            parametric_data.Add("Assy position_X6", _hiveParam.Assy_position_X6);
            parametric_data.Add("Assy position_Y6", _hiveParam.Assy_position_Y6);
            parametric_data.Add("Assy position_Z6", _hiveParam.Assy_position_Z6);
            parametric_data.Add("Assy position_X9", _hiveParam.Assy_position_X9);
            parametric_data.Add("Assy position_Y9", _hiveParam.Assy_position_Y9);
            parametric_data.Add("Assy position_Z9", _hiveParam.Assy_position_Z9);
            parametric_data.Add("Assy position_X12", _hiveParam.Assy_position_X12);
            parametric_data.Add("Assy position_Y12", _hiveParam.Assy_position_Y12);
            parametric_data.Add("Assy position_Z12", _hiveParam.Assy_position_Z12);
            parametric_data.Add("Tossing poaition_X", _hiveParam.Tossing_poaition_X);
            parametric_data.Add("Tossing poaition_Y", _hiveParam.Tossing_poaition_Y);
            parametric_data.Add("Tossing poaition_Z", _hiveParam.Tossing_poaition_Z);

            return PostToServer("machinedata", obj.ToString(Formatting.None));
        }

        /// <summary>
        /// 抛小料（就是卷料）时调用，上传抛料信息
        /// </summary>
        /// <param name="inputTime"></param>
        /// <param name="outputTime"></param>
        /// <param name="tossingCode">TossHSGS1-1-03（Toss + 物料名称ALT/FLE + 机架S1/S3 + 吸嘴号 + 原因代码）</param>
        /// <param name="errorDetail"></param>
        /// <returns></returns>
        public bool SendTossingInfo(DateTime inputTime, DateTime outputTime, int nozzle, EN_TossingCode tossingCode)
        {
            JObject obj = new JObject();
            obj.Add("unit_sn", "1234567890");
            obj.Add("pass", false.ToString());
            obj.Add("input_time", inputTime.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));
            obj.Add("output_time", outputTime.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));

            JObject serials = new JObject();
            obj.Add("serials", serials);
            serials.Add("sub_id", "S1");//注意根据机台实际设置
            serials.Add("error_message", "Tossing Error");
            string code = $"Toss{"ALTS1"}-{nozzle}-{(int)tossingCode:D2}";//注意根据机台实际设置
            serials.Add("Tossing_code", code);//错误编号
            string detail = Enum.GetName(typeof(EN_TossingCode), tossingCode);
            serials.Add("error_detail", detail);//错误描述，什么的抛料，小料，产品

            JObject data = new JObject();
            obj.Add("data", data);
            data.Add("SW_Version", SoftwareVersion);
            data.Add("hive_state", (int)HiveStatus);
            data.Add("Bay", "1");
            data.Add("output_time_CH", outputTime.ToString("yyyy-MM-ddTHH:mm:ss.ff"));
            data.Add("Tossing_1", 1);

            return PostToServer("machinedata", obj.ToString(Formatting.None));
        }

        ///// <summary>
        ///// NPI抛小料（就是卷料）时调用，上传抛料信息
        ///// 现在NPI使用的，本来准备改成次数累计但是太麻烦了，所以暂时还是传1
        ///// </summary>
        //public bool SendTossingInfo(int nozzle, EN_TossingCode tossingCode, int times)
        //{
        //    DateTime time = DateTime.Now;
        //    JObject obj = new JObject();
        //    obj.Add("unit_sn", "1234567890");
        //    obj.Add("pass", false.ToString());
        //    obj.Add("input_time", time.AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));
        //    obj.Add("output_time", time.ToString("yyyy-MM-ddTHH:mm:ss.ffzz00"));

        //    JObject serials = new JObject();
        //    obj.Add("serials", serials);
        //    serials.Add("sub_id", "S1");//注意，根据机台实际情况修改
        //    serials.Add("error_message", "Tossing Error");
        //    //TossHSGS1-1-03（Toss + 物料名称ALT/FLE + 机架S1/S3 + 吸嘴号 + 原因代码）
        //    string code = $"TossALTS1-{nozzle}-{(int)tossingCode:D2}";//注意，根据机台实际情况修改
        //    serials.Add("Tossing_code", code);
        //    string detail = Enum.GetName(typeof(EN_TossingCode), tossingCode);
        //    serials.Add("error_detail", detail);

        //    JObject data = new JObject();
        //    obj.Add("data", data);
        //    data.Add("sw_version", SoftwareVersion);
        //    data.Add("hive_state", (int)HiveStatus);
        //    data.Add("Bay", "1");
        //    data.Add("output_time_CH", time.ToString("yyyy-MM-ddTHH:mm:ss.ff"));
        //    data.Add("Tossing_1", times);

        //    return PostToServer("machinedata", obj.ToString(Formatting.None));
        //}

        /// <summary>
        /// 尝试修改当前Hive状态。
        /// 如果输入的状态与当前状态不一致，则更新，并向hive发送状态变化；否则直接返回。
        /// </summary>
        /// <param name="state"></param>
        private void ChangeHiveState(EN_HiveStatus state)
        {
            if (HiveStatus == state || (int)state < 1 || (int)state > 5)
            {
                return;
            }
            DateTime stateChangeTime = DateTime.Now;
            SendState(stateChangeTime, state);
            if ((int)state <= 2)
            {
                ErrMessage = "";
                ErrDetail = "";
                ErrCode = "";
            }
            TimeSpan ts;
            if (stateChangeTime.Date != lastChangeStatusTime.Date)
            {
                //刚好跨天情况下，应该先补全上一天时间
                ts = lastChangeStatusTime.Date.AddDays(1) - lastChangeStatusTime;
                hiveStatusTime[hiveStatusTime.Length - 1].AddTime(HiveStatus, ts.TotalSeconds);
                for (int i = 1; i < hiveStatusTime.Length; i++)
                {
                    hiveStatusTime[i - 1] = hiveStatusTime[i].DeepCopy();
                }
                hiveStatusTime[hiveStatusTime.Length - 1].Clear();
                lastChangeStatusTime = lastChangeStatusTime.Date.AddDays(1);
            }
            //这里的两个时间必定在同一天
            ts = stateChangeTime - lastChangeStatusTime;
            hiveStatusTime[hiveStatusTime.Length - 1].AddTime(HiveStatus, ts.TotalSeconds);
            lastChangeStatusTime = stateChangeTime;
            HiveStatus = state;
            _ = Task.Run(() =>
            {
                //SendState(stateChangeTime, state);
                WriteHiveStatusTime();
            });
        }

        public static string GetBufferSHA1(byte[] buffer)
        {
            var hash = SHA1.Create();
            byte[] data = hash.ComputeHash(buffer);
            return BitConverter.ToString(data, 0, data.Length).Replace("-", "");
        }

        public static string GetFileSHA1(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "";
            }
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var hash = SHA1.Create();
                    byte[] data = hash.ComputeHash(fs);
                    return BitConverter.ToString(data, 0, data.Length).Replace("-", "");
                }
            }
            catch (Exception ex)
            {
                //有可能文件被占用，所以复制一份再读取
                string filepath2 = filePath + ".bak";
                try
                {
                    File.Copy(filePath, filepath2, true);
                    return GetFileSHA1(filepath2);
                }
                finally
                {
                    File.Delete(filepath2);
                }
            }
        }

        public string GetMainSoftwareSHA1()
        {
            string filePath = Process.GetCurrentProcess().MainModule.FileName;
            return GetFileSHA1(filePath);
        }

        public string GetVisionSoftwareSHA1()
        {
            string filePath = _stepStatus.ParamManager.CameraParam.VisionPath;
            return GetFileSHA1(filePath);
        }

        public string GetConfigFileSHA1()
        {
            StringBuilder sb = new StringBuilder();
            List<string> paths = _stepStatus.ParamManager.GetPaths();
            foreach (var path in paths)
            {
                sb.Append(GetFileSHA1(path));
            }
            return GetBufferSHA1(Encoding.UTF8.GetBytes(sb.ToString()));
        }
    }
}
