/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-24
 * 说明：（固高运动控制）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using CS_QMotion;
using QA.Business.CacheParam;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Model.Alarm;
using QA.Business.Station;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Component.Motion.Googol
{
    public enum En_SpeedType
    {
        Low,
        Mid,
        High,
        Work
    }

    public enum En_RobotWorkCtrl
    {
        Pause = 0x01,
        Resume = 0x02,
        Stop = 0x03,
        SlowPause = 0x28,
    }

    public enum En_AxisNum
    {
        X1 = 0,
        Y1,
        X2,
        Y2,
        Z2,
        R1,
        R2,
        //R3,
        //R4,
    }

    public enum En_AxisStatus
    {
        Axis_Alarm = 0x02, //伺服报警
        Axis_MoveErr = 0x10, //运动出错
        Axis_PosLimit = 0x20, //触发正限位
        Axis_NegLimit = 0x40, //触发负限位
        Axis_EStop = 0x100, //急停报警
        Axis_Power = 0x200, //电机使能
    }

    public enum En_StationNo
    {
        StationNo1,
        StationNo2,
        StationNo3,
    }

    public enum En_GetAxisClrSts
    {
        //Bit0 保留
        Bit0_Reserved = 0x01,

        //Bit1 驱动器报警标志 控制轴连接的驱动器报警时置 1
        Bit1_DriverAlarm = 0x02,

        //Bit2 保留
        Bit2_Reserved = 0x04,

        //Bit3 保留
        Bit3_Reserved = 0x08,

        //Bit4 跟随误差越限标志 控制轴规划位置和实际位置的误差大于设定极限时置 1 
        Bit4_FollowOverLimited = 0x10,

        //Bit5 正限位触发标志 正限位开关电平状态为限位触发电平时置 1规划位置大于正向软限位时置 1
        Bit5_PositiveLimitTriggered = 0x20,

        //Bit6 负限位触发标志 负限位开关电平状态为限位触发电平时置 1规划位置小于负向软限位时置 1
        Bit6_NegtiveLimitTriggered = 0x40,

        //Bit7 IO 平滑停止触发标志 如果轴设置了平滑停止 IO，当其输入为触发电平时置 1，并自动平滑停止该轴
        Bit7_IOSmoothStopTriggered = 0x80,

        //Bit8 IO 急停触发标志 如果轴设置了急停 IO，当其输入为触发电平时置 1，并自动急停该轴
        Bit8_EmergencyStopTriggered = 0x100,

        //Bit9 电机使能标志 电机使能时置 1
        Bit9_MotorEnabled = 0x200,

        //Bit10 规划运动标志 规划器运动时置 1
        Bit10_MoveEnabled = 0x400,

        //Bit11 电机到位标志 规划器静止，规划位置和实际位置的误差小于设定误差带，并且在误差带内保持设定时间后，置起到位标志
        Bit11_MotorInPlaceReady = 0x800,
    }

    public class MotionGoogol_Component : IMGoogol, IMotion
    {
        #region Field
        private readonly int _axisNum = 7; //该机台总共使用的轴数为9个轴
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private CacheParamManager _cacheParamManager;
        private QMotion qMotion = new QMotion();
        private MotionGoogolParam _mGoogolParam;
        private int[] _ctrlSts = new int[7]; //每个轴状态
        private float[] axisfloats = new float[5];
        private object obj = new object();
        short olderrcode = 0;
        #endregion

        #region property

        public HomeUiParam_Enable Enable { get => _cacheParamManager.HomeUiParam.Enable; }
        public IParam Param { get; set; } = null;
        public string ComponentName { get; set; } = "MotionGoogol";
        public bool IsConnected { get; set; }
        public bool IsAutoStroke { get; set; } = true;

        public bool IsResetCompleted { get; set; } = false;

        private float[] Pulseequ = new float[8];
        public float[] CurPos { get; internal set; } = new float[9];
        public ushort EInput0 { get; set; }
        public ushort EInput1 { get; set; }
        public ushort EOutput0 { get; set; }
        public ushort EOutput1 { get; set; }
        public int[] AxisSts { get; set; } = new int[SysConfig.AxisCount];
        public short[] AInput { get; set; } = new short[6]; //模拟输入

        #endregion

        #region Event

        public event Action<RobotRealStateInfo> RobotValueRefresh;

        #endregion

        #region Constructor

        public MotionGoogol_Component()
        {
            Param = new MotionGoogolParam(); //固高运动板卡的参数放在组件类中
            _mGoogolParam = IoC.Get<MotionGoogolParam>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }

        #endregion

        #region Method

        public bool Initial(IParam param)
        {
            try
            {
                Param = _mGoogolParam = (param as MotionGoogolParam).DeepCopy();

                //Pulseequ = new float[10] {
                //    _mGoogolParam.PluseEquivalentX1,
                //    _mGoogolParam.PluseEquivalentY1,
                //    _mGoogolParam.PluseEquivalentX2,
                //    _mGoogolParam.PluseEquivalentY2,
                //    _mGoogolParam.PluseEquivalentZ2,
                //    _mGoogolParam.PluseEquivalentR1,
                //    _mGoogolParam.PluseEquivalentR2,
                //    _mGoogolParam.PluseEquivalentR3,
                //    _mGoogolParam.PluseEquivalentR4,
                //};

                Pulseequ = new float[8]
                {
                    1280,
                    1280,
                    1280,
                    1280,
                    2560,
                    138.89f,
                    138.89f,
                    138.89f,
                    //138.89f,
                    //138.89f
                };
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e + e.StackTrace);
                return false;
            }
            return true;
        }

        public bool SetSoftLimit()
        {
            if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.X1, true, 1, (_mGoogolParam.X1Max + 1) * Pulseequ[(byte)En_AxisNum.X1], -100f))
                return false;
            if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.Y1, true, 1, (_mGoogolParam.Y1Max + 1) * Pulseequ[(byte)En_AxisNum.Y1], -100f))
                return false;
            if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.X2, true, 1, (_mGoogolParam.X2Max + 1) * Pulseequ[(byte)En_AxisNum.X2], -100f))
                return false;
            if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.Y2, true, 1, (_mGoogolParam.Y2Max + 1) * Pulseequ[(byte)En_AxisNum.Y2], -100f))
                return false;
            if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.Z2, true, 1, (_mGoogolParam.Z2Max + 1) * Pulseequ[(byte)En_AxisNum.Z2], -100f))
                return false;
            if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.R1, true, 1, 250 * Pulseequ[(byte)En_AxisNum.R1],
                    -250f * Pulseequ[(byte)En_AxisNum.R1]))
                return false;
            if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.R2, true, 1, 250 * Pulseequ[(byte)En_AxisNum.R2],
                    -250f * Pulseequ[(byte)En_AxisNum.R2]))
                return false;
            //if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.R3, true, 1, 250 * Pulseequ[(byte)En_AxisNum.R3],
            //        -250f * Pulseequ[(byte)En_AxisNum.R3]))
            //    return false;
            //if (!qMotion.AxisSetSoftLimit((byte)En_AxisNum.R4, true, 1, 250 * Pulseequ[(byte)En_AxisNum.R4],
            //        -250f * Pulseequ[(byte)En_AxisNum.R4]))
            //    return false;
            return true;
        }

        /// <summary>
        /// 控制器尝试获取使能/解除使能
        /// </summary>
        /// <param name="isOpen"></param>
        /// <param name="axisId"></param>
        /// <returns></returns>
        public bool SetServeOnOff(bool isOpen, short axisId = -1)
        {
            //axisId = -1 默认所有轴开关伺服
            //axisId = 0,代表X1轴关闭，后面依次类推
            return qMotion.SetAxisOnOff(isOpen, axisId);
        }

        public bool Connect()
        {
            short errcode = 0;
            short getAxisNum = 0;
            //传入轴的数目、脉冲当量、输出出错代码
            bool ret = qMotion.Init(_axisNum, Pulseequ, ref getAxisNum, ref errcode);
            if (errcode != olderrcode)
            {
                olderrcode = errcode;
                if (errcode == 1)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "打开运动控制器失败", En_Logout_Type.Run, true);
                    return false;
                }
                else if (errcode == 2)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "EtherCAT初始化，通讯未完全建立，启动总线通讯失败", En_Logout_Type.Run, true);
                    return false;
                }
                else if (errcode == 3)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "复位运动控制器失败", En_Logout_Type.Run, true);
                    return false;
                }
                else if (errcode == 4)
                {
                    //下载配置信息到运动控制器，调用该指令后需再调用 GTN_ClrSts才能使该指令生效
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "下载配置信息到运动控制器失败", En_Logout_Type.Run, true);
                    return false;
                }
                else if (errcode == 5)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "读取EtherCAT总线的在线从站数目失败或数目不对", En_Logout_Type.Run, true);
                    return false;
                }
                else if (errcode == 6)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"读取EtherCAT总线的在线从站数目不对,当前数目：{getAxisNum.ToString()}",
                        En_Logout_Type.Run, true);
                    return false;
                }
                else if (errcode == 7)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "清除控制器报警失败", En_Logout_Type.Run, true);
                    return false;
                }
                else if (errcode == 8)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "扩展模块初始化失败", En_Logout_Type.Run, true);
                    return false;
                }
            }
            else if (errcode > 0 && errcode < 9)
            {
                return false;
            }
            if (!SetServeOnOff(true))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "控制器获取伺服使能失败", En_Logout_Type.Run, true);
                return false;
            }

            DateTime starttime = DateTime.Now;

            while (true)
            {
                if ((DateTime.Now - starttime).TotalSeconds > 3)
                {
                    break;
                }
                Thread.Sleep(200);
                if (GetServoEnabledStatus())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, "控制器已获取伺服使能", En_Logout_Type.Run, true);
                    return true;
                }
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "控制器获取伺服使能失败", En_Logout_Type.Run, true);
            return false;
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                int _EInput0 = 0;
                int _EInput1 = 0;
                int _EOutput0 = 0;
                int _EOutput1 = 0;
                short[] ain = new short[6];
                int[] sts0 = new int[7];
                //int[] sts1 = new int[1];
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
                            IsConnected = Connect();
                        }
                        _EInput0 = 0;
                        _EInput1 = 0;
                        _EOutput0 = 0;
                        _EOutput1 = 0;
                        ain = new short[6];
                        if (_mGoogolParam.IsCylinderDoubleSwitch)
                        {
                            //0 1都是io模块，2是读压力的模块
                            if (!GetCurPos()
                            || !qMotion.GetDIn(0, ref _EInput0)
                            || !qMotion.GetDOut(0, ref _EOutput0)
                            || !qMotion.GetDIn(1, ref _EInput1)
                            || !qMotion.GetDOut(1, ref _EOutput1))
                            {
                                IsConnected = false;
                                continue;
                            }
                            EInput0 = (ushort)_EInput0;
                            EInput1 = (ushort)_EInput1;
                            EOutput0 = (ushort)_EOutput0;
                            EOutput1 = (ushort)_EOutput1;

                            qMotion.GetAIn(2, 0, 6, ref ain);
                            AInput = ain;
                        }
                        else
                        {
                            //0 是io模块，1是读压力的模块
                            if (!GetCurPos()
                            || !qMotion.GetDIn(0, ref _EInput0)
                            || !qMotion.GetDOut(0, ref _EOutput0))
                            {
                                IsConnected = false;
                                continue;
                            }
                            EInput0 = (ushort)_EInput0;
                            EOutput0 = (ushort)_EOutput0;

                            qMotion.GetAIn(1, 0, 6, ref ain);
                            AInput = ain;
                        }
                        if (!qMotion.AxisGetSts(0, ref sts0, 7) /*|| !qMotion.AxisGetSts(8, ref sts1, 1)*/)
                        {
                            IsConnected = false;
                            continue;
                        }
                        //sts0[8] = sts1[0];
                        _ctrlSts = sts0;
                        MoveStroke();
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
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
            }
            return true;
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (Param.BUse)
            {
                if (!IsConnected)
                {
                    var count0 = alarmInfos.Count;
                    alarmInfos.Add(new AlarmInfoModel()
                    {
                        AlarmLevel = EN_WARN_LEVEL.Error,
                        AlarmModule = EN_WarnModules.Robot,
                        AlarmMsg = "无连接",
                        Datetime = DateTime.Now,
                        ErrorCode = 0,
                        Index = count0
                    });
                }

                else
                {
                    for (int i = 0; i < _ctrlSts.Length; i++)
                    {
                        for (int j = 0; j < 12; j++)
                        {
                            if ((_ctrlSts[i] & 0x01 << j) > 0x00)
                            {
                                string axisStr = ((En_AxisNum)i).ToString();
                                if (0x01 << j == (int)En_GetAxisClrSts.Bit1_DriverAlarm)
                                {
                                    var count = alarmInfos.Count;
                                    alarmInfos.Add(new AlarmInfoModel()
                                    {
                                        AlarmLevel = EN_WARN_LEVEL.Error,
                                        AlarmModule = EN_WarnModules.Robot,
                                        AlarmMsg = $"{axisStr}轴驱动器报警",
                                        Datetime = DateTime.Now,
                                        ErrorCode = 0,
                                        Index = count
                                    });
                                }
                                else if (0x01 << j == (int)En_GetAxisClrSts.Bit4_FollowOverLimited)
                                {
                                    //跟随误差越限标记
                                    var count = alarmInfos.Count;
                                    alarmInfos.Add(new AlarmInfoModel()
                                    {
                                        AlarmLevel = EN_WARN_LEVEL.Error,
                                        AlarmModule = EN_WarnModules.Robot,
                                        AlarmMsg = $"{axisStr}轴跟随误差越限",
                                        Datetime = DateTime.Now,
                                        ErrorCode = 0,
                                        Index = count
                                    });
                                }
                                else if (0x01 << j == (int)En_GetAxisClrSts.Bit5_PositiveLimitTriggered)
                                {
                                    //正限位触发标志
                                    var count = alarmInfos.Count;
                                    alarmInfos.Add(new AlarmInfoModel()
                                    {
                                        AlarmLevel = EN_WARN_LEVEL.Error,
                                        AlarmModule = EN_WarnModules.Robot,
                                        AlarmMsg = $"{axisStr}轴正限位触发",
                                        Datetime = DateTime.Now,
                                        ErrorCode = 0,
                                        Index = count
                                    });
                                }
                                else if (0x01 << j == (int)En_GetAxisClrSts.Bit6_NegtiveLimitTriggered)
                                {
                                    //负限位触发标志
                                    var count = alarmInfos.Count;
                                    alarmInfos.Add(new AlarmInfoModel()
                                    {
                                        AlarmLevel = EN_WARN_LEVEL.Error,
                                        AlarmModule = EN_WarnModules.Robot,
                                        AlarmMsg = $"{axisStr}轴负限位触发",
                                        Datetime = DateTime.Now,
                                        ErrorCode = 0,
                                        Index = count
                                    });
                                }
                                else if (0x01 << j == (int)En_GetAxisClrSts.Bit7_IOSmoothStopTriggered)
                                {
                                    //IO 平滑停止触发标志
                                    var count = alarmInfos.Count;
                                    alarmInfos.Add(new AlarmInfoModel()
                                    {
                                        AlarmLevel = EN_WARN_LEVEL.Error,
                                        AlarmModule = EN_WarnModules.Robot,
                                        AlarmMsg = $"{axisStr}轴IO 平滑停止触发",
                                        Datetime = DateTime.Now,
                                        ErrorCode = 0,
                                        Index = count
                                    });
                                }
                                else if (0x01 << j == (int)En_GetAxisClrSts.Bit8_EmergencyStopTriggered)
                                {
                                    //急停触发标志
                                    var count = alarmInfos.Count;
                                    alarmInfos.Add(new AlarmInfoModel()
                                    {
                                        AlarmLevel = EN_WARN_LEVEL.Error,
                                        AlarmModule = EN_WarnModules.Robot,
                                        AlarmMsg = $"{axisStr}轴急停触发",
                                        Datetime = DateTime.Now,
                                        ErrorCode = 0,
                                        Index = count
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region 获取轴状态

        public bool GetServoEnabledStatus()
        {
            int[] sts0 = new int[7];
            //int[] sts1 = new int[1];
            if (!qMotion.AxisGetSts(0, ref sts0, 7))
                return false;
            //if (!qMotion.AxisGetSts(8, ref sts1, 1))
            //    return false;

            //sts0[8] = sts1[0];
            for (int i = 0; i < sts0.Length; i++)
            {
                if ((sts0[i] & (int)En_GetAxisClrSts.Bit9_MotorEnabled) <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        //Bit10_MoveEnabled ：注意false 说明在运动 ，true代表静止
        public bool GetAxisClrSts(En_GetAxisClrSts clrSts)
        {
            int[] sts0 = new int[7];
            //int[] sts1 = new int[1];
            if (!qMotion.AxisGetSts(0, ref sts0, 7))
                return false;
            //if (!qMotion.AxisGetSts(8, ref sts1, 1))
            //    return false;

            //sts0[8] = sts1[0];
            for (int i = 0; i < sts0.Length; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if ((sts0[i] & 0x01 << j) > 0x00)
                    {
                        if (0x01 << j == (int)clrSts)
                        {
                            //报警
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool ExistAlarmAxisClrSts()
        {
            int[] sts0 = new int[7];
            //int[] sts1 = new int[1];
            if (!qMotion.AxisGetSts(0, ref sts0, 7))
                return false;
            //if (!qMotion.AxisGetSts(8, ref sts1, 1))
            //    return false;

            //sts0[8] = sts1[0];

            for (int i = 0; i < sts0.Length; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if ((sts0[i] & 0x01 << j) > 0x00)
                    {
                        if (0x01 << j == (int)En_GetAxisClrSts.Bit1_DriverAlarm)
                        {
                            //驱动器报警
                            return false;
                        }
                        else if (0x01 << j == (int)En_GetAxisClrSts.Bit4_FollowOverLimited)
                        {
                            //跟随误差越限标志
                            return false;
                        }
                        else if (0x01 << j == (int)En_GetAxisClrSts.Bit5_PositiveLimitTriggered)
                        {
                            //正限位触发标志
                            return false;
                        }
                        else if (0x01 << j == (int)En_GetAxisClrSts.Bit6_NegtiveLimitTriggered)
                        {
                            //负限位触发标志
                            return false;
                        }
                        else if (0x01 << j == (int)En_GetAxisClrSts.Bit7_IOSmoothStopTriggered)
                        {
                            //IO 平滑停止触发标志
                            return false;
                        }
                        else if (0x01 << j == (int)En_GetAxisClrSts.Bit8_EmergencyStopTriggered)
                        {
                            //急停触发标志
                            return false;
                        }
                        else
                            return true;
                    }
                    else
                        return true;
                }
            }
            return true;
        }

        /// <summary>
        /// 轴速度转换成脉冲数
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private float[] ConvertToPluse(En_AxisNum axis, float[] values)
        {
            float[] pluses = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                pluses[i] = values[i] * _mGoogolParam.PluseEquivalents[(int)axis];
            }
            return pluses;
        }

        #endregion

        /// <summary>
        /// 轴坐标转换成脉冲数
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private float ConvertToPluse(En_AxisNum axis, float value)
        {
            return value * _mGoogolParam.PluseEquivalents[(int)axis];
        }

        #region 碰撞保护

        /// <summary>
        /// 返回吸嘴轴系的Y是否在安全位置
        /// </summary>
        /// <returns></returns>
        public bool Station2InSafeArea()
        {
            if (!GetCurPos())
            {
                return false;
            }
            //120 核心参数，不能乱改，后果自负
            return CurPos[(byte)En_AxisNum.Y2] < _mGoogolParam.SafeY2;
        }

        /// <summary>
        /// 将吸嘴轴Y移动到安全位置
        /// </summary>
        /// <returns></returns>
        public bool MoveY2ToSafePos(bool moveTo0 = true)
        {
            if (!MoveAbsoluteSingleAxis(En_AxisNum.Y2, moveTo0 ? 0 : _mGoogolParam.SafeY2, true) || Exit())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 当吸嘴轴去贴合时，进行相机轴的避让操作
        /// </summary>
        /// <param name="targetX2">吸嘴轴目标X</param>
        /// <param name="targetX2">是否需要移动Y2。如果两个轴分别由两个step控制，应传入false，否则传入true</param>
        /// <returns></returns>
        public bool SafeAvoid(bool waitX1Y1ForEnd = true)
        {
            //if (targetX2 < _mGoogolParam.SafeX2 && CurPos[(byte)En_AxisNum.X1] > 1)
            //{
            //    //需要将X1Y1运动到（0，0）
            //    if (moveStation2 && CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.SafeY2)
            //    {
            //        if (!MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || Exit())
            //        {
            //            return false;
            //        }
            //        if (!MoveAbsoluteSingleAxis(En_AxisNum.Y2, _mGoogolParam.SafeY2, true) || Exit())
            //        {
            //            return false;
            //        }
            //    }
            //    if (!MoveAbsoluteX1Y1(new float[] { 0, 0 }, waitX1Y1ForEnd, true) || Exit())
            //    {
            //        return false;
            //    }
            //}
            //else if (targetX2 >= _mGoogolParam.SafeX2 && CurPos[(byte)En_AxisNum.X1] < _mGoogolParam.X1Max - 1)
            //{
            //    //需要将X1Y1运动到（_mGoogolParam.X1Max，0）位置
            //    if (moveStation2 && CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.SafeY2)
            //    {
            //        if (!MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || Exit())
            //        {
            //            return false;
            //        }
            //        if (!MoveAbsoluteSingleAxis(En_AxisNum.Y2, _mGoogolParam.SafeY2, true) || Exit())
            //        {
            //            return false;
            //        }
            //    }
            //    if (!MoveAbsoluteX1Y1(new float[] { _mGoogolParam.X1Max, 0 }, waitX1Y1ForEnd, true) || Exit())
            //    {
            //        return false;
            //    }
            //}
            //需要将X1Y1运动到（0，0）
            //if (moveStation2 && CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.SafeY2)
            //{
            //    if (!MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || Exit())
            //    {
            //        return false;
            //    }
            //    if (!MoveAbsoluteSingleAxis(En_AxisNum.Y2, _mGoogolParam.SafeY2, true) || Exit())
            //    {
            //        return false;
            //    }
            //}
            if (!MoveAbsoluteX1Y1(new float[] { 0, 0 }, waitX1Y1ForEnd, true) || Exit())
            {
                return false;
            }
            return true;
        }

        #endregion

        #region 报警处理

        public bool CleanAlarm()
        {
            return qMotion.CleanAlarm();
        }

        #endregion

        #region StopAxis 停止轴系运动

        public bool StopAxisMove(En_AxisNum axisId)
        {
            bool ret = false;
            qMotion.Exit(true);
            ret = qMotion.StopAxisMove((byte)axisId);
            qMotion.Exit(false);
            return ret;
        }

        public bool StopAllAxisMove()
        {
            qMotion.Exit(true);
            if (!qMotion.StopMultiAxisMove(new float[7] { 0, 1, 2, 3, 4, 5, 6 }))
            {
                qMotion.Exit(false);
                return false;
            }
            qMotion.Exit(false);
            return true;
        }

        public void ExitAxisMove(bool isExit = true)
        {
            qMotion.Exit(isExit);
        }

        public bool Exit()
        {
            if (!qMotion.GetExitStatus())
            {
                return false;
            }
            DateTime time = DateTime.Now;
            while ((DateTime.Now - time).TotalMilliseconds < 1000)
            {
                if (!qMotion.GetExitStatus())
                {
                    return false;
                }
                Thread.Sleep(50);
            }
            return true;
        }

        public void SetExitSts(bool sts)
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"SetExitSts:{sts}", En_Logout_Type.Run, true);
            qMotion.Exit(sts);
        }

        #endregion

        #region Reset

        public bool SetResetSpeed(En_StationNo station)
        {
            try
            {
                bool rs = true;
                if (station == En_StationNo.StationNo1)
                {
                    rs &= qMotion.SetZeroSpeed((short)En_AxisNum.X1, ConvertToPluse(En_AxisNum.X1, _mGoogolParam.ResetSpeedX1));
                    rs &= qMotion.SetZeroSpeed((short)En_AxisNum.Y1, ConvertToPluse(En_AxisNum.Y1, _mGoogolParam.ResetSpeedY1));

                }
                else if (station == En_StationNo.StationNo2)
                {
                    rs &= qMotion.SetZeroSpeed((short)En_AxisNum.X2, ConvertToPluse(En_AxisNum.X2, _mGoogolParam.ResetSpeedX2));
                    rs &= qMotion.SetZeroSpeed((short)En_AxisNum.Y2, ConvertToPluse(En_AxisNum.Y2, _mGoogolParam.ResetSpeedY2));
                    rs &= qMotion.SetZeroSpeed((short)En_AxisNum.Z2, ConvertToPluse(En_AxisNum.Z2, _mGoogolParam.ResetSpeedZ2));
                    rs &= qMotion.SetZeroSpeed((short)En_AxisNum.R1, ConvertToPluse(En_AxisNum.R1, _mGoogolParam.ResetSpeedR1));
                    rs &= qMotion.SetZeroSpeed((short)En_AxisNum.R2, ConvertToPluse(En_AxisNum.R2, _mGoogolParam.ResetSpeedR2));
                    //rs &= qMotion.SetZeroSpeed((short)En_AxisNum.R3, ConvertToPluse(En_AxisNum.R3, _mGoogolParam.ResetSpeedR3));
                    //rs &= qMotion.SetZeroSpeed((short)En_AxisNum.R4, ConvertToPluse(En_AxisNum.R4, _mGoogolParam.ResetSpeedR4));
                }
                return rs;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            return false;
        }

        public bool ResetAxis(En_StationNo stationNo)
        {
            if (stationNo == En_StationNo.StationNo1)
            {
                //复位顺序必须是Y->X
                qMotion.ResetAxis((byte)En_AxisNum.Y1, false, false);

                if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.Y1))
                {
                    return false;
                }

                qMotion.ResetAxis((byte)En_AxisNum.X1, false, false);
                if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.X1))
                {
                    return false;
                }
            }
            else if (stationNo == En_StationNo.StationNo2)
            {
                //复位顺序必须是Z->Y->X
                qMotion.ResetAxis((byte)En_AxisNum.Z2, false, false);
                if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.Z2))
                {
                    return false;
                }

                qMotion.ResetAxis((byte)En_AxisNum.Y2, false, false);
                if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.Y2))
                {
                    return false;
                }

                qMotion.ResetAxis((byte)En_AxisNum.X2, false, false);
                if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.X2))
                {
                    return false;
                }

                qMotion.ResetAxis((byte)En_AxisNum.R1, false, false);
                qMotion.ResetAxis((byte)En_AxisNum.R2, false, false);
                //qMotion.ResetAxis((byte)En_AxisNum.R3, false, false);
                //qMotion.ResetAxis((byte)En_AxisNum.R4, false, false);

                //if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.R4))
                //{
                //    return false;
                //}
                //if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.R3))
                //{
                //    return false;
                //}
                if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.R2))
                {
                    return false;
                }
                if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.R1))
                {
                    return false;
                }
            }
            return true;
        }

        public bool ResetAllAxis()
        {
            IsResetCompleted = false;
            qMotion.CleanAlarm();

            if (!WaitAxisMoveEnd() || Exit())
                return false;

            SetResetSpeed(En_StationNo.StationNo1);
            SetResetSpeed(En_StationNo.StationNo2);
            //注意这里复位是有轴顺序的，不能乱调整
            //复位顺序必须是Z->Y->X
            qMotion.ResetAxis((byte)En_AxisNum.Z2, false, false);
            if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.Z2))
            {
                return false;
            }
            qMotion.ResetAxis((byte)En_AxisNum.R1, false, false);
            qMotion.ResetAxis((byte)En_AxisNum.R2, false, false);
            qMotion.ResetAxis((byte)En_AxisNum.Y1, false, false);
            qMotion.ResetAxis((byte)En_AxisNum.Y2, false, false);
            qMotion.ResetAxis((byte)En_AxisNum.X1, false, false);
            qMotion.ResetAxis((byte)En_AxisNum.X2, false, false);
            if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.Y1))
            {
                return false;
            }
            if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.Y2))
            {
                return false;
            }
            if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.X1))
            {
                return false;
            }
            if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.X2))
            {
                return false;
            }
            if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.R2))
            {
                return false;
            }
            if (!qMotion.AxisWaitResetZero((byte)En_AxisNum.R1))
            {
                return false;
            }
            IsResetCompleted = true;
            return true;
        }

        public bool ResetAxis(En_AxisNum axisId, bool waitforend = true, bool buseflag = true)
        {
            return qMotion.ResetAxis((short)axisId, waitforend, buseflag);
        }

        #endregion

        #region 输入输出相关

        #region 读输入/写输出操作

        /// <summary>
        /// 读一个输入口的状态
        /// </summary>
        /// <param name="input"></param>
        /// <returns>开启返回true，否则返回false</returns>
        public bool ReadInput(EN_GoogolExtendInput input)
        {
            if (!IsConnected)
            {
                return false;
            }
            int id = (byte)input / 16; //模块索引
            int index = (byte)input % 16; //在模块内的索引
            if (id == 0)
            {
                return (EInput0 & (1 << index)) > 0;
            }
            else if (id == 1)
            {
                return (EInput1 & (1 << index)) > 0;
            }
            return false;
        }

        /// <summary>
        /// 读一个输出口的状态
        /// </summary>
        /// <param name="output"></param>
        /// <returns>开启返回true，否则返回false</returns>
        public bool ReadOutput(EN_GoogolExtendOutput output)
        {
            if (!IsConnected)
            {
                return false;
            }
            int id = (byte)output / 16; //模块索引
            int index = (byte)output % 16; //在模块内的索引
            if (id == 0)
            {
                return (EOutput0 & (1 << index)) > 0;
            }
            else if (id == 1)
            {
                return (EOutput1 & (1 << index)) > 0;
            }
            return false;
        }

        /// <summary>
        /// 开启/关闭一个输出口
        /// </summary>
        /// <param name="output"></param>
        /// <param name="open"></param>
        /// <returns>通信是否成功</returns>
        private bool WriteOutput(EN_GoogolExtendOutput output, bool open)
        {
            if (!IsConnected)
            {
                return false;
            }
            byte value = (byte)output;
            return qMotion.SetSingleDOut(value / 16, value % 16, open);
            //return qMotion.SetSingleDOut(0, value, open);
        }

        /// <summary>
        /// 同时开启/关闭多个输出口
        /// </summary>
        /// <param name="outputs"></param>
        /// <param name="open"></param>
        /// <returns>通信是否成功</returns>
        private bool WriteOutput(EN_GoogolExtendOutput[] outputs, bool open)
        {
            bool[] arr = new bool[outputs.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = open;
            }
            return WriteOutput(outputs, arr);
        }

        /// <summary>
        /// 开启/关闭多个输出口，可以自定义每个输出口要开启还是关闭
        /// </summary>
        /// <param name="outputs"></param>
        /// <param name="opens"></param>
        /// <returns>通信是否成功</returns>
        private bool WriteOutput(EN_GoogolExtendOutput[] outputs, bool[] opens)
        {
            if (!IsConnected || outputs.Length != opens.Length)
            {
                return false;
            }
            //ushort output0 = EOutput0;
            //ushort output1 = EOutput1;
            //for (int i = 0; i < outputs.Length; i++)
            //{
            //    byte value = (byte)outputs[i];
            //    int idx = value % 16;
            //    if (value / 16 == 0)
            //    {
            //        bool nowOpen = (output0 & (1 << idx)) > 0;
            //        if (!opens[i] && nowOpen)
            //        {
            //            output0 -= (ushort)(1 << idx);
            //        }
            //        else if (opens[i] && !nowOpen)
            //        {
            //            output0 += (ushort)(1 << idx);
            //        }
            //    }
            //    else if (value / 16 == 1)
            //    {
            //        bool nowOpen = (output1 & (1 << idx)) > 0;
            //        if (!opens[i] && nowOpen)
            //        {
            //            output1 -= (ushort)(1 << idx);
            //        }
            //        else if (opens[i] && !nowOpen)
            //        {
            //            output1 += (ushort)(1 << idx);
            //        }
            //    }
            //}
            //return qMotion.SetDOut(0, output0) && qMotion.SetDOut(1, output1);
            for (int i = 0; i < outputs.Length; i++)
            {
                if (!WriteOutput(outputs[i], opens[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 切换一个输出的状态
        /// </summary>
        /// <param name="output"></param>
        public void ChangeOutput(EN_GoogolExtendOutput output)
        {
            WriteOutput(output, !ReadOutput(output));
        }

        #endregion

        #region 光源

        public bool SetUpLightHX(bool open)
        {
            return WriteOutput(_mGoogolParam.OutUpLightHX, open);
        }

        public bool SetUpLightTZ(bool open)
        {
            return true;
            //return WriteOutput(_mGoogolParam.OutUpLightTZ, open);
        }

        public bool SetDownLight(bool open)
        {
            return WriteOutput(_mGoogolParam.OutDownLight, open);
        }
        public bool SetFeederUpLight(bool open)
        {
            return WriteOutput(_mGoogolParam.OutFeederUpLight, open);
        }
        public bool SetPrUpLight(bool open)
        {
            return true;
            //return WriteOutput(_mGoogolParam.OutPrUpLight, open);
        }

        #endregion

        #region 气缸上下

        private EN_GoogolExtendOutput OutputCylinderUpDown(int nozzleNo, bool up)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            if (up)
            {
                return new[]
                {
                    _mGoogolParam.OutCylinderUpNo1,
                    _mGoogolParam.OutCylinderUpNo2,
                }[nozzleNo - 1];
            }
            else
            {
                return new[]
                {
                    _mGoogolParam.OutCylinderDownNo1,
                    _mGoogolParam.OutCylinderDownNo2,
                }[nozzleNo - 1];
            }
        }

        public EN_GoogolExtendInput InputCylinderUpDownReady(int nozzleNo, bool up)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            if (up)
            {
                return new[]
                {
                    _mGoogolParam.InCylinderUpReadyNo1,
                    _mGoogolParam.InCylinderUpReadyNo2,
                }[nozzleNo - 1];
            }
            else
            {
                return new[]
                {
                    _mGoogolParam.InCylinderDownReadyNo1,
                    _mGoogolParam.InCylinderDownReadyNo2,
                }[nozzleNo - 1];
            }
        }

        /// <summary>
        /// 单个气缸上/下。
        /// waitForEnd为true时，该方法直到气缸到位才会返回。
        /// waitForEnd为false时，该方法会立即返回，需要在之后使用IsCylinderUpDownReady检测是否到位。
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <param name="up"></param>
        /// <param name="waitForEnd"></param>
        /// <returns></returns>
        public bool SetCylinderUpDown(int nozzleNo, bool up, bool waitForEnd = true)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            if (_mGoogolParam.IsCylinderDoubleSwitch)
            {
                if (waitForEnd)
                {
                    return WriteOutput(
                               new[] { OutputCylinderUpDown(nozzleNo, true), OutputCylinderUpDown(nozzleNo, false) },
                               new[] { up, !up })
                           && IsCylinderUpDownReady(nozzleNo, up);
                }
                else
                {
                    return WriteOutput(
                        new[] { OutputCylinderUpDown(nozzleNo, true), OutputCylinderUpDown(nozzleNo, false) },
                        new[] { up, !up });
                }
            }
            else
            {
                //只要气缸下
                if (waitForEnd)
                {
                    return WriteOutput(OutputCylinderUpDown(nozzleNo, false), !up)
                        && IsCylinderUpDownReady(nozzleNo, up);
                }
                else
                {
                    return WriteOutput(OutputCylinderUpDown(nozzleNo, false), !up);
                }
            }
        }

        /// <summary>
        /// 返回单个气缸是否已到位
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <param name="up"></param>
        /// <returns></returns>
        public bool IsCylinderUpDownReady(int nozzleNo, bool up)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            var inputUpReady = InputCylinderUpDownReady(nozzleNo, true);
            var inputDownReady = InputCylinderUpDownReady(nozzleNo, false);
            if (_mGoogolParam.IsCylinderDoubleSwitch)
            {
                DateTime startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalSeconds < 5)
                {
                    if (!IsConnected)
                    {
                        return false;
                    }
                    if ((up && ReadInput(inputUpReady) && !ReadInput(inputDownReady))
                        || (!up && !ReadInput(inputUpReady) && ReadInput(inputDownReady)))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (up)
                {
                    DateTime startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 5)
                    {
                        if (!IsConnected)
                        {
                            return false;
                        }
                        if (up && ReadInput(inputUpReady))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    Thread.Sleep(200);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 所有气缸上/下。
        /// waitForEnd为true时，该方法直到所有气缸到位才会返回。
        /// waitForEnd为false时，该方法会立即返回，需要在之后使用IsCylindersUpDownReady检测是否到位。
        /// </summary>
        /// <param name="up"></param>
        /// <param name="waitForEnd"></param>
        /// <returns></returns>
        public bool SetAllCylindersUpDown(bool up, bool waitForEnd = true)
        {
            //启用吸嘴的气缸
            //List<EN_GoogolExtendOutput> uplist = new List<EN_GoogolExtendOutput>();
            //List<EN_GoogolExtendOutput> downlist = new List<EN_GoogolExtendOutput>();
            //if(Enable.UseNozzle1)
            //{
            //    uplist.Add(_mGoogolParam.OutCylinderUpNo1);
            //    downlist.Add(_mGoogolParam.OutCylinderDownNo1);
            //}
            //if (Enable.UseNozzle2)
            //{
            //    uplist.Add(_mGoogolParam.OutCylinderUpNo2);
            //    downlist.Add(_mGoogolParam.OutCylinderDownNo2);
            //}
            //if (Enable.UseNozzle3)
            //{
            //    uplist.Add(_mGoogolParam.OutCylinderUpNo3);
            //    downlist.Add(_mGoogolParam.OutCylinderDownNo3);
            //}
            //if (Enable.UseNozzle4)
            //{
            //    uplist.Add(_mGoogolParam.OutCylinderUpNo4);
            //    downlist.Add(_mGoogolParam.OutCylinderDownNo4);
            //}
            //var upArr = uplist.ToArray();
            //var downArr = downlist.ToArray();
            //所有气缸
            var upArr = new[]
            {
                _mGoogolParam.OutCylinderUpNo1,
                _mGoogolParam.OutCylinderUpNo2,
            };
            var downArr = new[]
            {
                _mGoogolParam.OutCylinderDownNo1,
                _mGoogolParam.OutCylinderDownNo2,
            };
            if (_mGoogolParam.IsCylinderDoubleSwitch)
            {
                if (waitForEnd)
                {
                    return WriteOutput(upArr, up) && WriteOutput(downArr, !up) && IsAllCylindersUpDownReady(up);
                }
                else
                {
                    return WriteOutput(upArr, up) && WriteOutput(downArr, !up);
                }
            }
            else
            {
                if (waitForEnd)
                {
                    return WriteOutput(downArr, !up) && IsAllCylindersUpDownReady(up);
                }
                else
                {
                    return WriteOutput(downArr, !up);
                }
            }
        }

        /// <summary>
        /// 返回所有气缸是否都已上到位
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
        public bool IsAllCylindersUpDownReady(bool up)
        {
            var inputsUpReady = new[]
            {
                InputCylinderUpDownReady(1, true),
                InputCylinderUpDownReady(2, true),
            };
            var inputsDownReady = new[]
            {
                InputCylinderUpDownReady(1, false),
                InputCylinderUpDownReady(2, false),
            };
            if (_mGoogolParam.IsCylinderDoubleSwitch)
            {
                DateTime startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalSeconds < 5)
                {
                    if (!IsConnected)
                    {
                        return false;
                    }
                    bool ok = true;
                    for (int i = 0; i < 2; i++)
                    {
                        ok &= (up && ReadInput(inputsUpReady[i]) && !ReadInput(inputsDownReady[i]))
                              || (!up && !ReadInput(inputsUpReady[i]) && ReadInput(inputsDownReady[i]));
                    }
                    if (ok)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (up)
                {
                    DateTime startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 5)
                    {
                        if (!IsConnected)
                        {
                            return false;
                        }
                        bool ok = true;
                        for (int i = 0; i < 2; i++)
                        {
                            ok &= up && ReadInput(inputsUpReady[i]);
                        }
                        if (ok)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    Thread.Sleep(200);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 两个气缸上/下。
        /// waitForEnd为true时，该方法直到所有气缸到位才会返回。
        /// waitForEnd为false时，该方法会立即返回，需要在之后使用IsCylindersUpDownReady检测是否到位。
        /// </summary>
        /// <param name="up"></param>
        /// <param name="waitForEnd"></param>
        /// <returns></returns>
        public bool SetTwoCylindersUpDown(bool is13, bool up, bool waitForEnd = true)
        {
            var upArr = new[]
            {
                is13 ? _mGoogolParam.OutCylinderUpNo1 : _mGoogolParam.OutCylinderUpNo2,
                is13 ? _mGoogolParam.OutCylinderUpNo3 : _mGoogolParam.OutCylinderUpNo4,
            };
            var downArr = new[]
            {
                is13 ? _mGoogolParam.OutCylinderDownNo1 : _mGoogolParam.OutCylinderDownNo2,
                is13 ? _mGoogolParam.OutCylinderDownNo3 : _mGoogolParam.OutCylinderDownNo4,
            };
            if (_mGoogolParam.IsCylinderDoubleSwitch)
            {
                if (waitForEnd)
                {
                    return WriteOutput(upArr, up) && WriteOutput(downArr, !up) && IsTwoCylindersUpDownReady(is13, up);
                }
                else
                {
                    return WriteOutput(upArr, up) && WriteOutput(downArr, !up);
                }
            }
            else
            {
                if (waitForEnd)
                {
                    return WriteOutput(downArr, !up) && IsTwoCylindersUpDownReady(is13, up);
                }
                else
                {
                    return WriteOutput(downArr, !up);
                }
            }
        }

        /// <summary>
        /// 返回两个气缸是否都已上到位
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
        public bool IsTwoCylindersUpDownReady(bool is13, bool up)
        {
            var inputsUpReady = new[]
            {
                InputCylinderUpDownReady(is13 ? 1 : 2, true),
                InputCylinderUpDownReady(is13 ? 3 : 4, true),
            };
            var inputsDownReady = new[]
            {
                InputCylinderUpDownReady(is13 ? 1 : 2, false),
                InputCylinderUpDownReady(is13 ? 3 : 4, false),
            };
            if (_mGoogolParam.IsCylinderDoubleSwitch)
            {
                DateTime startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalSeconds < 5)
                {
                    if (!IsConnected)
                    {
                        return false;
                    }
                    bool ok = true;
                    for (int i = 0; i < 2; i++)
                    {
                        ok &= (up && ReadInput(inputsUpReady[i]) && !ReadInput(inputsDownReady[i]))
                              || (!up && !ReadInput(inputsUpReady[i]) && ReadInput(inputsDownReady[i]));
                    }
                    if (ok)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (up)
                {
                    DateTime startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 5)
                    {
                        if (!IsConnected)
                        {
                            return false;
                        }
                        bool ok = true;
                        for (int i = 0; i < 2; i++)
                        {
                            ok &= up && ReadInput(inputsUpReady[i]);
                        }
                        if (ok)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    Thread.Sleep(200);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 吸嘴真空吸/吹

        public EN_GoogolExtendOutput OutputVacuumSuctionRupture(int nozzleNo, bool suction)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            if (suction)
            {
                return new[]
                {
                    _mGoogolParam.OutVacuumSuctionNo1,
                    _mGoogolParam.OutVacuumSuctionNo2,
                }[nozzleNo - 1];
            }
            else
            {
                return new[]
                {
                    _mGoogolParam.OutVacuumRuptureNo1,
                    _mGoogolParam.OutVacuumRuptureNo2,
                }[nozzleNo - 1];
            }
        }

        public EN_GoogolExtendInput InputMaterialReady(int nozzleNo)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            return new[]
            {
                _mGoogolParam.InMaterialReadyNo1,
                _mGoogolParam.InMaterialReadyNo2,
            }[nozzleNo - 1];
        }

        /// <summary>
        /// 单个吸嘴开吸TT/开吹FT/关吸TF/关吹FF
        /// </summary>
        /// <param name="nozzleNo">吸嘴</param>
        /// <param name="suction">true吸，false吹</param>
        /// <param name="open">开关</param>
        /// <returns></returns>
        public bool SetVacuum(int nozzleNo, bool suction, bool open)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            return WriteOutput(OutputVacuumSuctionRupture(nozzleNo, suction), open);
        }

        /// <summary>
        /// 单个吸嘴关吸开吹F/关吹开吸T
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <param name="suction"></param>
        /// <returns></returns>
        public bool SetVacuum(int nozzleNo, bool suction)
        {
            return WriteOutput(
                new[] { OutputVacuumSuctionRupture(nozzleNo, true), OutputVacuumSuctionRupture(nozzleNo, false) },
                new[] { suction, !suction });
        }

        /// <summary>
        /// 单个吸嘴是否开吸
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <returns></returns>
        public bool IsNozzleOpenSuction(int nozzleNo)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            if (!IsConnected)
            {
                return false;
            }
            return ReadOutput(OutputVacuumSuctionRupture(nozzleNo, true));
        }

        /// <summary>
        /// 返回吸嘴是否吸到料
        /// </summary>
        /// <param name="nozzleNo"></param>
        /// <returns></returns>
        public bool GetMaterialReady(int nozzleNo)
        {
            if (nozzleNo < 1 || nozzleNo > 2)
            {
                throw new Exception("吸嘴号" + nozzleNo + "不是1-2");
            }
            if (!IsConnected)
            {
                return false;
            }
            if (_mGoogolParam.IsCylinderDoubleSwitch)
            {
                int _EInput0 = 0;
                int _EInput1 = 0;
                qMotion.GetDIn(0, ref _EInput0);
                qMotion.GetDIn(1, ref _EInput1);
                EInput0 = (ushort)_EInput0;
                EInput1 = (ushort)_EInput1;
            }
            else
            {
                int _EInput0 = 0;
                qMotion.GetDIn(0, ref _EInput0);
                EInput0 = (ushort)_EInput0;
            }
            return ReadInput(InputMaterialReady(nozzleNo));
        }

        /// <summary>
        /// 返回四个吸嘴吸料状态，返回值表示通信是否成功
        /// </summary>
        /// <returns></returns>
        public bool GetMaterialsReady(out bool[] status)
        {
            status = new bool[2];
            if (!IsConnected)
            {
                return false;
            }
            for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
            {
                status[nozzleNo - 1] = ReadInput(InputMaterialReady(nozzleNo));
            }
            return true;
        }

        #endregion

        #region 飞达左右吹气

        public bool SetFeederVacuum(bool left, bool open)
        {
            if (Enable.FeederId == FeederId.左飞达)
            {
                return WriteOutput(left ? _mGoogolParam.OutFeederVacuumRuptureLeft : _mGoogolParam.OutFeederVacuumRuptureRight, open);
            }
            else
            {
                return WriteOutput(left ? _mGoogolParam.OutFeeder2VacuumRuptureLeft : _mGoogolParam.OutFeeder2VacuumRuptureRight, open);
            }
        }

        #endregion

        #endregion

        #region Speed

        public bool SetSpeed(En_AxisNum axis, float[] spd)
        {
            bool rs = false;
            try
            {
                float[] speedpluse = new float[3];
                for (int i = 0; i < spd.Length; i++)
                {
                    speedpluse[i] = spd[i] * Pulseequ[(byte)axis];
                }

                rs = qMotion.SetMoveSpeed((int)axis, speedpluse);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error,
                    $" Axis {axis.ToString()} Speed {NLogTrace.GetFloatArrayString(spd)} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetCustomSingleSpeed  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }

        public bool SetMoveSpeed(En_AxisNum axis, float speed)
        {
            try
            {
                bool rs = true;
                float[] speedParam = _mGoogolParam.GetSpeed(axis, En_SpeedType.Work);
                speedParam[1] = speed;
                rs &= qMotion.SetMoveSpeed((int)axis, ConvertToPluse(axis, speedParam));
                return rs;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, ex.Message + ex.StackTrace);
            }
            return false;
        }


        public bool SetSpeed(En_AxisNum axis, En_SpeedType speedtype)
        {
            bool rs = false;
            try
            {
                float[] speed = _mGoogolParam.GetSpeed(axis, speedtype);
                float[] speedpluse = new float[3];
                for (int i = 0; i < speed.Length; i++)
                {
                    speedpluse[i] = speed[i] * Pulseequ[(byte)axis];
                }
                rs = qMotion.SetMoveSpeed((int)axis, speedpluse);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error,
                    $" Axis {axis.ToString()} Speed {NLogTrace.GetFloatArrayString(speed)} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSpeed {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }

        public bool SetSpeedAll(En_SpeedType speedtype)
        {
            bool ret = true;
            try
            {
                var axisNum = Enum.GetNames(typeof(En_AxisNum));
                for (int i = 0; i < axisNum.Length; i++)
                {
                    if (!SetSpeed((En_AxisNum)Enum.Parse(typeof(En_AxisNum), axisNum[i]), speedtype))
                        ret = false;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSpeedAll  {ex.ToString()},{ex.StackTrace}");
            }
            return ret;
        }

        #endregion

        #region 当前坐标

        public bool GetCurPos()
        {
            bool rs = true;
            double[] xyzrrpulse1 = new double[2];
            double[] xyzrrpulse2 = new double[5];
            if (!qMotion.GetEncPos((byte)En_AxisNum.X1, 2, ref xyzrrpulse1))
            {
                CurPos[0] = -999.999f;
                CurPos[1] = -999.999f;
                rs = false;
            }
            else
            {
                CurPos[(byte)En_AxisNum.X1] = (float)Math.Round(xyzrrpulse1[0] / Pulseequ[(byte)En_AxisNum.X1], 3);
                CurPos[(byte)En_AxisNum.Y1] = (float)Math.Round(xyzrrpulse1[1] / Pulseequ[(byte)En_AxisNum.Y1], 3);
            }

            if (!qMotion.GetEncPos((byte)En_AxisNum.X2, 5, ref xyzrrpulse2))
            {
                CurPos[2] = -999.999f;
                CurPos[3] = -999.999f;
                CurPos[4] = -999.999f;
                CurPos[5] = -999.999f;
                CurPos[6] = -999.999f;
                //CurPos[7] = -999.999f;
                //CurPos[8] = -999.999f;
                rs = false;
            }
            else
            {
                CurPos[(byte)En_AxisNum.X2] = (float)Math.Round(xyzrrpulse2[0] / Pulseequ[(byte)En_AxisNum.X2], 3);
                CurPos[(byte)En_AxisNum.Y2] = (float)Math.Round(xyzrrpulse2[1] / Pulseequ[(byte)En_AxisNum.Y2], 3);
                CurPos[(byte)En_AxisNum.Z2] = (float)Math.Round(xyzrrpulse2[2] / Pulseequ[(byte)En_AxisNum.Z2], 3);
                CurPos[(byte)En_AxisNum.R1] = (float)Math.Round(xyzrrpulse2[3] / Pulseequ[(byte)En_AxisNum.R1], 3);
                CurPos[(byte)En_AxisNum.R2] = (float)Math.Round(xyzrrpulse2[4] / Pulseequ[(byte)En_AxisNum.R2], 3);
                //CurPos[(byte)En_AxisNum.R3] = (float)Math.Round(xyzrrpulse2[5] / Pulseequ[(byte)En_AxisNum.R3], 3);
                //CurPos[(byte)En_AxisNum.R4] = (float)Math.Round(xyzrrpulse2[6] / Pulseequ[(byte)En_AxisNum.R4], 3);
            }
            IsConnected = rs;
            return rs;
        }

        public bool GetCurPos(En_StationNo stationNo)
        {
            bool rs = true;
            if (stationNo == En_StationNo.StationNo1)
            {
                double[] xyzrrpulse1 = new double[2];
                if (!qMotion.GetEncPos((byte)En_AxisNum.X1, 2, ref xyzrrpulse1))
                {
                    CurPos[(byte)En_AxisNum.X1] = -999.999f;
                    CurPos[(byte)En_AxisNum.Y1] = -999.999f;
                    rs = false;
                }
                else
                {
                    CurPos[(byte)En_AxisNum.X1] = (float)Math.Round(xyzrrpulse1[0] / Pulseequ[(byte)En_AxisNum.X1], 3);
                    CurPos[(byte)En_AxisNum.Y1] = (float)Math.Round(xyzrrpulse1[1] / Pulseequ[(byte)En_AxisNum.Y1], 3);
                }
            }
            else if (stationNo == En_StationNo.StationNo2)
            {
                double[] xyzrrpulse2 = new double[7];

                if (!qMotion.GetEncPos((byte)En_AxisNum.X2, 5, ref xyzrrpulse2))
                {
                    CurPos[(byte)En_AxisNum.X2] = -999.999f;
                    CurPos[(byte)En_AxisNum.Y2] = -999.999f;
                    CurPos[(byte)En_AxisNum.Z2] = -999.999f;
                    CurPos[(byte)En_AxisNum.R1] = -999.999f;
                    CurPos[(byte)En_AxisNum.R2] = -999.999f;
                    //CurPos[(byte)En_AxisNum.R3] = -999.999f;
                    //CurPos[(byte)En_AxisNum.R4] = -999.999f;
                    rs = false;
                }
                else
                {
                    CurPos[(byte)En_AxisNum.X2] = (float)Math.Round(xyzrrpulse2[0] / Pulseequ[(byte)En_AxisNum.X2]);
                    CurPos[(byte)En_AxisNum.Y2] = (float)Math.Round(xyzrrpulse2[1] / Pulseequ[(byte)En_AxisNum.Y2]);
                    CurPos[(byte)En_AxisNum.Z2] = (float)Math.Round(xyzrrpulse2[2] / Pulseequ[(byte)En_AxisNum.Z2]);
                    CurPos[(byte)En_AxisNum.R1] = (float)Math.Round(xyzrrpulse2[3] / Pulseequ[(byte)En_AxisNum.R1], 3);
                    CurPos[(byte)En_AxisNum.R2] = (float)Math.Round(xyzrrpulse2[4] / Pulseequ[(byte)En_AxisNum.R2], 3);
                    //CurPos[(byte)En_AxisNum.R3] = (float)Math.Round(xyzrrpulse2[5] / Pulseequ[(byte)En_AxisNum.R3], 3);
                    //CurPos[(byte)En_AxisNum.R4] = (float)Math.Round(xyzrrpulse2[6] / Pulseequ[(byte)En_AxisNum.R4], 3);
                }
            }
            return rs;
        }

        public float GetAxisCurPos(En_AxisNum axis)
        {
            double[] pluse = new double[1];
            qMotion.GetPrfPos((int)axis, 1, ref pluse);
            return (float)(pluse[0] / _mGoogolParam.PluseEquivalents[(int)axis]);
        }

        #endregion

        #region Tape站

        public bool AxisWaitResetZero(short asid, int timeout = 40000)
        {
            ushort val = 0;
            int _timeout = timeout;
            while (timeout > 0)
            {
                timeout--;
                qMotion.EcatGetHomingStatus(asid, ref val);
                Thread.Sleep(10);
                if (val == 3)
                    break;
            }
            qMotion.EcatSetHomingMode(asid, 8);

            if (val != 3)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 判断是否驱动器报警
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public bool IsDriverAlarm(En_AxisNum axis)
        {
            return (AxisSts[(int)axis] & 0x01 << 1) != 0x00;
        }

        /// <summary>
        /// 判断轴是否正限位报警
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public bool IsAxisPosiLimitAlarm(En_AxisNum axis)
        {
            return (AxisSts[(int)axis] & 0x01 << 5) != 0x00;
        }

        /// <summary>
        /// 判断轴是否负限位报警
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public bool IsAxisNaviLimitAlarm(En_AxisNum axis)
        {
            return (AxisSts[(int)axis] & 0x01 << 6) != 0x00;
        }

        /// <summary>
        /// 判断轴是否上使能
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public bool IsAxisEnable(En_AxisNum axis)
        {
            return (AxisSts[(int)axis] & 0x01 << 9) != 0x00;
        }

        public bool WaitAxisMoveEnd()
        {
            DateTime starttime = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - starttime).TotalSeconds > 30)
                {
                    Console.WriteLine("运动超过20s");
                    break;
                }
                qMotion.CleanAlarm();
                if (GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                {
                    // false 说明在运动 ，true代表静止
                    return true;
                }
                //int[] sts = new int[8];              
                //qMotion.AxisGetSts(0, ref sts, 8);

                //int[] singlests = new int[1];
                //qMotion.AxisGetSts(8, ref singlests, 1);

                //Console.WriteLine($"Axis Status:{sts[0].ToString()}");
                //if (((sts[0] & 0x01 << 10) == 0x00) &&
                //    ((sts[1] & 0x01 << 10) == 0x00) &&
                //    ((sts[2] & 0x01 << 10) == 0x00) &&
                //    ((sts[3] & 0x01 << 10) == 0x00) &&
                //    ((sts[4] & 0x01 << 10) == 0x00) &&
                //    ((sts[5] & 0x01 << 10) == 0x00) &&
                //    ((sts[6] & 0x01 << 10) == 0x00) &&
                //    ((sts[7] & 0x01 << 10) == 0x00) &&
                //    ((singlests[0] & 0x01 << 10) == 0x00))
                //{
                //    return true;
                //}
            }
            return false;
        }

        public bool MoveAbsoluteSingleAxis(En_AxisNum axis, float pos, bool waitforend, bool buseflag = true,
            bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { axis }, new float[] { pos }, waitforend, buseflag, true,
                checkPLC);
        }

        public bool MoveAbsoluteX1Y1(float[] xy, bool waitforend, bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X1, En_AxisNum.Y1 }, new float[] { xy[0], xy[1] },
                waitforend, buseflag, true, checkPLC);
        }

        public bool MoveAbsoluteX2Y2(float[] xy, bool waitforend, bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2 }, new float[] { xy[0], xy[1] },
                waitforend, buseflag, true, checkPLC);
        }

        public bool MoveAbsoluteX2Y2R(En_AxisNum axisR, float[] xyr, bool waitforend,
            bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, axisR },
                new float[] { xyr[0], xyr[1], xyr[2] }, waitforend, buseflag, true, checkPLC);
        }

        public bool MoveAbsoluteX2Y2Z2(float[] xyz, bool waitforend, bool buseflag = true,
            bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.Z2 },
                new float[] { xyz[0], xyz[1], xyz[2] }, waitforend, buseflag, true, checkPLC);
        }

        public bool MoveAbsoluteX2Y2Z2R(En_AxisNum axisR, float[] xyzr, bool waitforend,
            bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.Z2, axisR },
                new float[] { xyzr[0], xyzr[1], xyzr[2], xyzr[3] }, waitforend, buseflag, true, checkPLC);
        }
        public bool MoveAbsoluteX2Y2Z2RAll(float[] xyzrall, bool waitforend,
            bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.Z2, En_AxisNum.R1, En_AxisNum.R2 },
               xyzrall, waitforend, buseflag, true, checkPLC);
        }
        public bool MoveAbsoluteX2Y2RAll(float[] xyzrall, bool waitforend,
            bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.R1, En_AxisNum.R2 },
               xyzrall, waitforend, buseflag, true, checkPLC);
        }
        public bool MoveAbsoluteX2Y2Z2RR(float[] xyzrr, bool waitforend, bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.Z2, En_AxisNum.R1, En_AxisNum.R2 },
                    new float[] { xyzrr[0], xyzrr[1], xyzrr[2], xyzrr[3], xyzrr[4] }, waitforend, buseflag, true, checkPLC);
        }

        //public bool MoveAbsoluteX2Y2Z2RR(float[] xyzrr, bool is13, bool waitforend, bool buseflag = true, bool checkPLC = true)
        //{
        //    var r1 = is13 ? En_AxisNum.R1 : En_AxisNum.R2;
        //    var r2 = is13 ? En_AxisNum.R3 : En_AxisNum.R4;
        //    return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.Z2, r1, r2 },
        //            new float[] { xyzrr[0], xyzrr[1], xyzrr[2], xyzrr[3], xyzrr[4] }, waitforend, buseflag, true, checkPLC);
        //}
        public bool MoveAbsoluteX2Y2Z2AllR(float[] xyzrr, bool waitforend, bool buseflag = true, bool checkPLC = true)
        {
            var r1 = En_AxisNum.R1;
            var r2 = En_AxisNum.R2;
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.Z2, r1, r2 },
                    new float[] { xyzrr[0], xyzrr[1], xyzrr[2], xyzrr[3], xyzrr[4] }, waitforend, buseflag, true, checkPLC);
        }

        public bool MoveRelativeSingleAxis(En_AxisNum axis, float dis, bool waitforend, bool buseflag = true,
            bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { axis }, new float[] { dis }, waitforend, buseflag, false,
                checkPLC);
        }

        public bool MoveRelativeX1Y1(float[] xy, bool waitforend, bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X1, En_AxisNum.Y1 }, new float[] { xy[0], xy[1] },
                waitforend, buseflag, false, checkPLC);
        }

        public bool MoveRelativeX2Y2(float[] xy, bool waitforend, bool buseflag = true, bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2 }, new float[] { xy[0], xy[1] },
                waitforend, buseflag, false, checkPLC);
        }

        public bool MoveRelativeX2Y2Z2(float[] xyz, bool waitforend, bool buseflag = true,
            bool checkPLC = true)
        {
            return MoveToPointAxes(new En_AxisNum[] { En_AxisNum.X2, En_AxisNum.Y2, En_AxisNum.Z2 },
                new float[] { xyz[0], xyz[1], xyz[2] }, waitforend, buseflag, false, checkPLC);
        }
        /// <summary>
        /// 是否复位或存在轴报警
        /// </summary>
        /// <returns></returns>
        public bool IsReset()
        {
            if (!IsResetCompleted)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "机台未复位完成，无法移动轴", En_Logout_Type.Run, true);
                return true;
            }
            if (!qMotion.CleanAlarm())
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "存在轴报警，无法移动轴", En_Logout_Type.Run, true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 控制轴移动。
        /// </summary>
        /// <param name="axes"></param>
        /// <param name="pos"></param>
        /// <param name="waitforend"></param>
        /// <param name="buseflag"></param>
        /// <param name="absolute"></param>
        /// <param name="checkPLC">轴运动完成后是否检测plc的暂停状态。
        /// 如果需要确保n次轴运动（即调用n次该方法）之间不会因为plc暂停/急停等停止，则除最后一次调用外的其他运动都应将该值设为false。</param>
        /// <returns></returns>
        private bool MoveToPointAxes(En_AxisNum[] axes, float[] pos, bool waitforend, bool buseflag, bool absolute,
            bool checkPLC = true)
        {
            if (!IsResetCompleted)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "机台未复位完成，无法移动轴", En_Logout_Type.Run, true);
                return false;
            }
            if (!qMotion.CleanAlarm())
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "存在轴报警，无法移动轴", En_Logout_Type.Run, true);
                return false;
            }
            if (axes.Length != pos.Length)
            {
                return false;
            }
            float[] pluse = new float[axes.Length];
            for (int i = 0; i < axes.Length; i++)
            {
                pluse[i] = pos[i] * Pulseequ[(byte)axes[i]];
                if (absolute)
                {
                    if (!qMotion.MoveAbsoluteToPointSingleAxis((short)axes[i], pluse[i], false, buseflag))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!qMotion.MoveRelativeToPointSingleAxis((short)axes[i], pluse[i], false, buseflag))
                    {
                        return false;
                    }
                }
            }
            if (waitforend == false)
            {
                return true;
            }
            PLC_Component _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            for (int i = 0; i < axes.Length; i++)
            {
                if (absolute)
                {
                    if (!qMotion.MoveAbsoluteToPointSingleAxis((short)axes[i], pluse[i], true, buseflag))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!qMotion.MoveRelativeToPointSingleAxis((short)axes[i], pluse[i], true, buseflag))
                    {
                        return false;
                    }
                }
            }
            if (checkPLC)
            {
                while (_plc_Component.IsCurStop)
                {
                    Thread.Sleep(100);
                    if (_plc_Component.IsCurEStop || _plc_Component.IsCurReset)
                    {
                        return false;
                    }
                }
            }
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            if (_plc_Component.IsCurEStop || (_plc_Component.IsCurReset && !_baseBiz.GetRunStep1IsReset()))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 自动运动防呆
        /// </summary>
        /// <param name="en_AxisNum"></param>
        /// <param name="v"></param>
        /// <param name="absolute"></param>
        /// <returns></returns>
        public void MoveStroke()
        {
            GetCurPos();
            bool IsStroke = false;
            if (!GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled) && IsAutoStroke)
            {
                for (int i = 0; i < axisfloats.Length; i++)
                {

                    if (CurPos[i] > axisfloats[i])
                    {
                        if (i == (byte)En_AxisNum.X1)
                        {
                            if (CurPos[(byte)En_AxisNum.X1] > _mGoogolParam.X1Max)
                            {
                                IsStroke = true;
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出相机X轴最大值，停止运动", En_Logout_Type.Alarm, true);

                                break;
                            }
                        }
                        else if (i == (byte)En_AxisNum.X2)
                        {
                            if (CurPos[(byte)En_AxisNum.X2] > _mGoogolParam.X2Max)
                            {
                                IsStroke = true;
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装X轴最大值，停止运动", En_Logout_Type.Alarm, true);

                                break;
                            }
                        }
                        else if (i == (byte)En_AxisNum.Y1)
                        {
                            if (CurPos[(byte)En_AxisNum.Y1] > _mGoogolParam.Y1Max)
                            {
                                IsStroke = true;
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出相机Y轴最大值，停止运动", En_Logout_Type.Alarm, true);

                                break;
                            }
                        }
                        else if (i == (byte)En_AxisNum.Y2)
                        {
                            if (CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.Y2Max)
                            {
                                IsStroke = true;
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装Y轴最大值，停止运动", En_Logout_Type.Alarm, true);

                                break;
                            }
                        }
                        else if (i == (byte)En_AxisNum.Z2)
                        {
                            if (CurPos[(byte)En_AxisNum.Z2] > _mGoogolParam.Z2Max)
                            {
                                IsStroke = true;
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装Z轴最大值，停止运动", En_Logout_Type.Alarm, true);

                                break;
                            }
                        }
                        if ((i == (byte)En_AxisNum.X1) || i == (byte)En_AxisNum.X2 || i == (byte)En_AxisNum.Y1 || i == (byte)En_AxisNum.Y2)
                        {
                            if (CurPos[(byte)En_AxisNum.X1] + CurPos[(byte)En_AxisNum.X2] > _mGoogolParam.X1X2Max &&
                       CurPos[(byte)En_AxisNum.Y1] + CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.Y1Y2Max)
                            {
                                IsStroke = true;
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "两个工站处于交叉状态，检测到撞击，停止运动", En_Logout_Type.Alarm, true);
                                break;
                            }
                            if (i == (byte)En_AxisNum.Y1 || i == (byte)En_AxisNum.Y2)
                            {
                                if (CurPos[(byte)En_AxisNum.Y1] + CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.Y1Y2JGMax)
                                {
                                    IsStroke = true;
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "两个工站处于交叉状态，检测到撞击，停止运动", En_Logout_Type.Alarm, true);

                                    break;
                                }
                            }
                            if (i == (byte)En_AxisNum.X2 || i == (byte)En_AxisNum.Y2)
                            {
                                //if (CurPos[(byte)En_AxisNum.Z2] > _mGoogolParam.Z2Height)
                                //{
                                //    IsStroke = true;
                                //    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "贴装Z轴超出安全范围", En_Logout_Type.Alarm, true);

                                //    break;
                                //}
                            }
                        }
                    }
                }
            }
            else
            {
                SetIsAutoStroke(true);
            }
            for (int i = 0; i < axisfloats.Length; i++)
            {
                axisfloats[i] = CurPos[i].DeepCopy();
            }
            if (IsStroke)
            {
                StopAllAxisMove();
            }
        }
        /// <summary>
        /// 运动防呆
        /// </summary>
        /// <param name="_AxisNum"></param>
        /// <param name="IsAdd"></param>
        public async void MoveStroke(En_AxisNum _AxisNum, bool IsAdd)
        {

            await Task.Run(() =>
            {
                SetIsAutoStroke(false);
                int stopnum = 3;
                while (true)
                {
                    GetCurPos();
                    if (IsAdd)
                    {
                        switch (_AxisNum)
                        {
                            case En_AxisNum.X1:
                                if (CurPos[(byte)En_AxisNum.X1] > _mGoogolParam.X1Max)
                                {
                                    StopAllAxisMove();
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出相机X轴最大值，停止运动", En_Logout_Type.Alarm, true);
                                    return;
                                }
                                break;
                            case En_AxisNum.X2:
                                if (CurPos[(byte)En_AxisNum.X2] > _mGoogolParam.X2Max)
                                {
                                    StopAllAxisMove();
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装X轴最大值，停止运动", En_Logout_Type.Alarm, true);
                                    return;
                                }
                                break;
                            case En_AxisNum.Y1:
                                if (CurPos[(byte)En_AxisNum.Y1] > _mGoogolParam.Y1Max)
                                {
                                    StopAllAxisMove();
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出相机Y轴最大值，停止运动", En_Logout_Type.Alarm, true);
                                    return;
                                }
                                break;
                            case En_AxisNum.Y2:
                                if (CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.Y2Max)
                                {
                                    StopAllAxisMove();
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装Y轴最大值，停止运动", En_Logout_Type.Alarm, true);
                                    return;
                                }
                                break;
                            case En_AxisNum.Z2:
                                if (CurPos[(byte)En_AxisNum.Z2] > _mGoogolParam.Z2Max)
                                {
                                    StopAllAxisMove();
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装Z轴最大值，停止运动", En_Logout_Type.Alarm, true);
                                    return;
                                }
                                break;
                        }
                        if (CurPos[(byte)En_AxisNum.X1] + CurPos[(byte)En_AxisNum.X2] > _mGoogolParam.X1X2Max &&
                        CurPos[(byte)En_AxisNum.Y1] + CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.Y1Y2Max)
                        {
                            StopAllAxisMove();
                            NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "两个工站处于交叉状态，检测到撞击，停止运动", En_Logout_Type.Alarm, true);
                            return;
                        }
                        if (CurPos[(byte)En_AxisNum.Y1] + CurPos[(byte)En_AxisNum.Y2] > _mGoogolParam.Y1Y2JGMax)
                        {
                            StopAllAxisMove();
                            NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "两个工站处于交叉状态，检测到撞击，停止运动", En_Logout_Type.Alarm, true);
                            return;
                        }
                    }
                    if (_AxisNum == En_AxisNum.X2 || _AxisNum == En_AxisNum.Y2)
                    {
                        if (CurPos[(byte)En_AxisNum.Z2] > _mGoogolParam.Z2Height)
                        {
                            StopAllAxisMove();
                            NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "贴装Z轴超出安全范围", En_Logout_Type.Alarm, true);
                            return;
                        }
                    }
                    if (GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        if (stopnum <= 0)
                        {
                            break;
                        }
                        stopnum--;
                    }
                }
            });
        }

        public bool MoveStroke(En_AxisNum _AxisNum, float pos)
        {
            SetIsAutoStroke(false);
            GetCurPos();
            float x1 = CurPos[(byte)En_AxisNum.X1];
            float x2 = CurPos[(byte)En_AxisNum.X2];
            float y1 = CurPos[(byte)En_AxisNum.Y1];
            float y2 = CurPos[(byte)En_AxisNum.Y2];
            float z2 = CurPos[(byte)En_AxisNum.Z2];
            switch (_AxisNum)
            {
                case En_AxisNum.X1:
                    x1 += pos;
                    if (x1 > _mGoogolParam.X1Max)
                    {
                        StopAllAxisMove();
                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出相机X轴最大值，停止运动", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    break;
                case En_AxisNum.X2:
                    x2 += pos;
                    if (x2 > _mGoogolParam.X2Max)
                    {
                        StopAllAxisMove();
                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装X轴最大值，停止运动", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    break;
                case En_AxisNum.Y1:
                    y1 += pos;
                    if (y1 > _mGoogolParam.Y1Max)
                    {
                        StopAllAxisMove();
                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出相机Y轴最大值，停止运动", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    break;
                case En_AxisNum.Y2:
                    y2 += pos;
                    if (y2 > _mGoogolParam.Y2Max)
                    {
                        StopAllAxisMove();
                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装Y轴最大值，停止运动", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    break;
                case En_AxisNum.Z2:
                    z2 += pos;
                    if (z2 > _mGoogolParam.Z2Max)
                    {
                        StopAllAxisMove();
                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "超出贴装Z轴最大值，停止运动", En_Logout_Type.Alarm, true);
                        return false;
                    }
                    break;
            }
            if (x1 + x2 > _mGoogolParam.X1X2Max &&
            y1 + y2 > _mGoogolParam.Y1Y2Max)
            {
                StopAllAxisMove();
                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "两个工站处于交叉状态，检测到撞击，停止运动", En_Logout_Type.Alarm, true);
                return false;
            }
            if (y1 + y2 > _mGoogolParam.Y1Y2JGMax)
            {
                StopAllAxisMove();
                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "两个工站处于交叉状态，检测到撞击，停止运动", En_Logout_Type.Alarm, true);
                return false;
            }
            if (_AxisNum == En_AxisNum.X2 || _AxisNum == En_AxisNum.Y2)
            {
                if (z2 > _mGoogolParam.Z2Height)
                {
                    StopAllAxisMove();
                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "贴装Z轴超出安全高度范围", En_Logout_Type.Alarm, true);
                    return false;
                }
            }
            return true;
        }

        public void SetIsAutoStroke(bool isAutoStroke)
        {
            lock (obj)
            {
                IsAutoStroke = isAutoStroke;
            }
        }

        /// <summary>
        /// 测试哪些运动指令的waitforend可用。
        /// 对于不可用的四种情况，现均已修复。
        /// </summary>
        public void testGoogolMove()
        {
            /*
             * 测试结果：
             * MoveAbsoluteToPointSingleAxis  可用waitforend
             * MoveAbsoluteToPointXY          可用waitforend
             * MoveAbsoluteToPointXYR         可用waitforend
             * MoveAbsoluteToPointXYZ         不可用waitforend
             * MoveAbsoluteToPointXYZR        不可用waitforend
             * 
             * MoveRelativeToPointSingleAxis  可用waitforend
             * MoveRelativeToPointXY          不可用waitforend
             * MoveRelativeToPointXYZ         不可用waitforend
             */
            MoveAbsoluteSingleAxis(En_AxisNum.X2, 20, true);
            MoveAbsoluteSingleAxis(En_AxisNum.X2, 0, false);

            MoveAbsoluteX2Y2(new float[] { 20, 20 }, true);
            MoveAbsoluteX2Y2(new float[] { 0, 0 }, false);

            MoveAbsoluteX2Y2R(En_AxisNum.R1, new float[] { 20, 20, 20 }, true);
            MoveAbsoluteX2Y2R(En_AxisNum.R1, new float[] { 0, 0, 0 }, false);

            MoveAbsoluteX2Y2Z2(new float[] { 20, 20, 20 }, true);
            MoveAbsoluteX2Y2Z2(new float[] { 0, 0, 0 }, false);

            MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, new float[] { 20, 20, 20, 20 }, true);
            MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, new float[] { 0, 0, 0, 0 }, false);

            //测试相对距离一定要等到轴走完再进行下一步！
            MoveRelativeSingleAxis(En_AxisNum.X2, 20, true);
            MoveRelativeSingleAxis(En_AxisNum.X2, -20, false);

            MoveRelativeX2Y2(new float[] { 20, 20 }, true);
            MoveRelativeX2Y2(new float[] { -20, -20 }, false);

            MoveRelativeX2Y2Z2(new float[] { 20, 20, 20 }, true);
            MoveRelativeX2Y2Z2(new float[] { -20, -20, -20 }, false);

            //防呆
            MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, new float[] { 0, 0, 0, 0 }, true);
        }

        public bool SetJogSpeed(En_AxisNum axisId, float[] speed, bool dir = true)
        {
            float[] speedpluse = new float[3] { 0, 0, 0 };
            for (int i = 0; i < speed.Length; i++)
            {
                speedpluse[i] = speed[i] * Pulseequ[(byte)axisId];
            }
            if (dir == false) speedpluse[1] = -1 * speed[1] * Pulseequ[(byte)axisId];
            return qMotion.SetJogSpeed((int)axisId, speedpluse);
        }

        public bool SetJogSpeed(En_AxisNum axis, En_SpeedType speedtype, bool dir = true)
        {
            bool rs = false;
            try
            {
                float[] speedpluse = new float[3] { 0, 0, 0 };
                float[] speed = _mGoogolParam.GetSpeed(axis, speedtype);

                for (int i = 0; i < speed.Length; i++)
                {
                    speedpluse[i] = speed[i] * Pulseequ[(byte)axis];
                }

                if (dir == false) speedpluse[1] = -1 * speed[1] * Pulseequ[(byte)axis];

                rs = qMotion.SetJogSpeed((int)axis, speedpluse);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error,
                    $" Axis {axis.ToString()} Speed {NLogTrace.GetFloatArrayString(speed)} rs {rs.ToString()}");
                /****新增防呆***/
                MoveStroke(axis, dir);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSpeed {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }

        public bool SetJogSpeedAll(En_SpeedType speedtype)
        {
            bool ret = true;
            try
            {
                var axisNum = Enum.GetNames(typeof(En_AxisNum));
                for (int i = 0; i < axisNum.Length; i++)
                {
                    if (!SetJogSpeed((En_AxisNum)Enum.Parse(typeof(En_AxisNum), axisNum[i]), speedtype))
                        ret = false;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSpeedAll  {ex.ToString()},{ex.StackTrace}");
            }
            return ret;
        }

        public bool JogAxis(En_AxisNum axisId, sbyte dir, bool isRun)
        {
            return qMotion.JogAxis((int)axisId, dir, isRun);
        }

        public bool ZeroPos(En_AxisNum axisId, int count = 1)
        {
            return qMotion.ZeroPos((int)axisId, count);
        }

        public bool ZeroPos(En_StationNo station)
        {
            if (station == En_StationNo.StationNo1)
            {
                return ZeroPos(En_AxisNum.X1, 2);
            }
            else
            {
                return ZeroPos(En_AxisNum.X2, 7);
            }
        }

        public string GetCurInfo()
        {
            StringBuilder sbinfo = new StringBuilder();
            sbinfo.Append(" X1: ");
            sbinfo.Append(CurPos[0].ToString("f2"));
            sbinfo.Append(" mm Y1: ");
            sbinfo.Append(CurPos[1].ToString("f2"));
            sbinfo.Append(" mm X2: ");
            sbinfo.Append(CurPos[2].ToString("f1"));
            sbinfo.Append(" mm Y2: ");
            sbinfo.Append(CurPos[3].ToString("f1"));
            sbinfo.Append(" mm Z2: ");
            sbinfo.Append(CurPos[4].ToString("f1"));
            sbinfo.Append(" mm R1: ");
            sbinfo.Append(CurPos[5].ToString("f2"));
            sbinfo.Append(" mm R2: ");
            sbinfo.Append(CurPos[6].ToString("f2"));
            //sbinfo.Append(" mm R3: ");
            //sbinfo.Append(CurPos[7].ToString("f2"));
            //sbinfo.Append(" mm R4: ");
            //sbinfo.Append(CurPos[8].ToString("f2"));
            return sbinfo.ToString();
        }

        #endregion
    }
}
