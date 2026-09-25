using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using CSSI9075;
using QA.Business.Interfaces;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Component.Motion.Robot9075
{
    #region 枚举
    public enum EN_SpeedType
    {
        Low,
        Mid,
        High,
        Work
    }
    public enum EN_RobotWorkCtrl
    {
        Pause = 0x01,
        Resume = 0x02,
        Stop = 0x03,
        SlowPause = 0x28,
    }
    public enum EN_AxisNum
    {
        X = 0,
        Y,
        Z,
        R,
        //SixAxis = 5,//第6轴，不能和其他轴混合使用
        //Y2 = 6,
        //S = 7,
    }
    //高低压力校准
    public enum EN_CerliPressF
    {
        Cerli_LowF = 0,
        Cerli_HighF
    }
    public enum EN_ROBOTAXIS_DIR
    {
        Axis_X_add = 0x04,
        Axis_X_sub,
        Axis_Y_add,
        Axis_Y_sub,
        Axis_Z_add,
        Axis_Z_sub,
        Axis_R_add = 0x0F,
        Axis_R_sub = 0x10,
    }
    public enum EN_AxisLimit
    {
        Axis_Ok = 0,
        Axis_1_left = 0x01,
        Axis_2_left,
        Axis_3_left,
        Axis_4_left,
        Axis_1_right = 0x10,
        Axis_2_right = 0x20,
        Axis_3_right = 0x30,
        Axis_4_right = 0x40,
    }
    public enum EN_CaliPressF
    {
        Cali_LowF = 0,
        Cali_HighF
    }
    public enum Motion_Status
    {
        Pause = 0x01,
        Resume = 0x02,
        Stop = 0x03,
        SlowPasue = 0x28
    }
    public enum EN_9036CaliStatus
    {
        Idle = 0,
        Cali_Ing,
        Cali_InitError,
        Cali_X_Error,
        Cali_Y_Error,
        Cali_Z_Error,
        Cali_Angle_Error,
        Cali_ResetBlock,
        Other,
    }
    #endregion

    public class Motion9075_Component : PropertyChangedBase, IM9075, IMotion
    {
        #region field
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        public Motion9075Param _motion9075Param;
        private IEventAggregator _eventAggregator;

        private bool _isUpdatePause = false;

        #endregion

        #region property        
        public virtual IParam Param { get; set; }
        //protected RobotRealStateInfo _robotRealStateInfo;

        public string ComponentName { get; set; } = "Motion9075";
        public bool IsConnected { get; set; }
        public string CompuInfo { get; internal set; }
        public bool IsRegister { get; internal set; }
        public bool Connected { get; internal set; }
        public float[] Pulseequ { get; internal set; } = new float[5];
        public bool IsServoAlarm { get; internal set; } = false;
        public bool IsOtherAlarm { get; internal set; } = false;
        public bool IsOtherError { get; internal set; } = false;
        public bool IsNeedReset { get; internal set; } = false;
        public string Version { get; internal set; } = string.Empty;
        public byte EInput0 { get; internal set; }
        public byte EInput1 { get; internal set; }
        public byte MInput { get; internal set; }
        public byte KInput { get; internal set; }
        public byte EOutput0 { get; internal set; }
        public byte EOutput1 { get; internal set; }
        public byte MOutput { get; internal set; }
        public float[] CurPress { get; internal set; } = new float[4];
        public bool[] AxisEnable { get; internal set; } = new bool[4];
        public float[] CurPos { get; internal set; } = new float[4];
        public int ErrorDetail { get; internal set; }
        public float FUpdateProcess { get; internal set; }
        public bool IsReset { get; set; }
        public bool IsSoldering { get; set; }

        //public event Action<string, RobotRealStateInfo> ActionRealStateInfo;

        #endregion

        public Motion9075_Component()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _motion9075Param = IoC.Get<Motion9075Param>();
            //_robotRealStateInfo = new RobotRealStateInfo();
        }

        public bool Initial(IParam param)
        {
            try
            {
                Param = _motion9075Param = (param as Motion9075Param).DeepCopy();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e + e.StackTrace);
                return false;
            }
            return true;
        }

        public bool Connect()
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_InitSerialPort(_motion9075Param.PortName, _motion9075Param.Baudrate);
                GetComputerInfo();
                if (rs)
                {
                    CheckRegister();
                    return ReadAxisEnbled() & ReadPulseEqu();
                }
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $"CSSI9075_InitSerialPort _motion9075Param.PortName {_motion9075Param.PortName} baudrate {_motion9075Param.Baudrate} rs {rs}");
                return rs;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"CSSI9075_InitSerialPort {ex.ToString()},{ex.StackTrace}");
            }

            return rs;
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
                    var delayTime = _motion9075Param.BUse ? 10 : 1000;
                    await Task.Delay(delayTime, _cancellationToken);
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        if (_motion9075Param.BUse)
                        {
                            _isUpdatePause = true;
                            _manualResetEvent.WaitOne();
                            _isUpdatePause = false;

                            ReadError();
                            ReadXYZR();
                            ReadInPort();
                            //EventStateInfo();

                            //ActionRealStateInfo?.Invoke(ComponentName, _robotRealStateInfo);
                            //_eventAggregator.Publish(_robotRealStateInfo, action => { Task.Run(action); });
                        }
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
        /// <summary>
        /// 获取当前Motion的信息
        /// </summary>
        /// <returns></returns>
        public string GetCurInfo()
        {
            StringBuilder sbinfo = new StringBuilder();
            sbinfo.Append(" X: ");
            sbinfo.Append(CurPos[0].ToString("f2"));
            sbinfo.Append(" mm Y: ");
            sbinfo.Append(CurPos[1].ToString("f2"));
            sbinfo.Append(" mm Z: ");
            sbinfo.Append(CurPos[2].ToString("f2"));
            sbinfo.Append(" mm R: ");
            sbinfo.Append(CurPos[3].ToString("f2"));
            sbinfo.Append(" mm F: ");
            sbinfo.Append(CurPress[2].ToString("f1"));
            sbinfo.Append(" N");
            return sbinfo.ToString();
        }

        public bool StartUpdate(string fileName)
        {
            ThreadPause();
            int timeout = 500;
            //等待Run()运行结束后再更新程序
            while (_isUpdatePause == false && timeout > 0)
            {
                Thread.Sleep(10);
                timeout--;
            }
            if (_isUpdatePause == false)
            {
                ThreadResume();
                return false;
            }
            if (!File.Exists(fileName))
            {
                ThreadResume();
                return false;
            }
            bool ret = false;
            try
            {
                //_isPaused = true;
                Func<string, bool> runUpdate = new Func<string, bool>(RunUpdate);
                ret = runUpdate.Invoke(fileName);
                Thread.Sleep(200);
                if (ret)
                    FUpdateProcess = 100.0f;
                //_isPaused = false;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace, En_Logout_Type.Exception);
                ret = false;
            }
            finally
            {
                ThreadResume();
            }
            return ret;
        }
        private bool RunUpdate(string filename)
        {
            var bRun = true;
            var bSuccess = false;
            var bContinue = false;
            ushort kb_num = 0;
            try
            {
                FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                BinaryReader reader = new BinaryReader(fs);
                var length = reader.BaseStream.Length;
                string tmp = filename.ToString().ToLower();
                if (tmp.EndsWith(".hdw"))
                {
                    byte[] data76 = new byte[76];
                    data76 = reader.ReadBytes(76);

                    if (!Update(data76))
                    {
                        bSuccess = false;
                    }
                    else
                    {
                        bSuccess = true;
                    }
                    bContinue = false;
                    bRun = false;
                }
                else
                {
                    if (!GetIsAtFirmwareUpdataMode(ref kb_num, ref bContinue, ref bSuccess))
                    {
                        if (!EnterFirmwareUpdateMode(ref kb_num))
                        {
                            bContinue = false;
                        }
                    }
                    if (GetIsAtFirmwareUpdataMode(ref kb_num, ref bContinue, ref bSuccess))
                    {
                        byte[] readdata = new byte[1024];
                        while (true)
                        {
                            if (GetIsAtFirmwareUpdataMode(ref kb_num, ref bContinue, ref bSuccess))
                            {
                                byte[] data1k = new byte[1024];
                                {//读第几个KB
                                    FileStream tmpfs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                                    BinaryReader tmpreadr = new BinaryReader(tmpfs);
                                    for (int i = 0; i <= kb_num; i++)
                                    {
                                        readdata = tmpreadr.ReadBytes(1024);
                                    }
                                    Buffer.BlockCopy(readdata, 0, data1k, 0, readdata.Length);
                                }
                                if (!SendFirmwareData1K(data1k, ref kb_num, ref bContinue, ref bSuccess))
                                {
                                    bRun = false; bContinue = false;
                                    break;
                                }
                            }
                            if (!bContinue)
                            {
                                bRun = false;
                                break;
                            }
                            else
                            {
                            }
                            Thread.Sleep(1);
                            FUpdateProcess = (float)(kb_num * 1024.0 / length * 100);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.StackTrace);
            }
            finally
            {
                //bRun = false;
                //_isPaused = false;
                ThreadResume();
            }
            return bSuccess;
        }
        private void ExitGloble()
        {
            try
            {
                CSSI9075Interface.CSSI9075_ExitGloble();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, _motion9075Param.ToString());
            }
            catch (Exception ex)
            {
                // throw ex;
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
        }
        public bool GetDoubleMarkOffset(CSSI_PointF[] srcmark, CSSI_PointF[] newmark, CSSI_PointF srcpoint, ref CSSI_PointF newpoint)
        {
            try
            {
                return CSSI9075Interface.CSSI9075_GetNewPointBy2Mark(srcmark, newmark, srcpoint, ref newpoint);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetNewPointBy2Mark ex {ex.ToString()},{ex.StackTrace}");
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <_motion9075Param name="timeout"></_motion9075Param>
        /// <_motion9075Param name="jumpout"></_motion9075Param>
        /// <returns></returns>
        public bool GetGorderCount(int timeout = 10, bool jumpout = true)
        {
            int time = timeout * 1000;
            ushort lesscount = 0;
            DateTime dt = DateTime.Now;
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"GetGCodeLessCount Set time = {time.ToString()} ms");
            if (!GetGCodeLessCount(ref lesscount))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "GetGCodeLessCount Error !");
                return false;
            }
            while (lesscount > 0)
            {
                if (DateTime.Now.Subtract(dt).TotalMilliseconds > time)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "GetGCodeLessCount timeout !");
                    return false;
                }
                if (jumpout)
                {
                    if (IsReset)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, "GetGCodeLessCount jumpout1 Status.IsReset = true !");
                        break;
                    }
                }
                if (!GetGCodeLessCount(ref lesscount))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "GetGCodeLessCount Error1 !");
                    return false;
                }
                Thread.Sleep(50);
            }
            if (jumpout)
            {
                if (IsReset)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, "GetGCodeLessCount jumpout2 Status.IsReset = true !");
                    return false;
                }
            }
            if (lesscount > 0)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetGCodeLessCount Error lesscount = {lesscount.ToString()} !");
                return false;
            }
            return true;
        }
        public bool ActionWheelClean(float[] xyzr, int delay)
        {
            if (!AbsoluteSingle(0, EN_AxisNum.Z, false, false) || CheckPause())
                return false;

            if (!AbsoluteXYZR(new float[4] { xyzr[0], xyzr[1], 0, xyzr[3] }, false, false) || CheckPause())
                return false;

            if (!AbsoluteSingle(xyzr[2], EN_AxisNum.Z, false, false) || CheckPause())
                return false;

            if (!WriteOutPort((byte)_motion9075Param.WheelClean, true, false))
                return false;

            if (!CSSI9075Interface.CSSI9075_Sleep(_motion9075Param.PortName, _motion9075Param.Addr, delay, false, false))
                return false;

            if (!WriteOutPort((byte)_motion9075Param.WheelClean, false, false))
                return false;

            if (!AbsoluteSingle(0, EN_AxisNum.Z, false, false))
                return false;
            bool ret = this.GetGorderCount(_motion9075Param.GOrderTimeout);
            return ret;
        }
        private bool CheckPause()
        {
            try
            {
                if (IsReset)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, "Status.IsReset = true");
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return false;
        }
        public bool ActionSolderCleanCraft(float[] xyzr, int clearcount, int delay, int tinlen, int tinretlen)
        {
            //Modify by luanl 2020/10/31 修改：TinWire_ManualCtrl，buseflag，true，false不起作用，通过GetGCodeLessCount判断出锡是否完成
            try
            {
                if (!AbsoluteSingle(0, EN_AxisNum.Z, false, false) || CheckPause())
                    return false;

                if (!AbsoluteXYZR(new float[4] { xyzr[0], xyzr[1], 0, xyzr[3] }, false, false) || CheckPause())
                    return false;

                if (!AbsoluteSingle(xyzr[2], EN_AxisNum.Z, false, false) || CheckPause())
                    return false;

                for (int i = 0; i < clearcount; i++)
                {
                    if (!WriteOutPort((byte)_motion9075Param.CleanBlow, true, false) || CheckPause())
                        return false;

                    if (!CSSI9075Interface.CSSI9075_Sleep(_motion9075Param.PortName, _motion9075Param.Addr, delay, false, false) || CheckPause())
                        return false;

                    if (!WriteOutPort((byte)_motion9075Param.CleanBlow, false, false) || CheckPause())
                        return false;

                    if (!CSSI9075Interface.CSSI9075_Sleep(_motion9075Param.PortName, _motion9075Param.Addr, delay, false, false) || CheckPause())
                        return false;
                }

                if (!AbsoluteSingle(0, EN_AxisNum.Z, false, false) || CheckPause())
                    return false;

                if (!CSSI9075Interface.CSSI9075_TinWire_ManualCtrl(_motion9075Param.PortName, _motion9075Param.Addr, tinlen, false))
                    return false;

                if (!CSSI9075Interface.CSSI9075_TinWire_ManualCtrl(_motion9075Param.PortName, _motion9075Param.Addr, -1 * tinretlen, false))
                    return false;

                bool ret = this.GetGorderCount(_motion9075Param.GOrderTimeout);
                return ret;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"ActionClearCraft prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }
        public bool JudgeGetGCodeLessCount(int timeout = 1000)
        {
            int time = timeout;
            ushort lesscount = 0;

            if (!GetGCodeLessCount(ref lesscount))
            {
                return false;
            }

            while (lesscount > 0 && time > 0)
            {
                if (!GetGCodeLessCount(ref lesscount))
                {
                    return false;
                }
                Thread.Sleep(1);
                time -= 1;
            }
            if ((lesscount > 0) || time <= 0)
            {
                return false;
            }
            return true;
        }

        public bool TinWire_ManualCtrl(long displuse)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_TinWire_ManualCtrl(_motion9075Param.PortName, _motion9075Param.Addr, displuse, false);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"TinWire_ManualCtrl displuse = {displuse.ToString()}");
                return rs;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"TinWire_ManualCtrl  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }
        public bool SetTinWireSpeed(float spd)//脉冲值
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetTinWireSpeed(_motion9075Param.PortName, _motion9075Param.Addr, spd);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"SetTinWireSpeed spdval = {spd.ToString()}");
                return rs;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetTinWireSpeed  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }
        public bool SetCount(int count)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetCounter(_motion9075Param.PortName, _motion9075Param.Addr, count);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"SetCounter {count.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetCounter  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return rs;
        }
        public bool GetCount(ref int count)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_GetCounter(_motion9075Param.PortName, _motion9075Param.Addr, ref count);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"GetCounter {count.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetCounter  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return rs;
        }
        public bool SetIronSolderStartorStop(bool start = true)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetIronSolderStart(_motion9075Param.PortName, _motion9075Param.Addr, start);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"SetIronSolderStartorStop {start.ToString()}");
                if (!rs)
                {
                    HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, $"SetIronSolderStartorStop Error {start.ToString()}");
                }
                return rs;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetIronSolderStart  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }

        public bool SolderDelay(int times, bool waitforend = false, bool buseflag = false)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_Sleep(_motion9075Param.PortName, _motion9075Param.Addr, times, waitforend, buseflag);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_Sleep value = {times.ToString()}");
                return rs;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SolderDelay  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }

        public bool SetIronDoSoldering(CSSI_IronSolderParam ironsolderparam, bool waitforend = false, bool buseflag = false)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_DoIronSolder(_motion9075Param.PortName, _motion9075Param.Addr, ironsolderparam, waitforend, buseflag);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"SetIronDoSoldering ");
                if (!rs)
                {
                    HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, $"SetIronDoSoldering Error ");
                }
                return rs;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetIronDoSoldering  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }

        public bool GetIronUseCount(ref int usecount)
        {
            try
            {
                return CSSI9075Interface.CSSI9075_GetIronUseCount(_motion9075Param.PortName, _motion9075Param.Addr, ref usecount);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetIronUseCount  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }
        public bool ClearIronUseCount(ref int usecount)
        {
            try
            {
                return CSSI9075Interface.CSSI9075_ClearIronUseCount(_motion9075Param.PortName, _motion9075Param.Addr);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"ClearIronUseCount  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }
        /// <summary>
        /// 设置焊台温度
        /// </summary>
        /// <_motion9075Param name="setT">设置温度</_motion9075Param>
        /// <returns></returns>
        public bool SetIronTemperature(ushort setT)
        {
            try
            {
                return CSSI9075Interface.CSSI9075_SetIronTemperatureStatus(_motion9075Param.PortName, _motion9075Param.Addr, setT);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetIronTemperature  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <_motion9075Param name="getcurT">读取当前温度</_motion9075Param>
        /// <_motion9075Param name="getsetT">读取设置温度</_motion9075Param>
        /// <returns></returns>
        public bool GetIronTemperature(ref ushort getcurT, ref ushort getsetT)
        {
            try
            {
                return CSSI9075Interface.CSSI9075_ReadIronTemperatureStatus(_motion9075Param.PortName, _motion9075Param.Addr, ref getcurT, ref getsetT);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetIronTemperature  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
        }
        /// <summary>
        /// 判断温度设置和实际温度是否在误差之内
        /// </summary>
        /// <_motion9075Param name="offsettmp"></_motion9075Param>
        /// <_motion9075Param name="isAlarm"></_motion9075Param>
        /// <returns></returns>
        public bool JudgeTempRangeIn(ushort offsettmp, ref bool isAlarm)
        {
            ushort getcurT = 0;
            ushort getsetT = 0;
            bool ret = false;

            try
            {
                ret = CSSI9075Interface.CSSI9075_ReadIronTemperatureStatus(_motion9075Param.PortName, _motion9075Param.Addr, ref getcurT, ref getsetT);
                if (ret == false)
                {
                    isAlarm = false;
                    return false;
                }
                if (Math.Abs(getcurT - getsetT) <= offsettmp)
                {
                    isAlarm = true;
                }
                else
                    isAlarm = false;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"JudgeTempRangeIn  ex {ex.ToString()},{ex.StackTrace}");
                return false;
            }
            return true;
        }

        public bool ReadOutPort()
        {
            try
            {
                byte eoutput0 = 0, e0utput1 = 0, moutput = 0;
                if (CSSI9075Interface.CSSI9075_ReadOutPutPortStatus(_motion9075Param.PortName, _motion9075Param.Addr, ref eoutput0, ref e0utput1, ref moutput))
                {
                    EOutput0 = eoutput0;
                    EOutput1 = e0utput1;
                    MOutput = moutput;
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"ReadOutPort prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ex {ex.ToString()},{ex.StackTrace}");
            }
            return false;
        }

        public bool ReadInPort()
        {
            try
            {
                byte einput0 = 0, einput1 = 0, minput = 0, kinput = 0;
                if (CSSI9075Interface.CSSI9075_ReadInPutPortStatus(_motion9075Param.PortName, _motion9075Param.Addr, ref einput0, ref einput1,
                    ref minput, ref kinput))
                {
                    EInput0 = einput0;
                    EInput1 = einput1;
                    MInput = minput;
                    KInput = kinput;
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error,
                    $"ReadInPort prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ex {ex.ToString()},{ex.StackTrace}");
            }

            return false;
        }

        public bool GetFindPressPID(ref ushort p, ref ushort i, ref ushort d)//500,3,0
        {
            int[] data = new int[1];
            if (!CSSI9075Interface.CSSI9075_GetSysData(_motion9075Param.PortName, _motion9075Param.Addr, 0x4D + 2 * 2, data))
                return false;
            p = (ushort)data[0];
            i = (ushort)(data[0] >> 16);
            if (!CSSI9075Interface.CSSI9075_GetSysData(_motion9075Param.PortName, _motion9075Param.Addr, 0x49 + 2 * 2, data))
                return false;
            d = (ushort)data[0];
            return true;
        }

        public bool SetFindPressPID(ushort p, ushort i, ushort d)
        {
            return CSSI9075Interface.CSSI9075_SetFindPressPID(_motion9075Param.PortName, _motion9075Param.Addr, (byte)EN_AxisNum.Z, p, i, d);
        }
        public bool ReadCurF(ref float val)
        {
            try
            {
                float[] curf = new float[4];
                if (CSSI9075Interface.CSSI9075_ReadCurF(_motion9075Param.PortName, _motion9075Param.Addr, curf))
                {
                    for (int i = 0; i < curf.Length; i++)
                    {
                        CurPress[i] = curf[i];
                    }
                    val = CurPress[2];
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"ReadPressF prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ex {ex.ToString()},{ex.StackTrace}");
            }
            return false;
        }
        public bool ReadCurF()
        {
            try
            {
                float[] curf = new float[4];
                if (CSSI9075Interface.CSSI9075_ReadCurF(_motion9075Param.PortName, _motion9075Param.Addr, curf))
                {
                    for (int i = 0; i < curf.Length; i++)
                        CurPress[i] = curf[i];//2020/10/09库里面输出的直接就是N单位
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"ReadPressF prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ex {ex.ToString()},{ex.StackTrace}");
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <_motion9075Param name="portnum"></_motion9075Param>
        /// <_motion9075Param name="openclose"></_motion9075Param>
        /// <returns></returns>
        public bool WriteOutPort(byte portnum, bool openclose, bool isdirect = false)
        {
            bool rs = false;
            try
            {
                bool isex = portnum < 16;
                if (isdirect)
                {
                    rs = CSSI9075Interface.CSSI9075_WriteOutPutPortDirect(_motion9075Param.PortName, _motion9075Param.Addr, isex, isex ? portnum : (byte)(portnum - 16), openclose);
                }
                else
                    rs = CSSI9075Interface.CSSI9075_WriteOutPutPort(_motion9075Param.PortName, _motion9075Param.Addr, isex, isex ? portnum : (byte)(portnum - 16), openclose);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $"WriteOutput prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} bExt {isex.ToString()} portnum {portnum} openclose {openclose.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"WriteOutput prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ex {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }

        private bool GetIsAtFirmwareUpdataMode(ref ushort kb_num, ref bool bContinue, ref bool bSuccess)
        {
            bool ret = false;
            try
            {
                ret = CSSI9075Interface.CSSI9075_GetIsAtFirmwareUpdataMode(_motion9075Param.PortName, _motion9075Param.Addr, ref kb_num, ref bContinue, ref bSuccess);
                if (!ret)
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, " GetIsAtFirmwareUpdataMode() ret false");
            }
            catch (Exception ex)
            {
                // throw ex;
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return ret;
        }
        public bool Update(byte[] buffer)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_UpdateConfig(_motion9075Param.PortName, _motion9075Param.Addr, buffer);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"CSSI9075_UpdateConfig prortname {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ex {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }
        public bool IsAtUpdateMode(ref ushort kb_num, ref bool bContinue, ref bool bSuccess)
        {
            bool rs = false;

            try
            {
                rs = CSSI9075Interface.CSSI9075_GetIsAtFirmwareUpdataMode(_motion9075Param.PortName, _motion9075Param.Addr, ref kb_num, ref bContinue, ref bSuccess);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"CSSI9075_GetIsAtFirmwareUpdataMode _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }
        public bool SendFirmwareData1K(byte[] data1k, ref ushort kb_num, ref bool bContinue, ref bool bSuccess)
        {
            bool rs = false;

            try
            {
                rs = CSSI9075Interface.CSSI9075_SendFirmwareData1K(_motion9075Param.PortName, _motion9075Param.Addr, data1k, ref kb_num, ref bContinue, ref bSuccess);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SendFirmwareData1K _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} {ex.ToString()},{ex.StackTrace}");
            }

            return rs;
        }
        public bool EnterFirmwareUpdateMode(ref ushort kb_num)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_EnterFirmwareUpdateMode(_motion9075Param.PortName, _motion9075Param.Addr, ref kb_num);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"EnterFirmwareUpdateMode _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }
        public static string[] GetSerialPortName()
        {
            try
            {
                return CSSI9075Interface.CSSI9075_GetSerialPortNames();
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetSerialPortName {ex.ToString()},{ex.StackTrace}");
            }
            return null;
        }


        public bool SetCustomSingleSpeed(EN_AxisNum axis, float[] spd)
        {
            bool rs = false;

            try
            {
                float[] speedpluse = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    speedpluse[i] = spd[i] * Pulseequ[(int)axis];
                }
                rs = CSSI9075Interface.CSSI9075_SetSpeedAxis(_motion9075Param.PortName, _motion9075Param.Addr, (byte)axis, speedpluse);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $" _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} Axis {axis.ToString()} Speed {NLogTrace.GetFloatArrayString(spd)} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetCustomSingleSpeed _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }
        public bool SetSpeed(EN_AxisNum axis, EN_SpeedType speedtype)
        {
            bool rs = false;
            try
            {
                float[] speed = _motion9075Param.GetSpeed(axis, speedtype);
                float[] speedpluse = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    //if(axis == EN_AxisNum.SixAxis)
                    //{
                    //    speedpluse[i] = Status.Pulseequ[(int)axis-1] * speed[i];
                    //}
                    //else
                    {
                        speedpluse[i] = Pulseequ[(int)axis] * speed[i];
                    }
                }
                rs = CSSI9075Interface.CSSI9075_SetSpeedAxis(_motion9075Param.PortName, _motion9075Param.Addr, (byte)axis, speedpluse);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $" _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} Axis {axis.ToString()} Speed {NLogTrace.GetFloatArrayString(speed)} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSpeed _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return rs;

        }
        public bool SetSpeedAll(EN_SpeedType speedtype)
        {
            bool ret = true;
            try
            {
                var axisNum = Enum.GetNames(typeof(EN_AxisNum));
                for (int i = 0; i < axisNum.Length; i++)
                {
                    if (!SetSpeed((EN_AxisNum)Enum.Parse(typeof(EN_AxisNum), axisNum[i]), speedtype))
                        ret = false;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetSpeedAll _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return ret;
        }
        public bool ReadError()
        {
            bool rs = false;
            int errordetail = 0;
            bool bServAlarm = false;
            bool bOtherError = false;
            bool bNeedReset = false;
            rs = CSSI9075Interface.CSSI9075_GetServAlarm(_motion9075Param.PortName, _motion9075Param.Addr, ref bServAlarm, ref bOtherError, ref bNeedReset) && CSSI9075Interface.CSSI9075_GetOtherAlarmDetail(_motion9075Param.PortName, _motion9075Param.Addr, ref errordetail);
            if (rs)
            {
                if ((!IsServoAlarm) && bServAlarm)
                {
                    //ServoAlarm?.BeginInvoke(null, null);
                }
                if (errordetail != ErrorDetail)
                {
                    //OtherAlarm?.BeginInvoke(Status.GetCurErrInfo(), null, null);
                }
                if ((!IsNeedReset) && bNeedReset)
                {
                    //NeedReset?.BeginInvoke(null, null);
                }
                if ((!IsOtherError) && bOtherError)
                {
                    //OtherError?.BeginInvoke(null, null);
                }
                IsOtherError = bOtherError;
                IsNeedReset = bNeedReset;
                IsServoAlarm = bServAlarm;
                ErrorDetail = errordetail;

            }
            return rs;
        }
        public bool CheckTmpAlarm1()
        {
            if (!_motion9075Param.BuseTempAlarmIO) return false;
            return (EInput1 & (1 << 3)) != 0 ? true : false;
        }
        public bool CheckTmpAlarm2()
        {
            if (!_motion9075Param.BuseTempAlarmIO) return false;
            return (EInput1 & (1 << 4)) != 0 ? true : false;
        }
        public bool CheckTmpAlarm3()
        {
            if (!_motion9075Param.BuseTempAlarmIO) return false;
            return (EInput1 & (1 << 5)) != 0 ? true : false;
        }

        public bool CheckAllAlarm()
        {
            if (CheckTmpAlarm1() || CheckTmpAlarm2() || CheckTmpAlarm3())
            {
                return true;
            }
            return false;
        }
        public bool SetInterpolationSpeed(float[] spd)
        {
            bool rs = false;
            try
            {
                float[] speedpluse = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    speedpluse[i] = spd[i] * Pulseequ[(int)EN_AxisNum.X];
                }
                rs = CSSI9075Interface.CSSI9075_SetInterpolationSpeed(_motion9075Param.PortName, _motion9075Param.Addr, speedpluse);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $" _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} Speed {NLogTrace.GetFloatArrayString(spd)} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetInterpolationSpeed _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }

        public bool SetInterpolationMode(bool mode)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetInterpolationMode(_motion9075Param.PortName, _motion9075Param.Addr, mode);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $" _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetInterpolationMode _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }
        public bool SetStartMotionBuffer()
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_StartMotionBuffer(_motion9075Param.PortName, _motion9075Param.Addr);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $" _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StartMotionBuffer _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }
        public bool SetEndMotionBuffer()
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_EndMotionBuffer(_motion9075Param.PortName, _motion9075Param.Addr);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $" _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"EndMotionBuffer _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }
        public bool SetCloseAheadDis(float dis)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetCloseAheadDis(_motion9075Param.PortName, _motion9075Param.Addr, dis);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $" _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetCloseAheadDis _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }
        #region 9036Calib
        public bool Start9036Calib()
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_Start9036Cali(_motion9075Param.PortName, _motion9075Param.Addr);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_Start9036Cali _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Start9036Calib _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }

            return rs;
        }

        public bool Set9036CaliParam(bool xreverse, bool yreverse, bool c9063p)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_Set9036CaliParam(_motion9075Param.PortName, _motion9075Param.Addr, xreverse, yreverse, c9063p);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_Set9036CaliParam _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()}");

            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Set9036CaliParam _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }

            return rs;
        }

        public bool Get9036CaliResult(int[] xyz)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_Get9036CaliResult(_motion9075Param.PortName, _motion9075Param.Addr, xyz);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_Get9036CaliResult _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()} x: {xyz[0].ToString("f2")}  y: {xyz[1].ToString("f2")} z: {xyz[2].ToString("f2")}");

            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Get9036CaliResult _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }

            return rs;
        }

        public bool Get9036CalibState(ref byte calistatus)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_Get9036CaliStatus(_motion9075Param.PortName, _motion9075Param.Addr, ref calistatus);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_Get9036CaliResult _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()} calistatus: {calistatus.ToString()} ");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Get9036CaliStatus _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }
        #endregion 9036Calib

        public bool SetFindPressMode(CSSI_FindPressParam param)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetFindPressMode(_motion9075Param.PortName, _motion9075Param.Addr, param);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_SetFindPressMode _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()} enable {NLogTrace.GetBoolArrayString(AxisEnable)}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"SetFindPressMode _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");

            }
            return rs;
        }

        public bool CheckMoveEnd()
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_CheckMoveEnd(_motion9075Param.PortName, _motion9075Param.Addr);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_CheckMoveEnd _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"CheckMoveEnd _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }

        public bool StartMotionBuffer()
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_StartMotionBuffer(_motion9075Param.PortName, _motion9075Param.Addr);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $"StartMotionBuffer _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StartMotionBuffer _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }

        public bool EndMotionBuffer(bool lastreflash = true)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_EndMotionBuffer(_motion9075Param.PortName, _motion9075Param.Addr, lastreflash);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, $"EndMotionBuffer _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"EndMotionBuffer _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }

        public bool GetGCodeLessCount(ref ushort lesscount)
        {
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_GetGCodeLessCount(_motion9075Param.PortName, _motion9075Param.Addr, ref lesscount);
                //NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_GetGCodeLessCount _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {lesscount.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"GetGCodeLessCount _motion9075Param.PortName {_motion9075Param.PortName}  {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }

        public bool ReadAxisEnbled()
        {
            bool rs = false;
            rs = CSSI9075Interface.CSSI9075_ReadAxisEnable(_motion9075Param.PortName, _motion9075Param.Addr, AxisEnable);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_ReadAxisEnable _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()} enable {NLogTrace.GetBoolArrayString(AxisEnable)}");
            return rs;
        }
        public bool SetAxisEnabled(bool[] enable)
        {
            bool rs = false;
            rs = CSSI9075Interface.CSSI9075_WriteAxisEnable(_motion9075Param.PortName, _motion9075Param.Addr, enable);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"SetAxisEnabled _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()} enable {NLogTrace.GetBoolArrayString(enable)}");
            ReadAxisEnbled();
            return rs;
        }
        public bool AbsoluteSingle(float dis, EN_AxisNum axis, bool waitforend = false, bool bUseFlag = false)
        {
            bool rs = false;
            float disPulse = 0;
            //if (axis == EN_AxisNum.SixAxis)
            //{
            //    disPulse = Status.Pulseequ[(byte)axis-1] * dis;
            //}else
            disPulse = Pulseequ[(byte)axis] * dis;
            try
            {
                rs = CSSI9075Interface.CSSI9075_MoveAbsoluteToPointSingleAxis(_motion9075Param.PortName, _motion9075Param.Addr, disPulse, (byte)axis, waitforend, bUseFlag);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"{axis.ToString()} Move {dis.ToString("f2")}");
                if (!rs)
                {
                    HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, $"{axis.ToString()} 移动错误 {dis.ToString("f2")}");
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"CSSI9075_AbsoluteSingle {ex.ToString()},{ex.StackTrace}");
            }
            return rs;
        }
        public bool AbsoluteXY(float[] xy, bool blnterpolation = false, bool useflag = false, bool waitforend = false)
        {
            bool rs = false;
            var xypluse = new float[]
            {
                xy[0]*Pulseequ[0],
                xy[1]*Pulseequ[1]
            };
            rs = CSSI9075Interface.CSSI9075_MoveAbsoluteToPointXY(_motion9075Param.PortName, _motion9075Param.Addr, xypluse, waitforend, blnterpolation, useflag);
            NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"XY Move {NLogTrace.GetFloatArrayString(xy)}");
            if (!rs)
            {
                HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, $"XY 移动错误 {NLogTrace.GetFloatArrayString(xy)}");
            }
            return rs;
        }
        public bool AbsoluteXYZ(float[] xyz, bool blnterpolation = false, bool useflag = false, bool waitforend = false)
        {
            bool rs = false;
            var xyzpluse = new float[]
            {
                xyz[0]*Pulseequ[0],
                xyz[1]*Pulseequ[1],
                xyz[2]*Pulseequ[2]
            };
            rs = CSSI9075Interface.CSSI9075_MoveAbsoluteToPointXYZ(_motion9075Param.PortName, _motion9075Param.Addr, xyzpluse, waitforend, blnterpolation, useflag);
            NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"XYZ Move {NLogTrace.GetFloatArrayString(xyz)}");
            if (!rs)
            {
                HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, $"XYZ 移动错误 {NLogTrace.GetFloatArrayString(xyz)}");
            }

            return rs;
        }
        public bool AbsoluteThreeAxis(float[] xyzr, bool blnterpolation = false, bool useflag = false, bool waitforend = false)
        {
            bool rs = false;
            byte[] axisnum = { 0, 1, 3 };
            var xyrpluse = new float[]
            {
                xyzr[0]*Pulseequ[0],
                xyzr[1]*Pulseequ[1],
                xyzr[2]*Pulseequ[3]
            };
            rs = CSSI9075Interface.CSSI9075_MoveAbsoluteToPointThreeAxis(_motion9075Param.PortName, _motion9075Param.Addr, axisnum, xyrpluse, waitforend, blnterpolation, useflag);
            NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"XYR Move {NLogTrace.GetFloatArrayString(xyzr)}");
            if (!rs)
            {
                HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, $"XYR 移动错误 {NLogTrace.GetFloatArrayString(xyzr)}");
            }

            return rs;
        }
        public bool Absolute3Axis(EN_AxisNum[] axiss, float[] positions, bool blnterpolation = false, bool useflag = false, bool waitforend = false)
        {
            bool rs = false;
            try
            {
                var xyzpluse = new float[]
                {
                    positions[0] * Pulseequ[(int) axiss[0]],
                    positions[1] * Pulseequ[(int) axiss[1]],
                    positions[2] * Pulseequ[(int) axiss[2]]

                };
                rs = CSSI9075Interface.CSSI9075_MoveAbsoluteToPointThreeAxis(_motion9075Param.PortName, _motion9075Param.Addr,
                    new byte[3] { (byte)axiss[0], (byte)axiss[1], (byte)axiss[2] }, xyzpluse, waitforend,
                    blnterpolation, useflag);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError,
                    $"Absolute3Axis Move {NLogTrace.GetFloatArrayString(positions)}");

                if (!rs)
                {
                    HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0,
                        $"Absolute3Axis 移动错误 {NLogTrace.GetFloatArrayString(positions)}");

                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return rs;
        }
        public bool AbsoluteXYZR(float[] xyzr, bool blnterpolation = false, bool useflag = false, bool waitforend = false)
        {
            bool rs = false;
            var xyzrpluse = new float[]
            {
                xyzr[0]*Pulseequ[0],
                xyzr[1]*Pulseequ[1],
                xyzr[2]*Pulseequ[2],
                xyzr[3]*Pulseequ[3]
            };
            rs = CSSI9075Interface.CSSI9075_MoveAbsoluteToPointXYZR(_motion9075Param.PortName, _motion9075Param.Addr, xyzrpluse, waitforend, blnterpolation, useflag);
            NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Sucess : EN_WARN_LEVEL.CriticalError, $"XYZR Move {NLogTrace.GetFloatArrayString(xyzr)}");
            if (!rs)
            {
                HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, $"XYZR 移动错误 {NLogTrace.GetFloatArrayString(xyzr)}");
            }

            return rs;
        }
        public bool Jog(EN_ROBOTAXIS_DIR dir, bool runorstop)
        {
            bool rs = false;
            rs = CSSI9075Interface.CSSI9075_JogAxis(_motion9075Param.PortName, _motion9075Param.Addr, (byte)dir, runorstop);
            return rs;
        }
        public bool MoveRelativeToPointSingleAxis(float dis, EN_AxisNum axis, bool waitforend = false, bool bUseFlag = false)
        {
            bool rs = false;
            var disPulse = Pulseequ[(byte)axis] * dis;
            rs = CSSI9075Interface.CSSI9075_MoveRelativeToPointSingleAxis(_motion9075Param.PortName, _motion9075Param.Addr, disPulse, (byte)axis, waitforend, bUseFlag);
            return rs;
        }

        public bool JogDis(EN_AxisNum axis, float jogdis)
        {
            bool ret = false;
            float[] curpos = CurPos;
            if (ReadXYZR())
            {
                ret = AbsoluteSingle(curpos[(byte)axis] + jogdis, axis, false, false);
            }
            return ret;
        }
        public bool ReadXYZR()
        {
            bool rs = false;
            float[] xyzrpulse = new float[4];
            if (CSSI9075Interface.CSSI9075_ReadXYZR(_motion9075Param.PortName, _motion9075Param.Addr, xyzrpulse))
            {
                CurPos[0] = xyzrpulse[0] / Pulseequ[0];
                CurPos[1] = xyzrpulse[1] / Pulseequ[1];
                CurPos[2] = xyzrpulse[2] / Pulseequ[2];
                CurPos[3] = xyzrpulse[3] / Pulseequ[3];
                rs = true;
            }
            //Status.CurPos = new float[4]
            //{
            //   (float) (random.Next(10000)/100.0),
            //   (float) (random.Next(10000)/100.0),
            //   (float) (random.Next(10000)/100.0),
            //   (float) (random.Next(10000)/100.0),
            //};
            Connected = rs;
            return rs;
        }
        public string GetComputerInfo()
        {
            string rsStr = "";
            rsStr = CSSI9075Interface.CSSI9075_GetComputerInfo();
            CompuInfo = rsStr;
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_GetComputerInfo ,_motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} Info {rsStr.ToString()} ");
            return rsStr;
        }
        public bool Register9075(string registercode)
        {
            bool rs = false;
            rs = CSSI9075Interface.CSSI9075_InitGloble(registercode/*, _motion9075Param.MoveBitFlag, _motion9075Param.MoveBufferFlag*/);
            IsRegister = rs;
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_InitGloble ,_motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} ret {rs.ToString()}  CSSI9075_InitGloble {registercode}");
            return rs;
        }
        public void CheckRegister()
        {
            IsRegister = Register9075(string.Empty);
        }
        public bool SetAxisEnable(bool[] SetAxisEnable)
        {
            bool rs;
            rs = CSSI9075Interface.CSSI9075_WriteAxisEnable(_motion9075Param.PortName, _motion9075Param.Addr, SetAxisEnable);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_WriteAxisEnable , _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()}  setAxisActive {NLogTrace.GetBoolArrayString(SetAxisEnable)}");
            return rs;
        }
        public bool ResetAll(bool bslow = false, bool waitforend = false)
        {
            bool rs;
            rs = CSSI9075Interface.CSSI9075_Reset(_motion9075Param.PortName, _motion9075Param.Addr, bslow, waitforend);
            if (!rs)
                //HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.Robot, 0, 0, _motion9075Param.GetErrInfo(0));
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_ResetAll ,_motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")}   rs {rs.ToString()} ");
            return rs;
        }
        public bool ResetSingle(EN_AxisNum axis, bool bslow = true, bool waitforend = false)
        {
            bool rs;
            rs = CSSI9075Interface.CSSI9075_ResetAxis(_motion9075Param.PortName, _motion9075Param.Addr, (byte)axis, bslow, waitforend);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"ResetSingele , _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} Axis {axis.ToString()} bslow {bslow}  rs {rs.ToString()} axis {axis}");
            return rs;
        }
        public bool ReadPulseEqu()
        {
            bool rs;
            rs = CSSI9075Interface.CSSI9075_GetPluse(_motion9075Param.PortName, _motion9075Param.Addr, Pulseequ);

            if (rs == true)
            {
                rs = CSSI9075Interface.CSSI9075_GetSAxisPluse(_motion9075Param.PortName, _motion9075Param.Addr, ref Pulseequ[4]);
            }
            else
                return rs;

            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_Getpulse , _motion9075Param.PortName{_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} rs {rs.ToString()} pulse {NLogTrace.GetFloatArrayString(Pulseequ)}");
            return rs;
        }
        public bool WorkCtrl(EN_RobotWorkCtrl ctrl)
        {
            bool ret = false;
            ret = CSSI9075Interface.CSSI9075_WorkCtrl(_motion9075Param.PortName, _motion9075Param.Addr, (byte)ctrl);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_WorkCtrl _motion9075Param.PortName {_motion9075Param.PortName} _motion9075Param.Addr {_motion9075Param.Addr.ToString("X2")} Ctrl {ctrl.ToString()} ret {ret.ToString()} ");
            return ret;
        }
        public string GetVersion()
        {
            string version = "";
            bool rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_GetRobotVersion(_motion9075Param.PortName, _motion9075Param.Addr, ref version);
                if (rs)
                    Version = version;
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"CSSI9075_GetRobotVersion , version {version} rs {rs.ToString()}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"CSSI9075_GetRobotVersion {ex.ToString()},{ex.StackTrace}");
            }
            return version;
        }
        /// <summary>
        /// 等待条件
        /// </summary>
        /// <_motion9075Param name="IOPort"></_motion9075Param>
        /// <_motion9075Param name="highlevel"></_motion9075Param>
        /// <_motion9075Param name="timeout"></_motion9075Param>
        /// <returns></returns>
        public bool WaitCondition(string IOPort, ushort timeout, bool highlevel = true)
        {
            var rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_WaitCondition(_motion9075Param.PortName, _motion9075Param.Addr, IOPort, (byte)(highlevel ? 0 : 1), timeout);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, _motion9075Param.ToString());
            }
            catch (Exception ex)
            {
                // throw ex;
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return rs;
        }
        public bool GetGCodeBufferSize(ref ushort size)
        {
            var rs = false;

            try
            {
                rs = CSSI9075Interface.CSSI9075_GetGCodeBufferSize(_motion9075Param.PortName, _motion9075Param.Addr, ref size);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, _motion9075Param.ToString() + " Size " + size.ToString());
            }
            catch (Exception ex)
            {
                // throw ex;
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return rs;
        }
        public bool SetFlag(byte flagnum, byte value)
        {
            var rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetFlag(_motion9075Param.PortName, _motion9075Param.Addr, flagnum, value);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, _motion9075Param.ToString());
            }
            catch (Exception ex)
            {
                // throw ex;
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return rs;
        }
        public bool CheckFlag(byte flagnum)
        {
            var rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_CheckFlag(_motion9075Param.PortName, _motion9075Param.Addr, flagnum);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, _motion9075Param.ToString());
            }
            catch (Exception ex)
            {
                // throw ex;
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return rs;
        }
        /// <summary>
        /// 设置高低压力校准
        /// </summary>
        /// <returns></returns>
        public bool SetHLPressCalib(EN_CerliPressF hl, float val)
        {
            return CSSI9075Interface.CSSI9075_SetCaliPressF(_motion9075Param.PortName, _motion9075Param.Addr, (byte)EN_AxisNum.Z, (byte)hl, val);
        }

        public bool SetFindPress(CSSI_FindPressParam param)
        {
            var rs = false;
            try
            {
                rs = CSSI9075Interface.CSSI9075_SetFindPressMode(_motion9075Param.PortName, _motion9075Param.Addr, param);
                NLogTrace.LogOut(rs ? EN_WARN_LEVEL.Info : EN_WARN_LEVEL.Error, _motion9075Param.ToString());
            }
            catch (Exception ex)
            {
                // throw ex;
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return rs;
        }
        // GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos);
        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> infos)
        {
            if (!Connected)
            {
                infos.Add(new AlarmInfoModel()
                {
                    Datetime = DateTime.Now,
                    Index = 0,
                    ErrorCode = 0,
                    AlarmMsg = "未连接",
                    AlarmModule = EN_WarnModules.Robot,
                });
            }
            if (IsServoAlarm)
            {
                infos.Add(new AlarmInfoModel()
                {
                    Datetime = DateTime.Now,
                    Index = 0,
                    ErrorCode = 0,
                    AlarmMsg = "伺服报警",
                    AlarmModule = EN_WarnModules.Robot,
                });
            }
            if (IsNeedReset)
            {
                infos.Add(new AlarmInfoModel()
                {
                    Datetime = DateTime.Now,
                    Index = 0,
                    ErrorCode = 0,
                    AlarmMsg = "需要复位",
                    AlarmModule = EN_WarnModules.Robot,
                });
            }
            if (!IsRegister)
            {
                infos.Add(new AlarmInfoModel()
                {
                    Datetime = DateTime.Now,
                    Index = 0,
                    ErrorCode = 0,
                    AlarmMsg = "未注册",
                    AlarmModule = EN_WarnModules.Robot,
                });
            }
        }
    }
}
