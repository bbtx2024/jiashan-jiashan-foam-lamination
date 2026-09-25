using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using HandyControl.Data;
using QA.Business.CacheParam;
using QA.Business.Component.Camera;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Model;
using QA.Business.Model.Alarm;
using QA.Business.Procedure;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using static QA.Business.Define.HiveAlarmDefine;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Steps
{
    public class StepStatus
    {
        #region Field
        private MotionGoogol_Component _mGoogol_Component;
        private PLC_Component _plc_Component;
        private Camera_Component _camera_Component;
        private CacheParamManager _cacheParamManager;
        private MES_Component _mes_Component;
        private Hive_Component _hive_Component;
        private ParamManager _paramManager;
        public ManualResetEvent ManualResetEvt_CtrlPlace = new ManualResetEvent(false);
        public AutoResetEvent AutoResetEvt_CtrlVisual = new AutoResetEvent(false);
        public ManualResetEvent AutoResetEvt_CtrlScanner = new ManualResetEvent(false);
        public AutoResetEvent AutoResetEvt_DownCam = new AutoResetEvent(false);
        public AutoResetEvent AutoResetEvt_Pressurize = new AutoResetEvent(false);
        private IEventAggregator _eventAggregator;
        private object obj = new object();
        private static bool createdInstance = false;
        private bool _mutex = false;
        public int[] NozzleInhaleNotReadyTimes1 = new int[4];//指示下视觉NG的【连续】次数
        public int[] NozzleInhaleNotReadyTimes2 = new int[4];//指示下视觉NG的【不连续】次数
        public bool[] PickTapeOk = new bool[4];
        public bool[] DownCamOk = new bool[4];
        public string[] TLNSns = new string[4];
        public string[] TLMSns = new string[4];
        public string[] tapeBaseDistance = new string[4];
        public string[] markToPhotoCenterOffsets = new string[12];
        public string TLMSn;
        /// <summary>
        /// 是否是第一次取料
        /// </summary>
        public bool isOnePick;
        public bool isEndCarrierFinish = false;
        public string pressurCarrierSN;
        public int CurrentCavNum = 0;
        public int CurrentNozeNum = 0;
        public int isCavityNull = 0;//载具空穴数量判断，如果连续3次空穴就判断整盘载具是空穴，此载具后续穴位不在进行空穴弹框提示

        public bool IFitCav10 = false;



        /// <summary>
        /// 只有空跑模式或视觉全部ok的穴位才会存进来
        /// </summary>
        public List<LaserSprayVisionPoint> CurNeedProcess = new List<LaserSprayVisionPoint>();
        /// <summary>
        /// 存储需要保压的穴位
        /// </summary>
        public List<LaserSprayVisionPoint> CurNeedDwell = new List<LaserSprayVisionPoint>();
        #endregion

        #region Property
        public CacheParamManager CacheParamManager { get; set; }
        public ParamManager ParamManager { get; set; }
        public GlobalVariable GlobalVariable { get; set; }
        public LaserSprayProcedure CurrentProcedure { get; set; }
        public OutputPerHour OutputPerHour { get; } = new OutputPerHour(DateTime.Now);
        public TossingPerHour TossingPerHour { get; } = new TossingPerHour(DateTime.Now);
        public HiveMachineStatusStatistic HiveMachineStatusStatistic { get; } = new HiveMachineStatusStatistic();
        public EN_RunStep NextStep1 { get; set; } = EN_RunStep.Idle;
        public EN_RunStep NextStep2 { get; set; } = EN_RunStep.Idle;
        public EN_RunStep NextStep3 { get; set; } = EN_RunStep.Idle;
        public EN_RunStep NextStep4 { get; set; } = EN_RunStep.Idle;
        /// <summary>
        /// 指示Station2是否可启动。
        /// 0表示Station1正在判断能否启动，1表示Station1判断可启动，2表示Station1判断不可启动。
        /// </summary>
        public int AllowStation2Start { get; set; } = 0;
        public int AllowStation3Start { get; set; } = 0;
        #endregion

        private HomeUiParam_Enable Enable { get => CacheParamManager.HomeUiParam.Enable; }
        public StepStatus()
        {
            if (createdInstance)
            {
                MessageBox.Error("已实例化StepStatus！");
                Environment.Exit(0);
            }
            createdInstance = true;
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _mes_Component = (MES_Component)IoC.Get<IMES>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _paramManager = IoC.Get<ParamManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            AutoResetEvt_CtrlVisual.Set();
            AutoResetEvt_CtrlScanner.Set();
            ManualResetEvt_CtrlPlace.Reset();
            AutoResetEvt_DownCam.Reset();
            for (int i = 0; i < TLNSns.Length; i++)
            {
                TLNSns[i] = "";
            }
            for (int i = 0; i < TLMSns.Length; i++)
            {
                TLMSns[i] = "";
            }
            CacheParamManager = IoC.Get<CacheParamManager>();
            ParamManager = IoC.Get<ParamManager>();
            GlobalVariable = IoC.Get<GlobalVariable>();
            //这里ParamManager还未读取，应该在InitAllComponent里面进行相关赋值（例如outPortsNozzleVacInhale）
        }

        #region 获取吸嘴相关信息（启用、飞达点位、下视觉点位、真空吸信号等）

        /// <summary>
        /// 返回指定序号吸嘴的启用禁用状态，true表示启用。
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <returns></returns>
        public bool UseNozzle(int nozzleNo)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            bool[] useNozzle = new[] {
                CacheParamManager.HomeUiParam.Enable.UseNozzle1,
                CacheParamManager.HomeUiParam.Enable.UseNozzle2,
            };
            return useNozzle[nozzleNo - 1];
        }

        /// <summary>
        /// 获取启用吸嘴的个数
        /// </summary>
        /// <returns></returns>
        public int GetEnableNozzleCount()
        {
            bool[] useNozzle = new[] {
                CacheParamManager.HomeUiParam.Enable.UseNozzle1,
                CacheParamManager.HomeUiParam.Enable.UseNozzle2,
            };
            int count = 0;
            for (int i = 0; i < useNozzle.Length; i++)
            {
                if (useNozzle[i])
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 返回指定序号吸嘴的左/右取料点位xyzr数组。
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public float[] FeederSingleTapePos(int nozzleNo, int index)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            if (Enable.FeederId == FeederId.左飞达)
            {
                if (index == 0)
                {
                    float[][] LeftFeederPos = new float[][] {
                    CacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos,
                    CacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos,
                };
                    return LeftFeederPos[nozzleNo - 1];
                }
                else
                {
                    float[][] RightFeederPos = new float[][] {
                    CacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos,
                    CacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos,
                };
                    return RightFeederPos[nozzleNo - 1];
                }
            }
            else
            {
                if (index == 0)
                {
                    float[][] LeftFeederPos = new float[][] {
                    CacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos,
                    CacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos,
                };
                    return LeftFeederPos[nozzleNo - 1];
                }
                else
                {
                    float[][] RightFeederPos = new float[][] {
                    CacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos,
                    CacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos,
                };
                    return RightFeederPos[nozzleNo - 1];
                }
            }
        }

        /// <summary>
        /// 获取吸嘴取料点补偿
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <returns></returns>
        public float[] FeederSingleTapePos(int nozzleNo)
        {
            return FeederSingleTapePos(nozzleNo, 0);
        }

        /// <summary>
        /// 返回指定序号吸嘴的取料点位xyzrr数组。
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public float[] FeederDoubleTapePos(bool is12)
        {
            return is12
                ? CacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos
                : CacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos;
        }
        /// <summary>
        /// 返回指定序号吸嘴的下视觉拍照位。
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <returns></returns>
        public float[] GetAxisNgSiloPos(int nozzleNo)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            float[][] axisNgSiloPoss = new float[][] {
                CacheParamManager.manualPositionParam.AxisNgSiloPos,
                CacheParamManager.manualPositionParam.AxisNgSiloPos2,
            };
            return axisNgSiloPoss[nozzleNo - 1];
        }

        /// <summary>
        /// 相机拍照
        /// </summary>
        /// <param name="isdodge">是否避让相机</param>
        /// <returns></returns>
        public bool FeederCDD(bool isdodge = true)
        {
        FeedCDD:
            if (isdodge)
            {
                //运动到Z轴安全位置
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2位置0失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                switch (feederId)
                {
                    case FeederId.左飞达:
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(_cacheParamManager.manualPositionParam.AxisFeeder1Pos, true))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[1]}", En_Logout_Type.Alarm, true);
                            return false;
                        }
                        break;
                    case FeederId.右飞达:
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(_cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[1]}", En_Logout_Type.Alarm, true);
                            return false;
                        }
                        break;
                    default:
                        break;
                }
            }
            //等待物料到位
            if (!TrigFeederConveyTape(true))
            {
                return false;
            }
            int CDDnum = 0;
        CDD:
            if (!_camera_Component.FeederCdd())
            {
                CDDnum++;
                if (CDDnum < ParamManager.CameraParam.FeederNGCDDnum)
                {
                    goto CDD;
                }
                if (_camera_Component.CDDNG("Feeder拍照"))
                {
                    goto FeedCDD;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 触发飞达送料直至有料
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <param name="isFirstNozzle"></param>
        /// <param name="isLastNozzle"></param>
        /// <returns></returns>
        public bool TrigFeederConveyTape(bool waitUntilTaprReady)
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
        GetTapeSN:
            //检查飞达是否锁紧
            if (!_plc_Component.IsFeederLocked())
            {
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                AlarmInfoModel alarm;
                if (feederId == FeederId.左飞达)
                {
                    alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"左飞达未锁紧", 104);
                }
                else
                {
                    alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"右飞达未锁紧", 105);
                }
                var result = MessageBox.Show($"飞达未锁紧！\n确定重新检测吗？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                _baseBiz.RemoveRunAlarm(alarm);
                if (result == MessageBoxResult.OK)
                {
                    goto GetTapeSN;
                }
                else
                {
                    return false;
                }
            }
            //空跑或已经有料就直接返回
            if (_plc_Component.IsSimulateRun() || _plc_Component.IsFeederTapeReady())
            {
                return true;
            }
            if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || ParamManager.MESParam.BUse)
            {
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                string tapeSN = feederId == FeederId.左飞达 ? ParamManager.MESParam.Feeder1TapeSN : ParamManager.MESParam.Feeder2TapeSN;
                //检查B2BTapeSN是否合规
                if (!_mes_Component.CheckTapeSNOk(out string info))
                {
                    //var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"未通过卷料SN检查", 111);
                    var result = MessageBox.Show($"未通过卷料SN检查，{info}，需要重新录入卷料SN！\n确定重新检测吗？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    //_baseBiz.RemoveRunAlarm(alarm);
                    if (result == MessageBoxResult.OK)
                    {
                        goto GetTapeSN;
                    }
                    else
                    {
                        return false;
                    }
                }
                //检测tape次数
                int count = CacheParamManager.HomeUiParam.TapeUseInfoParam.GetCountBySn(tapeSN);
                int maxCount = ParamManager.MESParam.TapeAlarmMaxUseCount;
                if (count >= maxCount)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{tapeSN}使用次数超过{maxCount}，必须更换卷料", En_Logout_Type.Alarm, true);

                    //var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"卷料使用次数到达上限", 112);
                    var result = MessageBox.Show($"卷料使用次数到达上限，需要更换卷料！\n确定重新检测吗？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    //_baseBiz.RemoveRunAlarm(alarm);
                    if (result == MessageBoxResult.OK)
                    {
                        goto GetTapeSN;
                    }
                    else
                    {
                        return false;
                    }
                }
                int alarmCount = ParamManager.MESParam.TapeAlarmMaxUseCount - ParamManager.MESParam.TapeAlarmTipLeftCount;
                if (count >= alarmCount)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"{tapeSN}已使用{alarmCount}次，请尽快更换卷料", En_Logout_Type.Alarm, true);
                }
            }

            //触发送料
            _plc_Component.TrigFeederConveyType();
            if (!waitUntilTaprReady)
            {
                return true;
            }
            //开始计时
            DateTime startTime = DateTime.Now;
            //判断5s内有没有获取到物料
            while (true)
            {
                if (_plc_Component.IsFeederTapeReady())
                {
                    return true;
                }
                if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                {
                    FeederId feederId = _cacheParamManager.HomeUiParam.Enable.GetCurFeederID();
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{feederId}送料超时", En_Logout_Type.Alarm, true);
                    //此时可能飞达料用完，需要换料，应该报警弹窗
                    var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{feederId}送料超时", 101+ (int)feederId);
                    var message = MessageBox.Show($"{feederId}送料超时，确定重新尝试送料吗？ 是：重新送料 否：取消送料 取消：切换飞达并重新送料", "提示信息", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (message == MessageBoxResult.Yes)
                    {
                        _baseBiz.RemoveRunAlarm(alarm);
                        return TrigFeederConveyTape(waitUntilTaprReady);
                    }
                    else if((message == MessageBoxResult.No))
                    {
                        _baseBiz.RemoveRunAlarm(alarm);
                        return false;
                    }
                    else
                    {
                        _baseBiz.RemoveRunAlarm(alarm);
                        _eventAggregator.Publish(new FunctionIsEnabledMessage() {}, action => { Task.Run(action); });
                        Thread.Sleep(3000);
                        return TrigFeederConveyTape(waitUntilTaprReady);

                    }
                }
                if (_mGoogol_Component.Exit()) return false;
            }
        }

        /// <summary>
        /// 清掉指定吸嘴上的物料
        /// </summary>
        /// <param name="nozzleNoSet">需要清料的吸嘴，传null表示是复位类型的清料（所有吸嘴都清，清完会回原点）</param>
        /// <returns></returns>
        public bool ThrowTapes(HashSet<int> nozzleNoSet, bool IsCDD = false, bool IsReset = false, EN_TossingCode tossingCode = EN_TossingCode.BreakVacuum, CarrierStatus carrierStatus = null, bool MaterialorVacuum = false)
        {
            //清料结束后是否移动到原点
            bool moveToResetPointAtEnd = false;
            if (nozzleNoSet == null)
            {
                nozzleNoSet = new HashSet<int> { 1, 2 };
                moveToResetPointAtEnd = true;
            }
            if (nozzleNoSet.Count == 0)
            {
                return true;
            }


            if (CacheParamManager.manualPositionParam.AxisNgSiloPos[1] >= ParamManager.MotionGoogolParam.SafeY2)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"吸嘴1抛料点位不在安全位置", En_Logout_Type.Alarm, true);
                MessageBox.Error("吸嘴1抛料点位不在安全位置！");
                return false;
            }
            if (CacheParamManager.manualPositionParam.AxisNgSiloPos2[1] >= ParamManager.MotionGoogolParam.SafeY2)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"吸嘴2抛料点位不在安全位置", En_Logout_Type.Alarm, true);
                MessageBox.Error("吸嘴2抛料点位不在安全位置！");
                return false;
            }

            //所有吸嘴开始上
            if (!_mGoogol_Component.SetAllCylindersUpDown(true, false))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"所有吸嘴开始上失败", En_Logout_Type.Alarm, true);
                return false;
            }
            //运动到Z轴安全位置
            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2位置0失败", En_Logout_Type.Alarm, true);
                return false;
            }
            //所有吸嘴上到位
            if (!_mGoogol_Component.IsAllCylindersUpDownReady(true))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"所有吸嘴未上到位", En_Logout_Type.Alarm, true);
                return false;
            }
            if (_mGoogol_Component.Exit()) return false;
            //判断要不要多抛一个
            if (CacheParamManager.HomeUiParam.Enable.PickOpportunity != EN_PickOpportunity.AfterUpCam
                && CacheParamManager.HomeUiParam.Enable.KeepNoTapeOnFeeder
                && GetEnableNozzleCount() % 2 == 0
                && !moveToResetPointAtEnd)
            {
                //无论是取料时发现平台有多余物料而抛料，还是由于吸嘴物料ng而抛料，都需要确认是否要多抛一个
                //如果启用偶数个吸嘴，且飞达物料数目与吸嘴抛料后应该取料数目同为奇数或同为偶数，则不需多抛；否则需要额外抛一个
                //（添加启用偶数个吸嘴作为条件原因是启用奇数个吸嘴时，正常是每次取奇数个，大概率会剩一个，如果仍抛就太频繁了）
                //需要额外抛一个的时候，优先选取同取吸嘴额外抛。例如3号抛料后应取料，而14不用，应优先选4号而非1号。
                //如果优先选取的吸嘴不能抛，则遍历所有吸嘴，选取第一个可作为多抛的吸嘴
                //计算吸嘴抛料后应该取料数目
                int needGetTapeCount = 0;
                if (!_mGoogol_Component.GetMaterialsReady(out bool[] sucStatus))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "获取两个个吸嘴真空吸信号失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    //有两种情况属于抛料后应该取料：1.启用且需要抛料（默认需要抛料的吸嘴一定有料） 2.启用且当前无物料
                    if (UseNozzle(nozzleNo) && (nozzleNoSet.Contains(nozzleNo) || !sucStatus[nozzleNo - 1]))
                    {
                        needGetTapeCount++;
                    }
                }
                //触发飞达送料，根据来了几个料决定要不要多抛
                if (!TrigFeederConveyTape(true))
                {
                    return false;
                }
                if (_mGoogol_Component.Exit()) return false;
                int feederTapeNum = 0;
                if (!_plc_Component.IsSimulateRun() && IsCDD && !IsReset && !Enable.IsFeederCheck)
                {
                    if (!FeederCDD(true))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"拍照失败", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    feederTapeNum = _camera_Component.feederCDDMode.GetTapeOKNum();
                }
                //判断是否同为奇数或同为偶数
                if (needGetTapeCount % 2 != feederTapeNum % 2)
                {
                    //不是同为奇数或同为偶数，应该多抛一个；优先选取同取吸嘴
                    bool find = false;
                    foreach (int nozzleNo in nozzleNoSet)
                    {
                        int extraThrowTapeNozzleNo = nozzleNo % 2 == 0 ? nozzleNo - 1 : nozzleNo + 1;
                        if (UseNozzle(extraThrowTapeNozzleNo) && sucStatus[extraThrowTapeNozzleNo - 1] && !nozzleNoSet.Contains(extraThrowTapeNozzleNo))
                        {
                            nozzleNoSet.Add(extraThrowTapeNozzleNo);
                            find = true;
                            break;
                        }
                    }
                    //遍历，选取第一个可以额外抛的
                    if (!find)
                    {
                        for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                        {
                            if (UseNozzle(nozzleNo) && sucStatus[nozzleNo - 1] && !nozzleNoSet.Contains(nozzleNo))
                            {
                                nozzleNoSet.Add(nozzleNo);
                                break;
                            }
                        }
                    }
                }
            }
            if (_mGoogol_Component.Exit()) return false;
            //遍历吸嘴抛料
            foreach (int nozzleNo in nozzleNoSet)
            {
                float[] posNgMaterial = GetAxisNgSiloPos(nozzleNo);
                //移动到NG料仓的X轴Y轴位置
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { posNgMaterial[0], posNgMaterial[1] }, true, true))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到吸嘴{nozzleNo}Ng料仓点点XY位置失败,Axis[X2,Y2]:{posNgMaterial[0].ToString("f2")},{posNgMaterial[1].ToString("f2")}", En_Logout_Type.Alarm, true);
                    return false;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, false, false))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{nozzleNo}号吸嘴开始下失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                //移动到NG料仓的Z轴位置
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posNgMaterial[2], true, true))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Ng料仓点Z位置:{posNgMaterial[2].ToString("f3")}失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                if (!_mGoogol_Component.IsCylinderUpDownReady(nozzleNo, false))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{nozzleNo}号吸嘴未下到位", En_Logout_Type.Alarm, true);
                    return false;
                }

                //需要清料的吸嘴关吸开吹->等待->关吹。由于气压较小，要一个个吹
                DateTime startTime = DateTime.Now;
                if (!_plc_Component.SetSuctionFilm(1))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"吸废膜开吸失败", En_Logout_Type.Alarm, true);
                    return false;
                }

                if (!_mGoogol_Component.SetVacuum(nozzleNo, false))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{nozzleNo}号吸嘴关吸开吹失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                Thread.Sleep(ParamManager.OtherSettingParam.OpenBreakTime);
                if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{nozzleNo}号吸嘴关吹失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, true))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{nozzleNo}号吸嘴未上到位", En_Logout_Type.Alarm, true);
                    return false;
                }
                if (!_plc_Component.SetSuctionFilm(0))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"吸废膜关吸失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                PickTapeOk[nozzleNo - 1] = false;
                if (_mGoogol_Component.Exit()) return false;

                /********2024/05/21新增抛料信息********/
                DateTime endTime = DateTime.Now;
                //发送小料抛料信息
                if (!IsReset)
                {
                    _hive_Component.SendTossingInfo(startTime, endTime, nozzleNo / 2, tossingCode);
                }
                //记录抛料信息用作写tray信息最后复检汇总展示抛料数据
                if (carrierStatus != null)
                {
                    carrierStatus.Tape_Isthrow[CurrentCavNum - 1] = 1;
                    carrierStatus.Tape_ThrowCount[CurrentCavNum - 1]++;
                    if (MaterialorVacuum)
                    {
                        carrierStatus.Tape_ThrowReason[CurrentCavNum - 1] = (int)EN_TossingCode.MaterialDeflect;
                    }
                    else
                    {
                        carrierStatus.Tape_ThrowReason[CurrentCavNum - 1] = (int)EN_TossingCode.BreakVacuum;
                    }
                }
                //运动到Z轴安全位置
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2位置0失败", En_Logout_Type.Alarm, true);
                    return false;
                }
            }
            if (_mGoogol_Component.Exit()) return false;
            //所有吸嘴上到位
            if (!_mGoogol_Component.SetAllCylindersUpDown(true))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"所有吸嘴未上到位", En_Logout_Type.Alarm, true);
                return false;
            }
            if (_mGoogol_Component.Exit()) return false;

            if (moveToResetPointAtEnd || IsReset)
            {
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { 0, 0 }, true, true))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到原点位置失败", En_Logout_Type.Alarm, true);
                    return false;
                }
                if (_mGoogol_Component.Exit()) return false;
            }
            return true;
        }

        /// <summary>
        /// 清楚所有吸嘴上的物料
        /// </summary>
        /// <param name="IsReset"></param>
        /// <returns></returns>
        public bool ThrowAllTapes(bool IsReset = false)
        {
            return ThrowTapes(null, false, IsReset);
        }

        /// <summary>
        /// 返回下视觉点位xyzr数组(固定位，非吸嘴)。
        /// </summary>
        /// <returns></returns>
        public float[] DownCameraPos()
        {
            return CacheParamManager.manualPositionParam.AxisDownCamera_NoPos;
        }
        /// <summary>
        /// 返回指定序号吸嘴的下视觉点位xyzr数组。
        /// </summary>
        /// <returns></returns>
        public float[] DownCameraPos(int nozzleIdx)
        {
            if (nozzleIdx == 1)
            {
                return _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos;
            }
            else
            {
                return _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos;
            }
        }

        #endregion

        #region 载具信息相关
        public List<CarrierStatus> Carriers = new List<CarrierStatus>();
        public void AddCarrier(CarrierStatus carrier)
        {
            lock (obj)
            {
                Carriers.Add(carrier);
            }
        }
        public CarrierStatus GetCurCarrier()
        {
            lock (obj)
            {
                return Carriers.Count > 0 ? Carriers[0] : null;
            }
        }
        public void RemoveCurCarrier()
        {
            lock (obj)
            {
                if (Carriers.Count > 0)
                {
                    Carriers.Remove(Carriers[0]);
                }
            }
        }
        public void ClearAllCarriers()
        {
            lock (obj)
            {
                Carriers.Clear();
            }
        }
        #endregion

        /// <summary>
        /// 关闭真空吸 -> 打开真空破 -> 等待 -> 气缸上抬 -> 关闭真空破
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <returns></returns>
        public bool SetCloseSucOpenBreakAndCylinderUp(int nozzleNo, int breakDelay = 200)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                return false;
            }
            //关吸开吹
            if (!_mGoogol_Component.SetVacuum(nozzleNo, false))
            {
                return false;
            }
            //等待吹一段时间
            Thread.Sleep(breakDelay);
            //气缸上抬
            if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, true))
            {
                return false;
            }
            //关闭吹
            if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 设置轴互锁状态。true表示一站（相机）可以在载具区域移动，false表示二站（吸嘴）可以在载具区域移动
        /// </summary>
        /// <param name="mutex"></param>
        public void SetMoveMutex(bool mutex)
        {
            lock (obj)
            {
                _mutex = mutex;
            }
        }

        /// <summary>
        /// 获取轴互锁状态。true表示一站（相机）可以在载具区域移动，false表示二站（吸嘴）可以在载具区域移动
        /// </summary>
        /// <returns></returns>
        public bool GetMoveMutex()
        {
            return _mutex;
        }

        /// <summary>
        /// 复位上视觉定位ok状态
        /// </summary>
        public void ResetIsUpCamFinishedState()
        {
            foreach (var item in CurrentProcedure.VisionPoints)
            {
                item.isUpCamFinished = false;
            }
        }

        public int GetIndexFromCurProcedure(int cavityNo)
        {
            int i = 0;
            foreach (var item in CurrentProcedure.VisionPoints)
            {
                if (cavityNo == item.CavityNum)
                {
                    break;
                }
                i++;
            }
            return i;
        }

        /// <summary>
        /// 上相机全部处理结束，返回true；否则返回false
        /// </summary>
        /// <returns></returns>
        public bool IsAllUpCamFinished()
        {
            foreach (var item in CurrentProcedure.VisionPoints)
            {
                //穴位启用并且未处理时，返回false
                if (item.IsUsed && !item.isUpCamFinished)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取Tray信息
        /// </summary>
        /// <param name="carrierStatus"></param>
        /// <returns></returns>
        public bool GetTray(CarrierStatus carrierStatus)
        {
            try
            {
                string previousTrayFile = $@"\\{ParamManager.ScannerParam.TrayIP}\tray\current\{carrierStatus.carrierSN}.txt";
                if (!File.Exists(previousTrayFile))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"未找到{carrierStatus.carrierSN}的Tray文件(Không tìm thấy tập tin Tray)", En_Logout_Type.Alarm, true);
                    return false;
                }
                using (StreamReader sr = new StreamReader(previousTrayFile))
                {
                    string line;
                    string[] strArr;
                    //载具码
                    line = sr.ReadLine();
                    if (line != carrierStatus.carrierSN)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第1行载具码不一致(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    //errorCode
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第2行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.errorCode[i] = int.Parse(strArr[i]);
                        if (carrierStatus.errorCode[i] < (int)EN_TrayStatus.Empty || carrierStatus.errorCode[i] > (int)EN_TrayStatus.XYOverRange2)
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第2行穴位{i + 1}数据异常(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                            return false;
                        }
                    }
                    //sip码
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第3行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.sipSN[i] = strArr[i];
                    }
                    //Tape贴装吸嘴
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第4行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.Tape_Nozzle[i] = int.Parse(strArr[i]);
                    }
                    //Tape贴装压力
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Scan->{carrierStatus.carrierSN}.txt第5行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.Tape_PastePress[i] = float.Parse(strArr[i]);
                    }
                    //Tape保压头
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Scan->{carrierStatus.carrierSN}.txt第6行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.Tape_Indenter[i] = int.Parse(strArr[i]);
                    }
                    //Tape SN
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第7行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.Tape_SN[i] = strArr[i];
                    }
                    //Tape 是否抛料
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第8行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.Tape_Isthrow[i] = int.Parse(strArr[i]);
                    }
                    //Tape 抛料次数
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第9行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.Tape_ThrowCount[i] = int.Parse(strArr[i]);
                    }
                    //Tape 抛料类型
                    line = sr.ReadLine();
                    strArr = line.Split(',');
                    if (strArr.Length != carrierStatus.errorCode.Length)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{carrierStatus.carrierSN}.txt第10行个数{strArr.Length}不等于{carrierStatus.errorCode.Length}(Tray phân tích thất bại)", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        carrierStatus.Tape_ThrowReason[i] = int.Parse(strArr[i]);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"未能获取{carrierStatus.carrierSN}的Tray信息(Tray phân tích thất bại)，{ex.Message}", En_Logout_Type.Alarm, true);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Alarm);
                return false;
            }
        }

        /// <summary>
        /// 保存并备份Tray信息
        /// </summary>
        public bool BackupAndWriteTray(CarrierStatus carrierStatus)
        {
            try
            {
                DateTime nowTime = DateTime.Now;
                string backupDir = "D://tray//backup//" + nowTime.ToString("yyyy-MM-dd") + "//";
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
                string backupTrayFile = backupDir + nowTime.ToString("HHmmss") + "_" + carrierStatus.carrierSN + ".txt";
                using (StreamWriter sw = new StreamWriter(backupTrayFile, false))
                {
                    //载具码
                    sw.WriteLine(carrierStatus.carrierSN);
                    //errorCode
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.errorCode[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //sip码
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.sipSN[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //贴装吸嘴
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.Tape_Nozzle[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //压力
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.Tape_PastePress[i].ToString("F2"));
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //保压头
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.Tape_Indenter[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //Tape SN
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.Tape_SN[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //Tape 是否抛料
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.Tape_Isthrow[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //Tape 抛料次数
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.Tape_ThrowCount[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //Tape 抛料类型
                    sb = new StringBuilder();
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.Tape_ThrowReason[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                    //判断是否为空穴，将信息写入Tary穴位文件中
                    sb = new StringBuilder();
                    int dwellStatus = 0;
                    for (int i = 0; i < carrierStatus.errorCode.Length; i++)
                    {
                        sb.Append(carrierStatus.isEmpty[i]);
                        if (i != carrierStatus.errorCode.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sw.WriteLine(sb.ToString());
                }
                //写入Tray信息
                string currentTrayDir = $@"D:\tray\current";
                if (!Directory.Exists(currentTrayDir))
                {
                    Directory.CreateDirectory(currentTrayDir);
                }
                string currentTrayFile = $@"{currentTrayDir}\{carrierStatus.carrierSN}.txt";
                File.Copy(backupTrayFile, currentTrayFile, true);
                return true;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"未能备份并写入{carrierStatus.carrierSN}的Tray信息，{e.Message}", En_Logout_Type.Exception, true);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception);
                return false;
            }
        }

        /// <summary>
        /// 载具处理完毕时调用该方法，告诉plc释放载具，并写tray等
        /// </summary>
        /// <returns></returns>
        public bool CarrierFinish(CarrierStatus carrierStatus)
        {
            //把当前载具SN写给保压位
            pressurCarrierSN = carrierStatus.carrierSN;
            //强制写tray
            if (!BackupAndWriteTray(carrierStatus))
            {
                return false;
            }
            //构建分组信息
            // _camera_Component.AddCarrierGroupInfo(carrierStatus);
            //在新线程中执行分组指令
            _ = Task.Run(() =>
            {
                _camera_Component.MoveUnusedImgs(false);
            });
            //if (ParamManager.MESParam.TapeNames == TapeNames.Flex_GND_Tape)
            //{
            //    //保存信息到表格
            //    string dir0 = $@"D:\QKProject\Data\Product";
            //    if (!Directory.Exists(dir0))
            //    {
            //        Directory.CreateDirectory(dir0);
            //    }
            //    string file = $@"{dir0}\{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
            //    bool exists = File.Exists(file);
            //    using (StreamWriter sw = new StreamWriter(file, true))
            //    {
            //        if (!exists)
            //        {
            //            //载具时间（年月日） 载具时间（时分秒） 载具码 穴位号 上视觉结果 Sip码 吸嘴号
            //            sw.WriteLine("载具时间年月日,载具时间时分秒,载具码,穴位号,穴位状态,Sip码,吸嘴号,CenterX,CenterY,CenterA,TapeX,TapeY,TapeA");
            //        }
            //        try
            //        {
            //            for (int idx = 0; idx < 12; idx++)
            //            {
            //                int[] arr = { 0, 3, 6, 9, 1, 4, 7, 10, 2, 5, 8, 11 };
            //                int i = arr[idx];
            //                //时间、载具码、穴位号、吸嘴号、信息
            //                sw.WriteLine(carrierStatus.scanTime.ToString("yyyy/MM/dd") + "," + carrierStatus.scanTime.ToString("HH:mm:ss") + "," +
            //                    carrierStatus.carrierSN + "," + (i + 1) + "," + carrierStatus.cavStateStr[i] + "," +
            //                    carrierStatus.sipSN[i] + "," + carrierStatus.Flex_GND_Tape_Nozzle[i] + "," +
            //                    carrierStatus.markToPhotoCenterOffsets[i] + "," + carrierStatus.tapeBaseDistance[i]);
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.Message, En_Logout_Type.Exception, true);
            //        }
            //    }
            //}
            //else
            //{
            //    //保存信息到表格
            //    string dir0 = $@"D:\QKProject\Data\Product";
            //    if (!Directory.Exists(dir0))
            //    {
            //        Directory.CreateDirectory(dir0);
            //    }
            //    //string file = dir0 + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";
            //    string file = $@"{dir0}\{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
            //    bool exists = File.Exists(file);
            //    using (StreamWriter sw = new StreamWriter(file, true))
            //    {
            //        if (!exists)
            //        {
            //            //载具时间（年月日） 载具时间（时分秒） 载具码 穴位号 上视觉结果 Sip码 吸嘴号
            //            sw.WriteLine("载具时间年月日,载具时间时分秒,载具码,穴位号,穴位状态,Sip码,吸嘴号,centreX,centreY,centreA,TapeX,TapeY,TapeA");
            //        }
            //        try
            //        {
            //            for (int idx = 0; idx < 12; idx++)
            //            {
            //                int[] arr = { 0, 3, 6, 9, 1, 4, 7, 10, 2, 5, 8, 11 };
            //                int i = arr[idx];
            //                //时间、载具码、穴位号、吸嘴号、信息
            //                sw.WriteLine(carrierStatus.scanTime.ToString("yyyy/MM/dd") + "," + carrierStatus.scanTime.ToString("HH:mm:ss") + "," +
            //                    carrierStatus.carrierSN + "," + (i + 1) + "," + carrierStatus.cavStateStr[i] + "," +
            //                    carrierStatus.sipSN[i] + "," + carrierStatus.Tape_Nozzle[i] + "," +
            //                    carrierStatus.markToPhotoCenterOffsets[i] + "," + carrierStatus.tapeBaseDistance[i]);
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.Message, En_Logout_Type.Exception, true);
            //        }
            //    }
            //}
            //控制保压，dwellStatus bit0-11 表示 穴位1234...12
            int dwellStatus = 0;
            foreach (var p in CurNeedDwell)
            {
                dwellStatus |= (1 << (p.CavityNum - 1));
            }
            _plc_Component.SetNeedDwell(dwellStatus);
            CurNeedDwell.Clear();

            //移除当前载具
            RemoveCurCarrier();
            //if (ParamManager.MESParam.TapeNames == TapeNames.Alert_Bumper & !_plc_Component.IsSimulateRun())
            //{
            //    _plc_Component.SetWorkResult(0x00002);//通知PLC放行保压相机拍照
            //    if (dwellStatus > 0)
            //    {
            //        AutoResetEvt_Pressurize.Set();
            //    }
            //}
            //else
            //{
            //    _plc_Component.SetWorkResult(0x00001);//通知PLC放行
            //}
            _plc_Component.SetWorkResult(1);//通知PLC放行

            GlobalVariable.CT.SetEndTime();
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, "单板CT：" + GlobalVariable.CT.TotalCT.ToString("F2") + " s", En_Logout_Type.Run, true);
            //重置isHaveDone状态
            ResetIsUpCamFinishedState();

            _plc_Component.SetPcMachineStatus(PcToPlcMachineStatus.工作完成);
            Thread.Sleep(100);
            _plc_Component.SetPcMachineStatus(PcToPlcMachineStatus.空闲中);

            NextStep1 = EN_RunStep.ScannerCarrierSnStep;
            AutoResetEvt_CtrlVisual.Set();
            AutoResetEvt_CtrlScanner.Set();
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"载具放行", En_Logout_Type.Run, true);
            #region 给HIVE发送machine data
            if (_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle)
            {
                DateTime inputTime = carrierStatus.scanTime;
                DateTime outputTime = DateTime.Now;

                for (int i = 0; i < 12; i++)
                {
                    string sipSN = carrierStatus.sipSN[i];
                    if (carrierStatus.errorCode[i] != (int)EN_TrayStatus.OK//只传ok的
                        || carrierStatus.sipSN[i] == ""
                        || carrierStatus.sipSN[i] == "DEFAULT")
                    {
                        continue;
                    }
                    _hive_Component.SendMachineData(sipSN, true, inputTime, outputTime);
                }
            }
            #endregion
            CacheParamManager.SaveHomeUiParam();
            return true;
        }

        /// <summary>
        /// 保存相机定位数据（CSV格式）
        /// </summary>
        /// <param name="carrierStatus"></param>
        /// <param name="CamType"></param>
        /// <param name="cavNum"></param>
        /// <param name="nozzleNum"></param>
        public void SaveToCsv(CarrierStatus carrierStatus, bool CamType, int cavNum, int nozzleNum = 0)
        {
            //上视觉相机NG数据
            if (CamType)
            {
                //保存信息到表格
                string dir0 = $@"D:\QKProject\Data\Other\UPCamNG";
                if (!Directory.Exists(dir0))
                {
                    Directory.CreateDirectory(dir0);
                }
                string file = $@"{dir0}\{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
                bool exists = File.Exists(file);
                //if (_paramManager.MESParam.TapeNames == TapeNames.Flex_GND_Tape)
                //{
                //    using (StreamWriter sw = new StreamWriter(file, true, Encoding.Default))
                //    {
                //        if (!exists)
                //        {
                //            //载具时间（年月日） 载具时间（时分秒） 载具码 穴位号 上视觉结果 Sip码 吸嘴号 markToPhotoCenterOffsets(定位坐标信息) tapeBaseDistance(输出的标签文本) 
                //            sw.WriteLine("载具时间年月日,载具时间时分秒,载具码,穴位号,穴位状态,Sip码,吸嘴号,centreX,centreY,centreA");
                //        }
                //        try
                //        {
                //            //时间、载具码、穴位号、吸嘴号、信息
                //            sw.WriteLine(carrierStatus.scanTime.ToString("yyyy/MM/dd") + "," + carrierStatus.scanTime.ToString("HH:mm:ss") + "," +
                //                carrierStatus.carrierSN + "," + (cavNum + 1) + "," + carrierStatus.cavStateStr[cavNum] + "," +
                //                carrierStatus.sipSN[cavNum] + "," + carrierStatus.Tape_Nozzle[cavNum] + "," +
                //                carrierStatus.markToPhotoCenterOffsets[cavNum]);
                //        }
                //        catch (Exception e)
                //        {
                //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.Message, En_Logout_Type.Exception, true);
                //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception, false);
                //        }
                //    }
                //}
            }
            else//下视觉NG数据
            {
                //保存信息到表格
                string dir0 = $@"D:\QKProject\Data\Other\DownCamNG";
                if (!Directory.Exists(dir0))
                {
                    Directory.CreateDirectory(dir0);
                }
                string file = $@"{dir0}\{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
                bool exists = File.Exists(file);
                //if (_paramManager.MESParam.TapeNames == TapeNames.Flex_GND_Tape)
                //{
                //    using (StreamWriter sw = new StreamWriter(file, true, Encoding.Default))
                //    {
                //        if (!exists)
                //        {
                //            //载具时间（年月日） 载具时间（时分秒） 载具码 穴位号 上视觉结果 Sip码 吸嘴号 贴合点坐标
                //            sw.WriteLine("载具时间年月日,载具时间时分秒,载具码,穴位号,穴位状态,Sip码,吸嘴号,centreX,centreY,centreA");
                //        }
                //        try
                //        {
                //            //时间、载具码、穴位号、吸嘴号、信息
                //            sw.WriteLine(carrierStatus.scanTime.ToString("yyyy/MM/dd") + "," + carrierStatus.scanTime.ToString("HH:mm:ss") + "," +
                //                carrierStatus.carrierSN + "," + (cavNum + 1) + "," + carrierStatus.cavStateStr[cavNum] + "," +
                //                carrierStatus.sipSN[cavNum] + "," + carrierStatus.Flex_GND_Tape_Nozzle[cavNum] + "," +
                //                carrierStatus.tapeBaseDistance[cavNum]);
                //        }
                //        catch (Exception e)
                //        {
                //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.Message, En_Logout_Type.Exception, true);
                //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception, false);
                //        }
                //    }
                //}
                //else
                //{
                //    using (StreamWriter sw = new StreamWriter(file, true, Encoding.Default))
                //    {
                //        if (!exists)
                //        {
                //            //载具时间（年月日） 载具时间（时分秒） 载具码 穴位号 上视觉结果 Sip码 吸嘴号 贴合点坐标
                //            sw.WriteLine("载具时间年月日,载具时间时分秒,载具码,穴位号,穴位状态,Sip码,吸嘴号,centreX,X基准,centreY,Y基准,centreA,Angle基准,NG原因");
                //        }
                //        try
                //        {
                //            //时间、载具码、穴位号、吸嘴号、信息
                //            float[] dataOffsets = new float[3];
                //            string[] strOffsets = carrierStatus.tapeBaseDistance[cavNum].Split(',');
                //            for (int i = 0; i < strOffsets.Length; i++)
                //            {
                //                dataOffsets[i] = float.Parse(strOffsets[i]);
                //            }
                //            CompareDownMark(dataOffsets, out string ngResult, nozzleNum, out string markx, out string marky);
                //            sw.WriteLine(carrierStatus.scanTime.ToString("yyyy/MM/dd") + "," + carrierStatus.scanTime.ToString("HH:mm:ss") + "," +
                //               carrierStatus.carrierSN + "," + (cavNum + 1) + "," + carrierStatus.cavStateStr[cavNum] + "," +
                //               carrierStatus.sipSN[cavNum] + "," + carrierStatus.Tape_Nozzle[cavNum] + "," +
                //               dataOffsets[0] + "," + markx + "," +
                //               dataOffsets[1] + "," + marky + "," +
                //               dataOffsets[2] + "," + ParamManager.CameraParam.DownMarkAngle + "," +
                //               ngResult);
                //        }
                //        catch (Exception e)
                //        {
                //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.Message, En_Logout_Type.Exception, true);
                //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception, false);
                //        }
                //    }
                //}
            }
        }

        /// <summary>
        /// 获取上视觉当前穴位的标准值和NG原因
        /// </summary>
        /// <param name="marktophotocenteroffsets"></param>
        /// <param name="cavnum"></param>
        /// <param name="ngResult"></param>
        /// <param name="markX"></param>
        /// <param name="markY"></param>
        //private void ComPareUPMark(float[] marktophotocenteroffsets, int cavnum, out string ngResult, out string markX, out string markY)
        //{
        //    ngResult = "Null";
        //    markX = "Null";
        //    markY = "Null";
        //    float[] MarkXs = new[]
        //       {
        //               _paramManager.CameraParam.MarkX1,
        //               _paramManager.CameraParam.MarkX2,
        //               _paramManager.CameraParam.MarkX3,
        //               _paramManager.CameraParam.MarkX4,
        //               _paramManager.CameraParam.MarkX5,
        //               _paramManager.CameraParam.MarkX6,
        //               _paramManager.CameraParam.MarkX7,
        //               _paramManager.CameraParam.MarkX8,
        //               _paramManager.CameraParam.MarkX9,
        //               _paramManager.CameraParam.MarkX10,
        //               _paramManager.CameraParam.MarkX11,
        //               _paramManager.CameraParam.MarkX12,
        //            };
        //    float[] MarkposXs = new[]
        //  {
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //              _paramManager.CameraParam.MarkposX,
        //            };
        //    float[] MarkYs = new[]
        // {
        //              _paramManager.CameraParam.MarkY1,
        //              _paramManager.CameraParam.MarkY2,
        //              _paramManager.CameraParam.MarkY3,
        //              _paramManager.CameraParam.MarkY4,
        //              _paramManager.CameraParam.MarkY5,
        //              _paramManager.CameraParam.MarkY6,
        //              _paramManager.CameraParam.MarkY7,
        //              _paramManager.CameraParam.MarkY8,
        //              _paramManager.CameraParam.MarkY9,
        //              _paramManager.CameraParam.MarkY10,
        //              _paramManager.CameraParam.MarkY11,
        //              _paramManager.CameraParam.MarkY12,
        //            };
        //    float[] MarkposYs = new[]
        //  {
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //              _paramManager.CameraParam.MarkposY,
        //            };

        //    if (!(MarkXs[cavnum] + MarkposXs[cavnum] > marktophotocenteroffsets[0] && MarkXs[cavnum] - MarkposXs[cavnum] < marktophotocenteroffsets[0]))
        //    {
        //        ngResult = "X-NG";
        //        markX = MarkXs[cavnum].ToString();
        //        markY = MarkYs[cavnum].ToString();
        //    }
        //    if (!(MarkYs[cavnum] + MarkposYs[cavnum] > marktophotocenteroffsets[1] && MarkYs[cavnum] - MarkposYs[cavnum] < marktophotocenteroffsets[1]))
        //    {
        //        ngResult = "Y-NG";
        //        markX = MarkXs[cavnum].ToString();
        //        markY = MarkYs[cavnum].ToString();
        //    }
        //}

        /// <summary>
        /// 获取下视觉当前穴位的标准值和NG原因
        /// </summary>
        /// <param name="marktophotocenteroffsets"></param>
        /// <param name="ngResult"></param>
        /// <param name="nozzle"></param>
        /// <param name="markX"></param>
        /// <param name="markY"></param>
        private void CompareDownMark(float[] marktophotocenteroffsets, out string ngResult, int nozzle, out string markX, out string markY)
        {
            ngResult = "Null";
            float[] MarkXS = new[]
                    {
                       _paramManager.CameraParam.DownMark_X1,
                       _paramManager.CameraParam.DownMark_X2,
                    };
            float[] MarkYS = new[]
                    {
                       _paramManager.CameraParam.DownMark_Y1,
                       _paramManager.CameraParam.DownMark_Y2,
                    };
            float MarkposXS = _paramManager.CameraParam.DownMarkpos_X;
            float MarkposYS = _paramManager.CameraParam.DownMarkpos_Y;
            float MarkAngle = _paramManager.CameraParam.DownMarkAngle;
            float MarkposAngle = _paramManager.CameraParam.DownMarkAnglePos;

            if (!(MarkXS[nozzle] + MarkposXS > marktophotocenteroffsets[0] && MarkXS[nozzle] - MarkposXS < marktophotocenteroffsets[0]))
            {
                ngResult = "X-NG";
            }
            if (!(MarkYS[nozzle] + MarkposYS > marktophotocenteroffsets[1] && MarkYS[nozzle] - MarkposYS < marktophotocenteroffsets[1]))
            {
                ngResult = "Y-NG";
            }
            if (!(MarkAngle + MarkposAngle > marktophotocenteroffsets[2] && MarkAngle - MarkposAngle < marktophotocenteroffsets[2]))
            {
                ngResult = "Angle-NG";
            }
            markX = MarkXS[nozzle].ToString();
            markY = MarkYS[nozzle].ToString();
        }

        /// <summary>
        /// 获取当前吸嘴Feed取料手动补偿值
        /// </summary>
        /// <param name="nozzel"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public float[] FeederSingleOffset(int nozzel, int index)
        {
            nozzel -= 1;
            if (_paramManager.CameraParam.IsFeedRepairBus)
            {
                if (CacheParamManager.HomeUiParam.Enable.FeederId == FeederId.左飞达)
                {
                    if (index == 0)
                    {
                        float[] LeftXPos = new float[]
                              {
                    _paramManager.CameraParam.FeedRepair_LX1,
                    _paramManager.CameraParam.FeedRepair_LX2,
                             };
                        float[] LeftYPos = new float[]
                             {
                    _paramManager.CameraParam.FeedRepair_LY1,
                    _paramManager.CameraParam.FeedRepair_LY2,
                             };
                        float[] LeftAPos = new float[]
                              {
                    _paramManager.CameraParam.FeedRepair_LA1,
                    _paramManager.CameraParam.FeedRepair_LA2,
                             };

                        return new float[] { LeftXPos[nozzel], LeftYPos[nozzel], LeftAPos[nozzel] };
                    }
                    else
                    {
                        float[] RightXPos = new float[]
                              {
                    _paramManager.CameraParam.FeedRepair_RX1,
                    _paramManager.CameraParam.FeedRepair_RX2,
                             };
                        float[] RightYPos = new float[]
                             {
                    _paramManager.CameraParam.FeedRepair_RY1,
                    _paramManager.CameraParam.FeedRepair_RY2,
                             };
                        float[] RightAPos = new float[]
                              {
                    _paramManager.CameraParam.FeedRepair_RA1,
                    _paramManager.CameraParam.FeedRepair_RA2,
                             };

                        return new float[] { RightXPos[nozzel], RightYPos[nozzel], RightAPos[nozzel] };
                    }
                }
                else
                {
                    if (index == 0)
                    {
                        float[] LeftXPos = new float[]
                              {
                    _paramManager.CameraParam.Feed2Repair_LX1,
                    _paramManager.CameraParam.Feed2Repair_LX2,
                             };
                        float[] LeftYPos = new float[]
                             {
                    _paramManager.CameraParam.Feed2Repair_LY1,
                    _paramManager.CameraParam.Feed2Repair_LY2,
                             };
                        float[] LeftAPos = new float[]
                              {
                    _paramManager.CameraParam.Feed2Repair_LA1,
                    _paramManager.CameraParam.Feed2Repair_LA2,
                             };

                        return new float[] { LeftXPos[nozzel], LeftYPos[nozzel], LeftAPos[nozzel] };
                    }
                    else
                    {
                        float[] RightXPos = new float[]
                              {
                    _paramManager.CameraParam.Feed2Repair_RX1,
                    _paramManager.CameraParam.Feed2Repair_RX2,
                             };
                        float[] RightYPos = new float[]
                             {
                    _paramManager.CameraParam.Feed2Repair_RY1,
                    _paramManager.CameraParam.Feed2Repair_RY2,
                             };
                        float[] RightAPos = new float[]
                              {
                    _paramManager.CameraParam.Feed2Repair_RA1,
                    _paramManager.CameraParam.Feed2Repair_RA2,
                             };

                        return new float[] { RightXPos[nozzel], RightYPos[nozzel], RightAPos[nozzel] };
                    }
                }
            }
            else
            {
                return new float[] { 0, 0, 0 };
            }
        }
    }

}

public class CarrierStatus
{
    #region FA11-004
    /// <summary>
    /// 载具SN
    /// </summary>
    public string carrierSN = "";
    /// <summary>
    /// 扫码时间
    /// </summary>
    public DateTime scanTime = DateTime.Now;
    /// <summary>
    /// 错误码，参考TrayStatus
    /// </summary>
    public int[] errorCode = new int[12];
    /// <summary>
    /// sip码
    /// </summary>
    public string[] sipSN = new string[12];

    /// <summary>
    /// 贴装吸嘴，01~02
    /// </summary>
    public int[] Tape_Nozzle = new int[12] { 1, 1, 1, 2, 2, 2, 1, 1, 1, 2, 2, 2 };
    /// <summary>
    /// 保压头，01~04
    /// </summary>
    public int[] Tape_Indenter = new int[12] { 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4 };
    /// <summary>
    /// 贴装压力，单位kg，保留两位小数，0.40-0.60
    /// </summary>
    public float[] Tape_PastePress = new float[12];
    /// <summary>
    /// Tape SN
    /// </summary>
    public string[] Tape_SN = new string[12];
    /// <summary>
    /// Tape 是否抛料
    /// </summary>
    public int[] Tape_Isthrow = new int[12];
    /// <summary>
    /// Tape 抛料次数
    /// </summary>
    public int[] Tape_ThrowCount = new int[12];
    /// <summary>
    /// Tape 抛料类型
    /// </summary>
    public int[] Tape_ThrowReason = new int[12];
    /// <summary>
    /// 上视觉结果
    /// </summary>

    public string[] cavStateStr = new string[12]; //上视觉结果
    /// <summary>
    /// Tape下视觉定位数据
    /// </summary>

    public string[] tapeBaseDistance = new string[12];

    /// <summary>
    /// 上视觉定位数据
    /// </summary>
    public string[] markToPhotoCenterOffsets = new string[12];

    #endregion


    private static Random random = new Random();

    //暂存拍照SN以供后续图片分组使用，必须在下面初始化为""，不能是null
    public string[] TLMOkSns = new string[12];//Feeder拍照这里只有ok的
    public string[] TLNOkSns = new string[12];//下视觉这里只有ok的
    public string[] TLTOkSns = new string[12];//上视觉定位OK
    public string[] TLTNgSns = new string[12];//上视觉定位NG
    public string[] TFCOkSns = new string[12];//保压相机定位OK
    public string[] TFCNgSns = new string[12];//保压相机定位NG
    public string[] TFC1Sns = new string[12];
    public int[] isEmpty = new int[12];//判断是否为空穴
    public bool[] TFC1Results = new bool[12];
    public List<string>[] TFC2Sns = new List<string>[12];

    public EN_Routing_Result[] routingResults = new EN_Routing_Result[12];

    public CarrierStatus()
    {
        for (int i = 0; i < 12; i++)
        {
            sipSN[i] = "";
            Tape_PastePress[i] = (float)(random.NextDouble() * 0.1 + 0.45);
            Tape_SN[i] = "";

            Tape_Isthrow[i] = 0;
            Tape_ThrowCount[i] = 0;
            Tape_ThrowReason[i] = 0;

            cavStateStr[i] = "";
            markToPhotoCenterOffsets[i] = "0,0";
            tapeBaseDistance[i] = "0";

            isEmpty[i] = 0;
        }
    }
}
