/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-14
 * 说明：（PLC通讯逻辑）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using CSSIModbus;
using Newtonsoft.Json.Linq;
using QA.Business.CacheParam;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.Alarm;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls;
using QA_Infrastructure.GeneralTool;
using QA_Infrastructure.NLogOut;


namespace QA.Business.Component.PLC
{
    public class PlcTrigInfo
    {
        public bool IsStart { get; set; } = false;
        public bool IsReset { get; set; } = false;
        public bool IsEStop { get; set; } = false;
        public bool IsStop { get; set; } = false;
    }

    public class PLC_Component : IPLC
    {
        #region Field
        public const int EVERY_ADDR_OFFSET = 1000;
        private PLCParam _plcParam = new PLCParam();
        private volatile CancellationTokenSource _cancellationTokenSource;
        private CacheParamManager _cacheParamManager;
        private CancellationToken _cancellationToken;
        private IEventAggregator _eventAggregator;
        private int _clientId;
        private ushort[] _readValue = new ushort[256];
        private ushort[] _readValue2 = new ushort[20];
        private const ushort _readValue2Add = 5000;
        private ushort[] _writeValue = new ushort[256];
        #endregion

        #region Property
        public bool IsCurReset { get; private set; }
        public bool IsCurStart { get; private set; }
        public bool IsCurEStop { get; private set; }
        public bool IsCurStop { get; private set; }

        public string ComponentName { get; set; }
        public bool IsConnected { get; set; }
        public IParam Param { get; set; }
        public bool IsCurSimulateRun { get; private set; }
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
        #endregion

        #region Constructor
        public PLC_Component()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _plcParam = IoC.Get<PLCParam>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }
        #endregion

        #region Method

        #region base

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool Initial(IParam param)
        {
            Param = _plcParam = param as PLCParam;
            _readValue = new ushort[_plcParam.FromPLC_AddrNum];//单次读取20ms左右
            _writeValue = new ushort[_plcParam.ToPLC_AddrNum];
            return true;
        }

        public bool Connect()
        {
            try
            {
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }
                    return CSSIModbusRTUInterface.CSSIModbusRTU_InitSerialPort(_plcParam.ComPort);
                }
                _clientId = CSSIModbusTCPInterface.CSSIModbusTCP_InitTcpClient(_plcParam.IP, _plcParam.Port, 500/*_plcParam.Timeout*/);
                return _clientId != -1;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }

        public bool DisConnect()
        {
            try
            {
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }
                    CSSIModbusRTUInterface.CSSIModbusRTU_Exit();
                    return true;
                }
                CSSIModbusTCPInterface.CSSIModbusTCP_Exit();
                return true;
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
            Task.Factory.StartNew(async () =>
            {
                //避免偶发的只存在一瞬间急停的情况直接被判定为急停，必须连续两次循环结果都为急停才算急停
                bool estopFlag = false;
                while (true)
                {
                    await Task.Delay(Param.BUse ? 100 : 1000, _cancellationToken);
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
                            DisConnect();
                            IsConnected = Connect();
                        }
                        if (!GetAddrMutiValue(_plcParam.StationID, _plcParam.FromPLC_StartAddr, _plcParam.FromPLC_AddrNum, ref _readValue))
                        {
                            IsConnected = false;
                            continue;
                        }
                        OnReadValueRefresh(_readValue);
                        if (!GetAddrMutiValue(_plcParam.StationID, 5000, (ushort)_readValue2.Length, ref _readValue2))
                        {
                            IsConnected = false;
                            continue;
                        }
                        PublishPlcIOMode();
                        if (IsEStop())
                        {
                            if (!IsCurEStop)
                            {
                                if (!estopFlag)
                                {
                                    estopFlag = true;
                                    continue;
                                }
                                _eventAggregator.Publish(new PlcTrigInfo() { IsEStop = true }, action => { Task.Run(action); });
                            }
                            IsCurEStop = true;
                            IsCurReset = false;
                            IsCurStop = false;
                            IsCurStart = false;
                            continue;
                        }
                        else
                        {
                            IsCurEStop = false;
                            estopFlag = false;
                        }
                        if (IsReset())
                        {
                            if (!IsCurReset)
                            {
                                _eventAggregator.Publish(new PlcTrigInfo() { IsReset = true }, action => { Task.Run(action); });
                            }
                            IsCurReset = true;
                            IsCurStop = false;
                            IsCurStart = false;
                            continue;
                        }
                        else
                        {
                            IsCurReset = false;
                        }
                        if (IsStop())
                        {
                            if (!IsCurStop)
                            {
                                _eventAggregator.Publish(new PlcTrigInfo() { IsStop = true }, action => { Task.Run(action); });
                            }
                            IsCurStop = true;
                            IsCurStart = false;
                            continue;
                        }
                        else
                        {
                            IsCurStop = false;
                        }
                        if (IsStart())
                        {
                            if (!IsCurStart)
                            {
                                _eventAggregator.Publish(new PlcTrigInfo() { IsStart = true }, action => { Task.Run(action); });
                            }
                            IsCurStart = true;
                            continue;
                        }
                        else
                        {
                            IsCurStart = false;
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

        protected virtual void OnReadValueRefresh(ushort[] obj)
        {
            ReadValueRefresh?.Invoke(obj);
        }

        public bool Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                if (!_plcParam.BUseModbusTcp)
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

        private static readonly Dictionary<int, string[]> errorInfo = new Dictionary<int, string[]>()
        {
            { 4000, new [] {
                "待料阻挡气缸原点异常",
                "待料阻挡气缸动点异常",
                "贴合顶升气缸原点异常",
                "贴合顶升气缸动点异常",
                "贴合阻挡气缸原点异常",
                "贴合阻挡气缸动点异常",
                "保压顶升气缸原点异常",
                "保压顶升气缸动点异常",
            }},
            { 4001, new [] {
                "保压阻挡气缸原点异常",
                "保压阻挡气缸动点异常",
                "保压上下1气缸原点异常",
                "保压上下1气缸动点异常",
                "保压上下2气缸原点异常",
                "保压上下2气缸动点异常",
                "保压上下3气缸原点异常",
                "保压上下3气缸动点异常",
            }},
            { 4002, new [] {
                "保压上下4气缸原点异常",
                "保压上下4气缸动点异常",
                "上相机气缸原点异常",
                "上相机气缸动点异常",
            }},
            { 4003, new [] {
                "正流流线轴1异常报警",
                "正流流线轴2异常报警",
                "回流流线轴异常报警",
                "流线调宽轴1异常报警",
                "流线调宽轴2异常报警",
                "保压X轴异常报警",
                "保压Z轴异常报警",

            }},
            //{ 4004, new [] {
            //    "",
            //    "",
            //    "",
            //    "",

            //}},
            { 4005, new [] {
                "设备急停中",
                "设备气压异常报警",
                "安全门1-2异常报警",
                "安全门3-4异常报警",
                "安全门5-6异常报警",
                "安全门7-8异常报警",
            }},
            { 4006, new [] {
                "PC失联",
                "PC端报警",
                "待料位检测有料异常",
                "待料位载具防呆检测异常",
                "贴合位检测有料异常",
                "贴合位载具顶升异常",
                "保压位检测有料异常",
                "保压位载具顶升异常",
            }},
            { 4007, new [] {
                "设备初始化复位失败",
                "负压异常报警",
                "安全光栅异常感应报警",
                "飞达全部异常报警",
                "保压头1使用次数已到报警",
                "保压头2使用次数已到报警",
                "保压头3使用次数已到报警",
                "保压头4使用次数已到报警",

            }},
        };
        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (_plcParam.BUse)
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
                        Index = count + 1
                    });
                }
                //不需要安全门报警，已经包含在errorInfo中
                //if (IsSafeDoorAlarm())
                //{
                //    var count = alarmInfos.Count;
                //    alarmInfos.Add(new AlarmInfoModel()
                //    {
                //        AlarmLevel = EN_WARN_LEVEL.Error,
                //        AlarmModule = EN_WarnModules.Plc,
                //        AlarmMsg = "安全门打开",
                //        Datetime = DateTime.Now,
                //        ErrorCode = 0,
                //        Index = count + 1
                //    });
                //}
                //不能有飞达未到位报警，否则换料时hive状态为downtime而非planned dt
                //if (!IsFeederLocked())
                //{
                //    var count = alarmInfos.Count;
                //    alarmInfos.Add(new AlarmInfoModel()
                //    {
                //        AlarmLevel = EN_WARN_LEVEL.Error,
                //        AlarmModule = EN_WarnModules.Plc,
                //        AlarmMsg = "飞达未到位",
                //        Datetime = DateTime.Now,
                //        ErrorCode = 0,
                //        Index = count + 1
                //    });
                //}
                ushort valueAlarm;
                foreach (var p in errorInfo)
                {
                    valueAlarm = 0;
                    if (GetAddrValue(0, (ushort)p.Key, ref valueAlarm))
                    {
                        for (byte bit = 0; bit < p.Value.Length; bit++)
                        {
                            if (GetUshortOneBitStatus(valueAlarm, bit) && !string.IsNullOrEmpty(p.Value[bit]))
                            {
                                var count = alarmInfos.Count;
                                alarmInfos.Add(new AlarmInfoModel()
                                {
                                    AlarmLevel = EN_WARN_LEVEL.Error,
                                    AlarmModule = EN_WarnModules.Plc,
                                    AlarmMsg = p.Value[bit],
                                    Datetime = DateTime.Now,
                                    ErrorCode = p.Key * 16 + bit,
                                    Index = count + 1
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断PLC是否启动
        /// </summary>
        /// <param name="plcidx"></param>
        /// <returns></returns>
        private bool IsStart()
        {
            return _readValue[_plcParam.FromPLC_MachineStatus - _plcParam.FromPLC_StartAddr] == (int)PlcToPcMachineStatus.运行中;
            //return (_readValue[_plcParam.FromPLC_RunStatus - _plcParam.FromPLC_StartAddr] & (1 << 1)) > 0 ? true : false;
        }
        /// <summary>
        /// 判断PLC是否复位
        /// </summary>
        /// <param name="plcidx"></param>
        /// <returns></returns>
        private bool IsReset()
        {
            return _readValue[_plcParam.FromPLC_MachineStatus - _plcParam.FromPLC_StartAddr] == (int)PlcToPcMachineStatus.复位中;
            //return (_readValue[_plcParam.FromPLC_RunStatus - _plcParam.FromPLC_StartAddr] & (1 << 2)) > 0 ? true : false;
        }
        /// <summary>
        /// 判断PLC是否停止（按停止按钮或飞达光栅触发）
        /// </summary>
        /// <returns></returns>
        private bool IsStop()
        {
            if (_readValue[_plcParam.FromPLC_MachineStatus - _plcParam.FromPLC_StartAddr] == (int)PlcToPcMachineStatus.暂停中)
            {
                return true;
            }
            if (_readValue[_plcParam.FromPLC_MachineStatus - _plcParam.FromPLC_StartAddr] == (int)PlcToPcMachineStatus.报警中)
            {
                return true;
            }
            return false;
            //return (_readValue[_plcParam.FromPLC_RunStatus - _plcParam.FromPLC_StartAddr] & (1 << 3)) > 0 ? true : false;
        }
        /// <summary>
        /// 判断PLC是否急停，注意为1表示正常
        /// </summary>
        /// <returns></returns>
        public bool IsEStop()
        {
            return _readValue[_plcParam.FromPLC_MachineStatus - _plcParam.FromPLC_StartAddr] == (int)PlcToPcMachineStatus.急停中;
            //return (_readValue[_plcParam.FromPLC_RunStatus - _plcParam.FromPLC_StartAddr] & (1 << 4)) > 0 ? false : true;
        }

        #endregion

        #region FA06-005-C

        /// <summary>
        /// 检测是否为流线模式
        /// </summary>
        /// <returns></returns>
        public bool IsNotNeedProcessMode()
        {
            return _readValue2[_plcParam.FromPLC_NotNeedProcessModeAddr - _readValue2Add] == 1;
        }

        /// <summary>
        /// 检测安全门是否报警
        /// </summary>
        /// <returns></returns>
        public bool IsSafeDoorAlarm()
        {
            return false;
            //return _readValue[_plcParam.FromPLC_SafeDoorAlarmAddr - _plcParam.FromPLC_StartAddr] == 1;
        }

        /// <summary>
        /// 读寿命
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetAlive(int index)
        {
            ushort addr = (ushort)(7800 + index * 2);
            ushort[] values = new ushort[2];
            if (!GetAddrMutiValue(0, addr, 2, ref values))
            {
                return -1;
            }
            return values[0] + values[1] * 0x10000;
        }

        /// <summary>
        /// 判断载具是否到位（已经顶起）
        /// </summary>
        /// <returns></returns>
        public bool IsCarrierReady()
        {
            return _readValue2[_plcParam.FromPLC_ReadySignal - _readValue2Add] == 1;
        }

        /// <summary>
        /// 判断PLC是否为空跑模式
        /// </summary>
        /// <returns></returns>
        public bool IsSimulateRun()
        {
            return _readValue2[_plcParam.FromPLC_SimulateRunSignal - _readValue2Add] == 1 || _readValue2[_plcParam.FromPLC_SimulateRunSignal2 - _readValue2Add] == 1;
        }

        /// <summary>
        /// 返回飞达是否锁紧（推到最里面）
        /// </summary>
        /// <returns></returns>
        public bool IsFeederLocked()
        {
            FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
            ushort status = 0;
            if (feederId == FeederId.左飞达)
            {
                status = _readValue2[_plcParam.FromPLC_FeederInPlaceStatus - _readValue2Add];
            }
            else
            {
                status = _readValue2[_plcParam.FromPLC_Feeder2InPlaceStatus - _readValue2Add];
            }
            return status == 1;
        }

        /// <summary>
        /// 返回飞达有没有物料，任意一个点位有就返回true
        /// </summary>
        /// <returns></returns>
        public bool IsFeederTapeReady()
        {
            ushort status = 0;
            if (_cacheParamManager.HomeUiParam.Enable.FeederId == FeederId.左飞达)
            {
                status = _readValue2[_plcParam.FromPLC_FeederTypeStatus - _readValue2Add];
            }
            else
            {
                status = _readValue2[_plcParam.FromPLC_Feeder2TypeStatus - _readValue2Add];
            }
            return status == 1;
        }

        /// <summary>
        /// 返回飞达某个点位有没有物料，序号从0开始
        /// </summary>
        /// <returns></returns>
        public bool IsFeederTapeReady(int index)
        {
            ushort status = 0;
            if (_cacheParamManager.HomeUiParam.Enable.FeederId == FeederId.左飞达)
            {
                status = _readValue2[_plcParam.FromPLC_FeederTypeStatus - _readValue2Add];
            }
            else
            {
                status = _readValue2[_plcParam.FromPLC_Feeder2TypeStatus - _readValue2Add];
            }
            return status == 1;
        }

        /// <summary>
        /// 触发一次飞达送料
        /// </summary>
        /// <returns></returns>
        public void TrigFeederConveyType()
        {
            ushort status = 0;
            if (_cacheParamManager.HomeUiParam.Enable.FeederId == FeederId.左飞达)
            {
                GetAddrValue(0, _plcParam.FromPLC_FeederGetTape, ref status);
            }
            else
            {
                GetAddrValue(0, _plcParam.FromPLC_Feeder2GetTape, ref status);
            }
            //为1，则先置为0
            if (status == 1)
            {
                if (_cacheParamManager.HomeUiParam.Enable.FeederId == FeederId.左飞达)
                {
                    SetAddrValue(0, _plcParam.FromPLC_FeederGetTape, 0);
                }
                else
                {
                    SetAddrValue(0, _plcParam.FromPLC_Feeder2GetTape, 0);
                }
                Thread.Sleep(200);
            }
            if (_cacheParamManager.HomeUiParam.Enable.FeederId == FeederId.左飞达)
            {
                SetAddrValue(0, _plcParam.FromPLC_FeederGetTape, 1 << 0);
            }
            else
            {
                SetAddrValue(0, _plcParam.FromPLC_Feeder2GetTape, 1 << 0);
            }
            //一段时间后自动重置标记
            _ = Task.Run(() =>
            {
                Thread.Sleep(200);
                if (_cacheParamManager.HomeUiParam.Enable.FeederId == FeederId.左飞达)
                {
                    SetAddrValue(0, _plcParam.FromPLC_FeederGetTape, 0);
                }
                else
                {
                    SetAddrValue(0, _plcParam.FromPLC_Feeder2GetTape, 0);
                }
            });
        }
        /// <summary>
        /// 相机气缸移动
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        public bool FeederCDDCylinderSS(FeederId feederId)
        {
            SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_FeederCDDCylinderReachOut, (ushort)feederId);
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < 5)
            {
                if (!IsConnected)
                {
                    return false;
                }
                if (_readValue2[_plcParam.FromPLC_FeederCDDCylinderReachOut - _readValue2Add] == (ushort)feederId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 设置各个穴位是否需要保压，bitn表示n+1穴
        /// </summary>
        /// <param name="plcidx"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetNeedDwell(int dwellStatus)
        {
            return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_NeedDwellAddr, (ushort)dwellStatus);
        }

        /// <summary>
        /// 设置加工结果
        /// </summary>
        /// <param name="plcidx"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetWorkResult(ushort workresult)
        {
            return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_WorkResultAddr, workresult);
        }
        /// <summary>
        /// 吸废膜
        /// </summary>
        /// <returns></returns>
        public bool SetSuctionFilm(ushort result)
        {
            return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_SuctionFilmAddr, result);
        }

        /// <summary>
        /// 判断吸废膜开关状态
        /// </summary>
        /// <returns></returns>
        public bool GetSuctionFilm()
        {
            ushort status = 0;
            GetAddrValue(0, _plcParam.ToPLC_SuctionFilmAddr, ref status);
            return status == 1;
        }

        /// <summary>
        /// 写寿命，7800开始，2个地址一个数
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool AddAlive(int nozzleNo)
        {
            int index = nozzleNo - 1 + 4;
            ushort addr = (ushort)(7800 + index * 2);
            int count = GetAlive(index);
            if (count == -1)
            {
                return false;
            }
            count++;
            return SetAddrMultiValue(_plcParam.StationID, addr, new ushort[] { (ushort)(count % 0x10000), (ushort)(count / 0x10000) });
        }

        #endregion

        #region 告诉PLC上位机的运行状态

        /// <summary>
        /// 设置心跳，大约2s一次
        /// </summary>
        /// <returns></returns>
        public bool SetHeartBeat()
        {
            return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_HeartBeat, 1);
        }

        PcToPlcMachineStatus pcToPlcMachineStatus = PcToPlcMachineStatus.空闲中;
        /// <summary>
        /// 设置PC设备状态
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool SetPcMachineStatus(PcToPlcMachineStatus status)
        {
            if (status == PcToPlcMachineStatus.报警警告清除)
            {
                status = pcToPlcMachineStatus;
            }
            if (!(status == PcToPlcMachineStatus.报警中 || status == PcToPlcMachineStatus.警告中))
            {
                pcToPlcMachineStatus = status;
            }
            return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_RunStausAddr, (ushort)status);
        }
        public bool SetIdle()
        {
            return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_RunStausAddr, 9);
        }
        //public bool SetSwReady(bool ready)
        //{
        //    return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_SwReady, ready ? (ushort)1 : (ushort)0);
        //}

        /// <summary>
        /// 等价于按下机台启动/暂停/复位按钮
        /// </summary>
        public bool TrigPlcButton(EN_Plc_TrigButton button)
        {
            return SetAddrValue(_plcParam.StationID, _plcParam.ToPLC_PlcButton, (ushort)button);
        }

        #endregion

        #region Modbus基础指令、地址读写、字符读写
        public bool GetUshortOneBitStatus(ushort val, byte index)
        {
            if (index > 15)
            {//Bit越界
                return false;
            }
            return (val & (1 << index)) > 0 ? true : false;
        }
        public bool GetAddrValue(byte id, ushort addr, ref ushort value)
        {
            try
            {
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_GetAddrValue(_plcParam.ComPort, id, addr, true, ref value);
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
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_GetAddrMutiValue(_plcParam.ComPort, id, addr, true, num, valueUshorts);
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
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }
                    ret = CSSIModbus.CSSIModbusRTUInterface.CSSIModbusRTU_GetAddrMutiValue(_plcParam.ComPort, id, addr, true, num, value);
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
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_SetAddrValue(_plcParam.ComPort, id, addr, value);
                }

                return CSSIModbusTCPInterface.CSSIModbusTCP_SetAddrValue(_clientId, id, addr, value);
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }
        }
        public bool SetFloatToPLC(byte id, ushort addr, float value)
        {
            ushort[] ushorts = GeneralTools.GetUshortArrayFromFloat(value);
            for (int i = 0; i < ushorts.Length; i++)
            {
                if (!SetAddrValue(id, (ushort)(addr + i), ushorts[i]))
                    return false;
            }
            //byte[] bytes = GeneralTools.GetBytesFromFloat(value, En_DateFormat.ABCD);

            //if (bytes.Length != 4)
            //{
            //    return false;
            //}

            //ushort d1 = (ushort)((bytes[0] << 8) + bytes[1]);
            //ushort d2 = (ushort)((bytes[2] << 8) + bytes[3]);

            //if (!SetAddrValue(id, addr, d1))
            //    return false;
            //if (!SetAddrValue(id, (ushort)(addr + 1), d2))
            //    return false;

            return true;

        }
        public bool GetAddrFloatValue(byte id, ushort addr, ref float flt)
        {
            bool ret = false;
            ushort[] value = new ushort[2];
            try
            {
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }
                    ret = CSSIModbus.CSSIModbusRTUInterface.CSSIModbusRTU_GetAddrMutiValue(_plcParam.ComPort, id, addr, true, (ushort)value.Length, value);
                }
                else
                {
                    ret = CSSIModbus.CSSIModbusTCPInterface.CSSIModbusTCP_GetAddrMutiValue(_clientId, id, addr, true, (ushort)value.Length, value);
                }
                flt = GeneralTools.GetFloatFromUshortArray(value, 0, value.Length);
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return ret;
        }
        public bool SetAddrMultiValue(byte id, ushort addr, ushort[] value)
        {
            try
            {
                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }

                    return CSSIModbusRTUInterface.CSSIModbusRTU_SetAddrMutiValue(_plcParam.ComPort, id, addr, value);
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

                if (!_plcParam.BUseModbusTcp)
                {
                    if (string.IsNullOrEmpty(_plcParam.ComPort) || !SerialPortCtrl.GetPortNames().Contains(_plcParam.ComPort))
                    {
                        return false;
                    }
                    return CSSIModbus.CSSIModbusRTUInterface.CSSIModbusRTU_SetAddrMutiValue(_plcParam.ComPort, id, addr, value);
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

        #endregion

        /// <summary>
        /// 发布PLC IO状态
        /// </summary>
        public void PublishPlcIOMode()
        {
            PlcIOMode plcIOMode = new PlcIOMode()
            {
                CddIsPosition1 = _readValue2[_plcParam.FromPLC_FeederCDDCylinderReachOut - _readValue2Add] == 1,
                CddIsPosition2 = _readValue2[_plcParam.FromPLC_FeederCDDCylinderReachOut - _readValue2Add] == 2,
                FeederTypeStatus = _readValue2[_plcParam.FromPLC_FeederTypeStatus - _readValue2Add] == 1,
                Feeder2TypeStatus = _readValue2[_plcParam.FromPLC_Feeder2TypeStatus - _readValue2Add] == 1,
                FeederInPlaceStatus = _readValue2[_plcParam.FromPLC_FeederInPlaceStatus - _readValue2Add] == 1,
                Feeder2InPlaceStatus = _readValue2[_plcParam.FromPLC_Feeder2InPlaceStatus - _readValue2Add] == 1,
            };
            _eventAggregator.Publish(plcIOMode, action => { Task.Run(action); });
        }

        public EN_FeederStatus GetFeederInPlaceStatus()
        {
            PlcIOMode plcIOMode = new PlcIOMode()
            {
                FeederInPlaceStatus = _readValue2[_plcParam.FromPLC_FeederInPlaceStatus - _readValue2Add] == 1,
                Feeder2InPlaceStatus = _readValue2[_plcParam.FromPLC_Feeder2InPlaceStatus - _readValue2Add] == 1,
            };
            EN_FeederStatus _FeederStatus;
            if (!plcIOMode.FeederInPlaceStatus && !plcIOMode.Feeder2InPlaceStatus)
            {
                _FeederStatus = EN_FeederStatus.NoUse;
            }
            else if (plcIOMode.FeederInPlaceStatus && plcIOMode.Feeder2InPlaceStatus)
            {
                _FeederStatus = EN_FeederStatus.BothUse;
            }
            else if (plcIOMode.FeederInPlaceStatus)
            {
                _FeederStatus = EN_FeederStatus.Feeder1Using;
            }
            else
            {
                _FeederStatus = EN_FeederStatus.Feeder2Using;
            }
            return _FeederStatus;
        }

    }
}
