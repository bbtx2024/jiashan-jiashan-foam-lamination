/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-24
 * 说明：（视觉功能逻辑）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Caliburn.Micro;
using HandyControl.Data;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.Alarm;
using QA.Business.Station;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls.ParamEnum;
using QA_Infrastructure.NLogOut;
using ICamera = QA.Business.Interfaces.ICamera;
using MessageBox = HandyControl.Controls.MessageBox;
using StepStatus = QA.Business.Steps.StepStatus;

namespace QA.Business.Component.Camera
{
    public enum EnCameraFuncCodeType
    {
        MulMark,
        MulRecheck,
    }

    public class Camera_Component : TcpCtrls, ICamera
    {
        private CameraParam _cameraParam = null;
        private SocketParam _socketParam = new SocketParam();
        private CacheParamManager _cacheParamManager;
        private CancellationTokenSource _cancellationTokenSource;
        private MotionGoogol_Component _mGoogol_Component;
        private PLC_Component _plc_Component;
        private CancellationToken _cancellationToken;
        private ParamManager _paramManager;
        public FeederCDDMode feederCDDMode = new FeederCDDMode();
        public DownCDDMode downCDDMode = new DownCDDMode();
        public UpCDDMode upCDDMode = new UpCDDMode();
        public GetFitPosMode getFitPosMode = new GetFitPosMode();

        public IParam Param { get; set; }
        public string ComponentName { get; set; } = "Camera";
        public new bool IsConnected => base.IsConnected;
        public bool IsTLMOk;
        public float[] Partxya;
        public string[] data;
        public List<List<float>> xyafloats;
        public bool IsCDDOk;
        public int TapeNum = 0;
        #region FA11-004
        /// <summary>
        /// 暂存载具SN即文件夹名称
        /// </summary>
        public const string carrierSN = "Production";
        /// <summary>
        /// Feeder拍照暂存Sn即文件夹名称
        /// </summary>
        public string[] FeederCddSn = new string[2];
        /// <summary>
        /// 下视觉拍照暂存Sn即文件夹名称
        /// </summary>
        public string[] DownCddSn = new string[2];
        /// <summary>
        /// 上视觉拍照暂存Sn即文件夹名称
        /// </summary>
        public string UpCddSn = "";
        #endregion

        public Camera_Component()
        {
            _cameraParam = IoC.Get<CameraParam>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _mGoogol_Component = new MotionGoogol_Component();
            _plc_Component = new PLC_Component();
            _paramManager = IoC.Get<ParamManager>();
        }

        public bool Initial(IParam param)
        {
            try
            {
                Param = _cameraParam = param as CameraParam;
                if (_cameraParam != null)
                {
                    _socketParam.Ip = _cameraParam.IP;
                    _socketParam.Port = _cameraParam.Port;
                    _socketParam.SendTimeout = _cameraParam.SendTimeout;
                    _socketParam.ReceiveTimeout = _cameraParam.ReceiveTimeout;
                    _socketParam.SendBuffSize = 8192;
                    _socketParam.ReceiveBuffSize = 8192;
                    base.SetParam(_socketParam);
                }
                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, "_cameraParam == null", En_Logout_Type.Exception);
                    return false;
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e + e.StackTrace, En_Logout_Type.Exception);
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
                            Connect(_socketParam);
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
                //base.Close();
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
            if (_cameraParam.BUse)
            {
                if (!IsConnected)
                {
                    var count = alarmInfos.Count;
                    alarmInfos.Add(new AlarmInfoModel()
                    {
                        AlarmLevel = EN_WARN_LEVEL.Error,
                        AlarmModule = EN_WarnModules.Camera,
                        AlarmMsg = "无连接",
                        Datetime = DateTime.Now,
                        ErrorCode = 0,
                        Index = count
                    });
                }
            }
        }

        #region FA11-004

        #region 标定协议
        /// <summary>
        /// 训练吸嘴
        /// </summary>
        /// <param name="nozzleId"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool StarTTN(NozzleId nozzleId, float x, float y, float r, int timeout = -1)
        {
            if (!Param.BUse) return true;
            string order = string.Empty;
            if (nozzleId == NozzleId.一号吸嘴)
            {
                order = "TTN1,";
            }
            else
            {
                order = "TTN2,";
            }
            order += $"{x},{y},{r}";
            string retorder = "";

            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            if (!SendData(order, ref retorder, $"TTN", timeout))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            string returnstr = /*Encoding.Default.GetString*/retorder.Trim('\0').Trim('\n').Trim('\r');
            return true;
        }
        /// <summary>
        /// 标定流程
        /// </summary>
        /// <param name="nozzleId">吸嘴号</param>
        /// <param name="type">标定类型</param>
        /// <param name="step">步骤数</param>
        /// <param name="count">次数</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="r">R</param>
        /// <returns></returns>
        public bool SendJointCalib(NozzleId nozzleId, string type, int step, int count, float x, float y, float r, float x2, float y2, int timeout = -1)
        {
            if (!Param.BUse) return true;

            string order = $"Calib,{(int)nozzleId},{type},{step},{count},{x},{y},{r},{x2},{y2},{r}" + "\r\n";
            string retorder = "";

            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            if (!SendData(order, ref retorder, $"rCalib,{(int)nozzleId},{type}", timeout))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            string returnstr = /*Encoding.Default.GetString*/retorder.Trim('\0').Trim('\n').Trim('\r');
            return true;
            //if (returnstr.Contains($"rCalib,{(int)nozzleId},{type},OK"))
            //{
            //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"接受命令成功", En_Logout_Type.Other);
            //    return true;
            //}
            //else
            //{
            //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"接受命令失败", En_Logout_Type.Other);
            //    return false;
            //}
        }
        public bool SendJointCalib(NozzleId nozzleId, string type, int step, int count, float x, float y, float r, int timeout = -1)
        {
            if (!Param.BUse) return true;

            string order = $"Calib,{(int)nozzleId},{type},{step},{count},{x},{y},{r}" + "\r\n";
            string retorder = "";

            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            if (!SendData(order, ref retorder, $"rCalib,{(int)nozzleId},{type}", timeout))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            string returnstr = /*Encoding.Default.GetString*/retorder.Trim('\0').Trim('\n').Trim('\r');
            return true;
            //if (returnstr.Contains($"rCalib,{(int)nozzleId},{type},OK"))
            //{
            //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"接受命令成功", En_Logout_Type.Other);
            //    return true;
            //}
            //else
            //{
            //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"接受命令失败", En_Logout_Type.Other);
            //    return false;
            //}
        }
        #endregion

        #region 生产协议
        /// <summary>
        /// Feeder拍照
        /// </summary>
        /// <param name="feederId">FeederID</param>
        /// <param name="nozzleId">吸嘴ID</param>
        /// <returns></returns>
        public bool FeederCdd(string carriersn = "")
        {
            var _stepStatus = IoC.Get<StepStatus>();
            FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
            NozzleId nozzleId = _stepStatus.PickTapeOk[(int)NozzleId.一号吸嘴 - 1] ? NozzleId.一号吸嘴 : NozzleId.二号吸嘴;
            //T1或T2,载具SN,穴位SN
            try
            {
                if (!_cameraParam.BUse) return true;
                var _baseBiz = IoC.Get<IBaseBiz>() as BaseBiz;
                string order = string.Empty;
                //根据不同飞达发送不同的指令
                if (feederId == FeederId.左飞达)
                {
                    order += $"T1";
                }
                else if (feederId == FeederId.右飞达)
                {
                    order += $"T2";
                }
                //手动模式载具码使用Test
                if (!_baseBiz.AutoRun)
                {
                    order += $",Test";
                }
                else
                {
                    order += $",{carrierSN}";
                }
                //手动模式存到测试文件夹
                if (!_baseBiz.AutoRun)
                {
                    order += ",Test";
                }
                else
                {
                    //if (string.IsNullOrEmpty(FeederCddSn[(int)nozzleId - 1]))
                    //{
                    //    FeederCddSn[(int)nozzleId - 1] = $"Virtual{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                    //}
                    order += $",{(int)nozzleId - 1}";
                    if (!string.IsNullOrEmpty(carriersn))
                    {
                        order += carriersn;
                    }
                }
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (feederId == FeederId.左飞达)
                {
                    if (!returnstr.StartsWith($"T1"))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                        return false;
                    }
                }
                else if (feederId == FeederId.右飞达)
                {
                    if (!returnstr.StartsWith($"T2"))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                        return false;
                    }
                }
                feederCDDMode = new FeederCDDMode();
                if (!feederCDDMode.AnalysisReOrder(order, returnstr))
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }
        public float[] GetPickPos(int nozzleid)
        {
            var _stepStatus = IoC.Get<StepStatus>();
            int posnum = 0;
            int index = 0;
            //吸嘴1对应左，吸嘴2对应右，优先取对应的物料
            if (nozzleid == (int)NozzleId.一号吸嘴)
            {
                if (feederCDDMode.TapeOk[0])
                {
                    index = 0;
                    posnum = 0;
                    feederCDDMode.TapeOk[0] = false;
                }
                else
                {
                    index = 1;
                    posnum = 1;
                    feederCDDMode.TapeOk[1] = false;
                }
            }
            else
            {
                if (feederCDDMode.TapeOk[1])
                {
                    index = 1;
                    posnum = 3;
                    feederCDDMode.TapeOk[1] = false;
                }
                else
                {
                    index = 0;
                    posnum = 2;
                    feederCDDMode.TapeOk[0] = false;
                }
            }
            float[] visionpos = new float[] { feederCDDMode.ResPoss[posnum].ResPosX, feederCDDMode.ResPoss[posnum].ResPosY, feederCDDMode.ResPoss[posnum].ResPosR };//视觉坐标
            float[] offsets = _stepStatus.FeederSingleOffset(nozzleid, index);//补偿
            float[] pos = new float[] { visionpos[0] + offsets[0], visionpos[1] + offsets[1], visionpos[2] + offsets[2] };//最终取料位置
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"获取-({_stepStatus.CacheParamManager.HomeUiParam.Enable.FeederId}-{(NozzleId)nozzleid}-{(index == 0 ? '左' : '右')}物料)取料位置:视觉:(X:{visionpos[0]},Y:{visionpos[1]},R:{visionpos[2]});补偿:(X:{offsets[0]},Y:{offsets[1]},R:{offsets[2]});最终位置:(X:{pos[0]},Y:{pos[1]},R:{pos[2]})", En_Logout_Type.Other, true);
            return pos;
        }
        /// <summary>
        /// 下视觉定位
        /// </summary>
        /// <param name="nozzleId">吸嘴号</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public bool DownCdd(int nozzleId, float x, float y, float r)
        {
            //T3或T4,载具SN,穴位SN,X,Y,R
            try
            {
                if (!_cameraParam.BUse) return true;
                var _baseBiz = IoC.Get<IBaseBiz>() as BaseBiz;
                string order = string.Empty;
                if (nozzleId == (int)NozzleId.一号吸嘴)
                {
                    order += $"T3";
                }
                else if (nozzleId == (int)NozzleId.二号吸嘴)
                {
                    order += $"T4";
                }
                if (!_baseBiz.AutoRun)
                {
                    order += $",Test";
                }
                else
                {
                    order += $",{carrierSN}";
                }
                if (!_baseBiz.AutoRun)
                {
                    order += ",Test";
                }
                else
                {
                    //if (string.IsNullOrEmpty(DownCddSn[(int)nozzleId - 1]))
                    //{
                    //    DownCddSn[(int)nozzleId - 1] = $"Virtual{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                    //}
                    order += $",{(int)nozzleId - 1}";
                }
                order += $",{x},{y},{r}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (nozzleId == (int)NozzleId.一号吸嘴)
                {
                    if (!returnstr.StartsWith($"T3"))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                        return false;
                    }
                }
                else if (nozzleId == (int)NozzleId.二号吸嘴)
                {
                    if (!returnstr.StartsWith($"T4"))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                        return false;
                    }
                }
                if (!downCDDMode.AnalysisReOrder(order, returnstr))
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"下视觉拍照-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// 下视觉定位
        /// </summary>
        /// <param name="nozzleId">吸嘴号</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public bool DownCdd(int nozzleId, float x, float y, float r,ref float result_RS)
        {
            //T3或T4,载具SN,穴位SN,X,Y,R
            try
            {
                if (!_cameraParam.BUse) return true;
                var _baseBiz = IoC.Get<IBaseBiz>() as BaseBiz;
                string order = string.Empty;
                if (nozzleId == (int)NozzleId.一号吸嘴)
                {
                    order += $"T3";
                }
                else if (nozzleId == (int)NozzleId.二号吸嘴)
                {
                    order += $"T4";
                }
                if (!_baseBiz.AutoRun)
                {
                    order += $",Test";
                }
                else
                {
                    order += $",{carrierSN}";
                }
                if (!_baseBiz.AutoRun)
                {
                    order += ",Test";
                }
                else
                {
                    //if (string.IsNullOrEmpty(DownCddSn[(int)nozzleId - 1]))
                    //{
                    //    DownCddSn[(int)nozzleId - 1] = $"Virtual{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                    //}
                    order += $",{(int)nozzleId - 1}";
                }
                order += $",{x},{y},{r}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (nozzleId == (int)NozzleId.一号吸嘴)
                {
                    if (!returnstr.StartsWith($"T3"))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                        return false;
                    }
                }
                else if (nozzleId == (int)NozzleId.二号吸嘴)
                {
                    if (!returnstr.StartsWith($"T4"))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                        return false;
                    }
                }
                if (!downCDDMode.AnalysisReOrder(order, returnstr))
                {
                    return false;
                }
                string[] str = returnstr.Split(',');
                result_RS = float.Parse(str[4]);
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"下视觉拍照-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// 上视觉定位
        /// </summary>
        /// <param name="cav">穴位号</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public bool UpCdd(int cav, float x, float y, float r, string carriersn = "Test", string cavsn = "Test")
        {
            //T5,载具SN,穴位SN,X,Y,R,穴位号
            try
            {
                if (!_cameraParam.BUse) return true;
                var _baseBiz = IoC.Get<IBaseBiz>() as BaseBiz;
                string order = "T5";
                if (!_baseBiz.AutoRun)
                {
                    order += $",Test";
                    order += $",Test";
                }
                else
                {
                    order += $",{carriersn}";
                    order += $",{cavsn}_{cav}";
                }
                order += $",{x},{y},{r},{cav}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (!returnstr.StartsWith($"T5"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                if (!upCDDMode.AnalysisReOrder(order, returnstr))
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// 上视觉定位
        /// </summary>
        /// <param name="cav">穴位号</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public bool UpCdd(int cav, float x, float y, float r, string carriersn , string cavsn ,ref int errorCode)
        {
            //T5,载具SN,穴位SN,X,Y,R,穴位号
            try
            {
                if (!_cameraParam.BUse) return true;
                var _baseBiz = IoC.Get<IBaseBiz>() as BaseBiz;
                string order = "T5";
                if (!_baseBiz.AutoRun)
                {
                    order += $",Test";
                    order += $",Test";
                }
                else
                {
                    order += $",{carriersn}";
                    order += $",{cavsn}_{cav}";
                }
                order += $",{x},{y},{r},{cav}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (!returnstr.StartsWith($"T5"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                if (!upCDDMode.AnalysisReOrder(order, returnstr, ref errorCode))
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// 获取贴装位置
        /// </summary>
        /// <param name="nozzleId">吸嘴号</param>
        /// <param name="cav">穴位号</param>
        /// <returns></returns>
        public bool GetFitPos(int nozzleId, int cav, out float[] fitpos)
        {
            fitpos = null;
            //T6,吸嘴号,穴位号
            try
            {
                if (!_cameraParam.BUse) return true;
                string order = string.Empty;
                order += $"T6,{nozzleId},{cav}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (!returnstr.StartsWith($"T6"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                if (!getFitPosMode.AnalysisReOrder(order, returnstr))
                {
                    return false;
                }
                fitpos = getFitPosMode.ResPosConvert();
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取贴装位置-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// 获取贴装位置
        /// </summary>
        /// <param name="carriersn">载具码</param>
        /// <param name="cav">穴位号</param>
        /// <returns></returns>
        public bool SaveAllPhoto(string carriersn, int cav, int times)
        {
            //T6,吸嘴号,穴位号
            try
            {
                if (!_cameraParam.BUse) return true;
                string order = string.Empty;
                order += $"S{times},{carriersn},{cav}";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (!returnstr.StartsWith($"S{times}"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = returnstr.Split(',');
                if (str.Length != 2)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照接收数据个数不为2！", En_Logout_Type.Other);
                    return false;
                }
                if (str[1] != "OK")
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照失败！", En_Logout_Type.Other);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取贴装位置-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        #endregion

        object obj = new object();

        #region 图像分组
        /// <summary>
        /// 按照穴位分类
        /// </summary>
        public void CavGroup(string carriersn, string cavsn, string feederCddSn, string nozzleNo)
        {
            lock (obj)
            {
                string date = DateTime.Now.ToString("yyyyMMdd");
                string previousdate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
                Task.Run(() =>
                {
                    Thread.Sleep(500);
                    List<string> cddsn = new List<string>();
                    ////移动Feeder对应图片到对应的cavsn中
                    //cddsn.Add(feederCddSn);
                    ////移动下视觉对应图片到对应的cavsn中
                    //cddsn.Add(nozzleNo);
                    //foreach (string imgstring in cddsn)
                    //{
                    //if (string.IsNullOrEmpty(imgstring) || cavsn == imgstring)
                    //{
                    //    continue;
                    //}
                    //    else
                    //    {

                    List<string> camType = new List<string> { "左飞达","右飞达", "吸嘴1", "吸嘴2" };
                    List<string> photoType = new List<string> { "RecordImage", "SourceImage" };
                    for (int i = 0; i < camType.Count; i++)
                    {
                        for (int j = 0; j < photoType.Count; j++)
                        {
                            string path = $"{_cameraParam.ImgPath}\\{date}\\{camType[i]}";
                            string sourceFolder = $"{path}\\Production\\{nozzleNo}\\{photoType[j]}";
                            string destinationFolder = $"{path}\\{carriersn}\\{cavsn}\\{photoType[j]}";
                            if (camType[i].Contains("飞达"))
                            {
                                destinationFolder = $"{path}\\{carriersn}\\{feederCddSn}\\{photoType[j]}";
                            }
                            MoveFolderContentsAndDelete(sourceFolder, destinationFolder);
                            //避免凌晨跨天的图片未移动
                            path = $"{_cameraParam.ImgPath}\\{previousdate}\\{camType[i]}";
                            sourceFolder = $"{path}\\Production\\{nozzleNo}\\{photoType[j]}";
                            destinationFolder = $"{path}\\{carriersn}\\{cavsn}\\{photoType[j]}";
                            if (camType[i].Contains("飞达"))
                            {
                                destinationFolder = $"{path}\\{carriersn}\\{feederCddSn}\\{photoType[j]}";
                            }
                            MoveFolderContentsAndDelete(sourceFolder, destinationFolder);
                        }
                    }
                    //    }
                    //}
                });
            }
        }
        /// <summary>
        /// 移动文件夹中的子文件夹及文件并删除源文件
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public bool MoveFolderContentsAndDelete(string sourceFolder, string destinationFolder, bool overwrite = true)
        {
            try
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"原路径：{sourceFolder};移动路径{destinationFolder}", En_Logout_Type.Other, true);
                // 验证源文件夹是否存在
                if (!Directory.Exists(sourceFolder))
                {
                    return true;
                }

                // 确保目标文件夹存在
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // 移动所有文件和子文件夹
                MoveDirectoryContents(sourceFolder, destinationFolder, overwrite);

                // 删除原文件夹
                Directory.Delete(sourceFolder, true);
                Console.WriteLine($"成功移动文件夹内容并从 '{sourceFolder}' 到 '{destinationFolder}'，并删除原文件夹");
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"移动文件夹内容失败: {ex.Message}", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// 递归移动文件夹中的所有内容
        /// </summary>
        private void MoveDirectoryContents(string sourcePath, string destinationPath, bool overwrite)
        {
            // 移动所有文件
            foreach (string sourceFile in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(sourceFile);
                string destFile = Path.Combine(destinationPath, fileName);

                // 如果目标文件已存在
                if (File.Exists(destFile))
                {
                    if (overwrite)
                    {
                        // 删除目标文件，然后移动源文件
                        File.Delete(destFile);
                        File.Move(sourceFile, destFile);
                        Console.WriteLine($"覆盖文件: {destFile}");
                    }
                    // 如果不覆盖，则跳过此文件
                    else
                    {
                        Console.WriteLine($"跳过文件 (已存在): {destFile}");
                    }
                }
                else
                {
                    // 目标文件不存在，直接移动
                    File.Move(sourceFile, destFile);
                    Console.WriteLine($"移动文件: {sourceFile} -> {destFile}");
                }
            }

            // 递归移动所有子文件夹
            foreach (string sourceDir in Directory.GetDirectories(sourcePath))
            {
                string dirName = Path.GetFileName(sourceDir);
                string destDir = Path.Combine(destinationPath, dirName);

                // 如果目标文件夹不存在，直接移动
                if (!Directory.Exists(destDir))
                {
                    Directory.Move(sourceDir, destDir);
                    Console.WriteLine($"移动文件夹: {sourceDir} -> {destDir}");
                }
                else
                {
                    // 目标文件夹已存在，递归处理内容
                    MoveDirectoryContents(sourceDir, destDir, overwrite);

                    // 如果源文件夹为空，删除它
                    if (Directory.GetFiles(sourceDir).Length == 0 &&
                        Directory.GetDirectories(sourceDir).Length == 0)
                    {
                        Directory.Delete(sourceDir);
                    }
                }
            }
        }
        #endregion

        #endregion

        #region 标定协议






        /// <summary>
        /// 标定流程
        /// </summary>
        /// <param name="command">指令（默认为C）</param>
        /// <param name="calibnumber">标定编号</param>
        /// <param name="X">X轴</param>
        /// <param name="Y">Y轴</param>
        /// <param name="A">A轴</param>
        /// <returns></returns>
        public bool CalibProcess(int calibnumber, float X, float Y, float A, string command = "C")
        {
            if (!Param.BUse) return true;
            string order = command + calibnumber + "," + X + "," + Y + "," + A + "\r\n";
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (returnstr.Contains(command + calibnumber.ToString() + ",1"))
            {
                return true;
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }
        }

        /// <summary>
        /// 标定流程中发送SET
        /// </summary>
        /// <param name="calibnumber">标定编号</param>
        /// <param name="getimagetimes">当前标定流程中需要取像的数量</param>
        /// <param name="X">X轴</param>
        /// <param name="Y">Y轴</param>
        /// <param name="A">R轴</param>
        /// <returns></returns>
        public bool CalibProcessSendSet(int calibnumber, int getimagetimes, float X, float Y, float A)
        {
            if (!Param.BUse) return true;
            string order = "SET," + calibnumber + "," + getimagetimes + "," + X + "," + Y + "," + A + "\r\n";
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);

            if (returnstr.Contains("SET,1"))
            {
                return true;
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }
        }

        /// <summary>
        /// 结束所有标定流程，并整合所有标定文件信息
        /// </summary>
        /// <param name="command">指令（默认为UN）</param>
        /// <param name="number">1</param>
        /// <returns></returns>
        public bool EndCalibUN(string command = "UN", int number = 1)
        {
            if (!Param.BUse) return true;
            string order = command + "," + number + "\r\n";
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (returnstr.Contains(command + ",1"))
            {
                return true;
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }
        }

        public bool SendScCalib(int calibnumber, int grabImageTimes)
        {
            if (!Param.BUse) return true;
            string order = "SC," + calibnumber.ToString() + "," + grabImageTimes.ToString() + "\r\n";//同步接收：SC,1\r\n
            string retorder = "";

            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            if (!SendData(order, ref retorder))//, "SC,1" + calibnumber.ToString()
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            string returnstr = /*Encoding.Default.GetString*/retorder.Trim('\0').Trim('\n').Trim('\r');
            if (returnstr.Contains("SC,1"))//+ calibnumber.ToString()
            {
                return true;
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"接收命令没有包含：SC,{calibnumber.ToString()}", En_Logout_Type.Other);
                return false;
            }
        }

        public bool SendEcCalib()
        {
            if (!Param.BUse) return true;
            string order = "EC\r\n";
            string retorder = "";
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            if (!SendData(order, ref retorder, "EC,1", 1, false))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            return true;
            //NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            //string returnstr = /*Encoding.Default.GetString*/retorder.Trim('\0').Trim('\n').Trim('\r');
            //if (returnstr.Contains("EC,1"))
            //{
            //    return true;
            //}
            //else
            //{
            //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"接收命令没有包含：EC,1", En_Logout_Type.Other);
            //    return false;
            //}
        }



        public bool SendCB8()
        {
            if (!Param.BUse) return true;
            string order = "CB,8" + "\r\n";//同步接收：SC,1\r\n
            string retorder = "";

            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            if (!SendData(order, ref retorder, "SC,1"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            string returnstr = /*Encoding.Default.GetString*/retorder.Trim('\0').Trim('\n').Trim('\r');
            if (returnstr.Contains("CB,1"))
            {
                return true;
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"接收命令没有包含：CB,8", En_Logout_Type.Other);
                return false;
            }
        }
        #endregion

        #region 生产协议

        /// <summary>
        /// Task of Locate Nozzle，定位吸嘴上物料
        /// </summary>
        /// <param name="sn">具有唯一性的SN</param>
        /// <param name="nozzleNo">吸嘴编号</param>
        /// <param name="originMaterialName">原始物料名称</param>
        /// <param name="cav">穴位号（目标物料未拍照时，可使用0）</param>
        /// <param name="targetMaterialName">目标物料名称</param>
        /// <param name="x">X轴</param>
        /// <param name="y">Y轴</param>
        /// <param name="a">R轴</param>
        /// <param name="partxya">吸嘴上物料特征坐标</param>
        /// <param name="data">视觉收集数据</param>
        /// <param name="xya">贴装目标坐标</param>
        /// <returns></returns>
        public bool TLN(string sn, int nozzleNo, string originMaterialName, int cav, string targetMaterialName, float x, float y, float a, out float[] partxya, out string[] data, out float[] xya)
        {
            partxya = null;
            data = null;
            xya = null;
            if (!_cameraParam.BUse) return true;
            string cmdID = "NA";//指令编号
            int n = 1;//拍照次数。次数越多，指令需要的项就越多，此处仅考虑拍照一次的情况
            string order;
            if (_cameraParam.UseNewOrder)
            {
                order = $"TLN,{cmdID},{n},{sn},{nozzleNo},{originMaterialName},{cav},{targetMaterialName},{x},{y},{a}\r\n";
            }
            else
            {
                order = $"TLN,{n},2,{sn},{nozzleNo},{originMaterialName},{cav},{targetMaterialName},{x},{y},{a}\r\n";
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (_cameraParam.UseNewOrder)
            {
                //不管是不是定位成功，都可以先解析数据
                try
                {
                    string[] str = returnstr.Split(',');
                    partxya = new float[] {
                    float.Parse(str[4]),
                    float.Parse(str[5]),
                    float.Parse(str[6]),};
                    data = str[7].Split('_');
                    xya = new float[] {
                    float.Parse(str[8]),
                    float.Parse(str[9]),
                    float.Parse(str[10]),};
                }
                catch (Exception)
                { }
                if (!returnstr.StartsWith($"TLN,{cmdID},{n},1"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
            }
            else
            {
                //不管是不是定位成功，都可以先解析数据
                try
                {
                    string[] str = returnstr.Split(',');
                    partxya = new float[] {
                    float.Parse(str[3]),
                    float.Parse(str[4]),
                    float.Parse(str[5]),};
                    data = str[6].Split('_');
                    xya = new float[] {
                    float.Parse(str[7]),
                    float.Parse(str[8]),
                    float.Parse(str[9]),};
                }
                catch (Exception)
                { }
                if (!returnstr.StartsWith($"TLN,{n},1"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Feeder拍照
        /// </summary>
        /// <param name="sn">具有唯一性的SN</param>
        /// <param name="targetMaterialName">原始物料名称</param>
        /// <param name="num">物料个数</param>
        /// <returns></returns>
        public bool TLM(string sn, string targetMaterialName, int num)
        {
            try
            {
                IsCDDOk = false;
                TapeNum = 0;

                //指令名称, 指令编号, 拍照次数, 料件序列号, 原始物料名称, 视野编号, 拍照时运动坐标 指令结束
                string[] strings = new string[]
                {
                "TLM",
                "NA",
                "1",
                sn,
                targetMaterialName,
                "1",
                "0",
                "0",
                "0"
                };
                string order = string.Join(",", strings) + "\r\n";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                //指令名称, 指令编号, 拍照次数, 错误代码, 视野编号， 物料个数, 视觉数据收集 指令结束
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (!returnstr.StartsWith($"TLM,NA,1,1,1"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = returnstr.Split(',');
                _plc_Component.IsFeederTapeReady();
                if (str[5] != num.ToString())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-视觉获取物料个数不匹配！", En_Logout_Type.Other, true);
                    return false;
                }
                TapeNum = num;
                IsCDDOk = true;
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// 计算物料坐标
        /// </summary>
        /// <param name="nozzleNos">吸嘴号</param>
        /// <param name="targetMaterialName">原始物料名称</param>
        /// <returns></returns>
        public bool GM(int[] nozzleNos, string targetMaterialName)
        {
            try
            {
                if (nozzleNos.Length > 2)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-程序错误-方法不允许传入2个吸嘴以上", En_Logout_Type.Alarm, true);
                    return false;
                }
                xyafloats = new List<List<float>>();
                //指令名称, 计算取料坐标个数, 吸嘴编号, 原始物料名称, 视野编号 指令结束
                List<string> strs = new List<string>();
                for (int i = 0; i < nozzleNos.Length; i++)
                {
                    strs.Add(nozzleNos[i].ToString());
                    strs.Add(targetMaterialName);
                    strs.Add("1");
                }
                List<string> strings = new List<string>()
            {
                "GM",
                nozzleNos.Length.ToString(),
                string.Join(",", strs)
            };
                string order = string.Join(",", strings) + "\r\n";
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
                string retorder = "";
                //指令名称, 计算取料坐标个数, 错误代码, 吸嘴编号, 计算取料坐标 指令结束
                if (!SendData(order, ref retorder))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                    return false;
                }
                string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
                if (!returnstr.StartsWith($"GM,{nozzleNos.Length}"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = returnstr.Split(',');
                //长度不匹配
                if (str.Length < 2 + nozzleNos.Length * 5)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-视觉返回的指令格式错误-长度不匹配", En_Logout_Type.Other, true);
                    return false;
                }
                //坐标个数不匹配
                if (str[1] != nozzleNos.Length.ToString())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-视觉返回的指令格式错误-坐标个数不匹配", En_Logout_Type.Other, true);
                    return false;
                }
                float[][] AxisFeederPickPos = new[]
                   {
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos,
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos,
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos,
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos,
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos,
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos,
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos,
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos,
                    };
                for (int i = 0; i < nozzleNos.Length; i++)
                {
                    List<string> liststring = str.Skip(2 + i * 5).Take(5).ToList();
                    //错误代码不匹配
                    if (liststring[0] != "1")
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-Tcp接收数据解析失败", En_Logout_Type.Other, true);
                        return false;
                    }
                    //吸嘴编号不匹配
                    if (liststring[1] != nozzleNos[i].ToString())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-视觉返回的指令格式错误-吸嘴编号不匹配", En_Logout_Type.Other, true);
                        return false;
                    }
                    //坐标是否为数字
                    List<string> listpos = liststring.Skip(2).Take(3).ToList();
                    if (!float.TryParse(listpos[0], out _) || !float.TryParse(listpos[1], out _) || !float.TryParse(listpos[2], out _))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-视觉返回的指令格式错误-坐标格式错误", En_Logout_Type.Other, true);
                        return false;
                    }
                    List<float> listposfloat = listpos.ConvertAll((t) => { return float.Parse(t); });
                    //取料坐标超限位
                    if (listposfloat[0] < 0 || listposfloat[1] < 0)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-视觉返回的坐标过小，引导点位[[X:{listposfloat[0]}Y:{listposfloat[1]}R:{listposfloat[2]}]]", En_Logout_Type.Other, true);
                        return false;
                    }
                    xyafloats.Add(listposfloat);
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-程序错误", En_Logout_Type.Other, true);
                return false;
            }
        }

        /// <summary>
        /// Feeder拍照不等待结果
        /// </summary>
        /// <param name="sn">具有唯一性的SN</param>
        /// <param name="targetMaterialName">原始物料名称</param>
        /// <param name="nozzleNos">吸嘴号</param>
        /// <param name="tapeNum">Feeder个数</param>
        /// <param name="cddnum"></param>
        public async void FeederCDD(string targetMaterialName, int[] nozzleNos, int tapeNum, int cddnum)
        {
            await Task.Run(() =>
            {
                _cacheParamManager.HomeUiParam.Enable.OncePickNum = EN_OncePickNum.One;
                IsCDDOk = false;
                //判断传入吸嘴数量及Feeder物料数量是否符合实际情况
                if (nozzleNos.Length < 1 || nozzleNos.Length > 4)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-取料吸嘴个数不匹配", En_Logout_Type.Run, true);
                    return;
                }
                if (tapeNum < 1 || tapeNum > 2)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-Feeder物料个数不匹配", En_Logout_Type.Run, true);
                    return;
                }
                string sn = $"TLM_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                for (int i = 0; i < cddnum; i++)
                {
                    if (TLM(sn, targetMaterialName, nozzleNos.Length))
                    {
                        if (GM(nozzleNos, targetMaterialName))
                        {
                            IsCDDOk = true;
                            break;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetMaterialName"></param>
        /// <param name="nozzleNos"></param>
        /// <param name="tapeNum"></param>
        /// <returns></returns>
        public bool FeederCDD(string targetMaterialName, int[] nozzleNos, int tapeNum)
        {
            //判断传入吸嘴数量及Feeder物料数量是否符合实际情况
            if (nozzleNos.Length < 1 || nozzleNos.Length > 4)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-取料吸嘴个数不匹配", En_Logout_Type.Run, true);
                return false;
            }
            if (tapeNum < 1 || tapeNum > 2)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照-Feeder物料个数不匹配", En_Logout_Type.Run, true);
                return false;
            }
            string sn = $"TLM_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
            if (!TLM(sn, targetMaterialName, tapeNum))
            {
                xyafloats = new List<List<float>>();
                //如果Feeder物料数等于2
                if (tapeNum == 2)
                {
                    if (_cacheParamManager.HomeUiParam.Enable.OncePickNum == EN_OncePickNum.One)
                    {
                        for (int i = 0; i < nozzleNos.Length; i++)
                        {
                            if (!GM(new int[] { nozzleNos[i] }, targetMaterialName))
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {

                    }
                }
                //反之,每个吸嘴对应一个物料计算一个取料坐标------不确定
                else
                {
                    for (int i = 0; i < nozzleNos.Length; i++)
                    {
                        if (!GM(new int[] { nozzleNos[i] }, targetMaterialName))
                        {
                            return false;
                        }
                    }

                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Task of Locate Target，定位载具内物料
        /// </summary>
        /// <param name="sn">具有唯一性的SN</param>
        /// <param name="nozzleNo">吸嘴编号（原始物料未拍照时，可使用0）</param>
        /// <param name="originMaterialName">原始物料名称</param>
        /// <param name="cav">穴位号</param>
        /// <param name="targetMaterialName">目标物料名称</param>
        /// <param name="x">X轴</param>
        /// <param name="y">Y轴</param>
        /// <param name="a">R轴</param>
        /// <param name="partxya">载具内物料特征坐标</param>
        /// <param name="xya">贴装目标坐标</param>
        /// <returns></returns>
        public bool TLT(string sn, int nozzleNo, string originMaterialName, int cav, string targetMaterialName, float x, float y, float a, out float[] partxya, out string[] data, out float[] xya)
        {
            partxya = null;
            data = null;
            xya = null;
            if (!_cameraParam.BUse) return true;
            string cmdID = "NA";//指令编号
            int n = 1;//拍照次数。次数越多，指令需要的项就越多，此处仅考虑拍照一次的情况
            string order;
            if (_cameraParam.UseNewOrder)
            {
                order = $"TLT,{cmdID},{n},{sn},{nozzleNo},{originMaterialName},{cav},{targetMaterialName},{x},{y},{a}\r\n";
            }
            else
            {
                order = $"TLT,{n},1,{sn},{nozzleNo},{originMaterialName},{cav},{targetMaterialName},{x},{y},{a}\r\n";
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (_cameraParam.UseNewOrder)
            {
                //不管是不是定位成功，都可以先解析数据
                try
                {
                    string[] str = returnstr.Split(',');
                    partxya = new float[] {
                    float.Parse(str[4]),
                    float.Parse(str[5]),
                    float.Parse(str[6]),};
                    data = str[7].Split('_');
                    xya = new float[] {
                    float.Parse(str[8]),
                    float.Parse(str[9]),
                    float.Parse(str[10]),};
                }
                catch (Exception)
                { }
                if (!returnstr.StartsWith($"TLT,{cmdID},{n},1"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
            }
            else
            {
                //不管是不是定位成功，都可以先解析数据
                try
                {
                    string[] str = returnstr.Split(',');
                    partxya = new float[] {
                    float.Parse(str[3]),
                    float.Parse(str[4]),
                    float.Parse(str[5]),};
                    data = str[6].Split('_');
                    xya = new float[] {
                    float.Parse(str[7]),
                    float.Parse(str[8]),
                    float.Parse(str[9]),};
                }
                catch (Exception)
                { }
                if (!returnstr.StartsWith($"TLT,{n},1"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }

            }
            return true;
        }

        /// <summary>
        /// Get Target Pose for Place，取料获取贴装坐标前，计算吸嘴贴装物料的机构目标位置(不拍照)
        /// </summary>
        /// <param name="nozzleNo">吸嘴编号</param>
        /// <param name="originMaterialName">物料A</param>
        /// <param name="targetMaterialName">物料B</param>
        /// <param name="cav">穴位号</param>
        /// <param name="xya">贴装目标坐标</param>
        /// <returns></returns>
        public bool GT(int nozzleNo, string originMaterialName, int cav, string targetMaterialName, out float[] xya)
        {
            xya = null;
            if (!_cameraParam.BUse) return true;
            int n = 1;//计算贴装坐标个数。个数越多，指令需要的项就越多，此处仅考虑计算一个的情况
            string order = $"GT,{n},{nozzleNo},{originMaterialName},{cav},{targetMaterialName}\r\n";
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (!returnstr.StartsWith($"GT,{n},1"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }
            string[] str = returnstr.Split(',');
            xya = new float[] {
                float.Parse(str[3]),
                float.Parse(str[4]),
                float.Parse(str[5]),
            };
            return true;
        }

        /// <summary>
        /// Task of Final Check，复检
        /// </summary>
        public bool TFC(string sn, int cav, string material, float x, float y, float a, out string[] data)
        {
            data = null;
            if (!_cameraParam.BUse) return true;
            string cmdID = "NA";//指令编号
            int n = 1;//拍照次数。次数越多，指令需要的项就越多，此处仅考虑拍照一次的情况
            string order;
            if (_cameraParam.UseNewOrder)
            {
                order = $"TFC,{cmdID},{n},{sn},{cav},{material},{x},{y},{a}\r\n";
            }
            else
            {
                order = $"TFC,{n},1,{sn},1,Foam,{cav},{material},{x},{y},{a}\r\n";
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (_cameraParam.UseNewOrder)
            {
                if (!returnstr.StartsWith($"TFC,{cmdID},{n},1"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = returnstr.Split(',');
                data = str[4].Split('_');
            }
            else
            {
                if (!returnstr.StartsWith($"TFC,{n},1"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = returnstr.Split(',');
                data = str[3].Split('_');
            }
            return true;
        }

        /// <summary>
        /// 复检，判断Bumper状态
        /// </summary>
        public bool TFC_CheckBumper(string sn, int cav, string materialName)
        {
            return TFC(sn, cav, materialName, 0, 0, 0, out string[] data);
        }

        /// <summary>
        /// Change Product，切换视觉产品（即左下角产品，一般用于切换曝光）
        /// </summary>
        /// <param name="productID">产品序号，1开始</param>
        /// <returns></returns>
        public bool CP(int productID)
        {
            if (!Param.BUse) return true;
            string order = $"CP,{productID}\r\n";
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (!returnstr.StartsWith("CP,1"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Change Config，切换视觉任务（例如复检任务有多个，则可使用该指令切换）
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="materialName"></param>
        /// <param name="configID"></param>
        /// <returns></returns>
        public bool CC(string taskName, string materialName, int configID)
        {
            if (!Param.BUse) return true;
            int n = 1;//配置数量。数量越多，指令需要的项就越多，此处仅考虑切换一个配置的情况
            string order = $"CC,{n},{taskName},{materialName},{configID}\r\n";
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"发送命令：{order}", En_Logout_Type.Other);
            string retorder = "";
            if (!SendData(order, ref retorder))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收失败！", En_Logout_Type.Other);
                return false;
            }
            string returnstr = retorder.Trim('\0').Trim('\n').Trim('\r');
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"接收命令：{retorder.Trim('\0')}", En_Logout_Type.Other);
            if (!returnstr.StartsWith("CC,1"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 相机定位NG弹框提示
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool CDDNG(string str)
        {
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Camera, $"{str}失败，是否重新拍照？", 108);
            if (MessageBox.Show($"{str}失败，是否重新拍照？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _baseBiz.RemoveRunAlarm(alarm);
                return true;
            }
            _baseBiz.RemoveRunAlarm(alarm);
            return false;
        }
        #endregion

        #region 图像分组命名相关

        /*
         * 图像分组命名功能由两部分组成。
         * 其一是康耐视的Group指令，可以将图像移动到指定文件夹。
         * 注意Group指令只能移动到相对路径（父文件夹为年月日文件夹）。
         * 其二是代码编写的修改图像名称的方法，可以修改移动完毕的图像的名称。
         * 
         * 以下是各类型图像存储的位置。
         * 【下定位ok且使用】
         * 相对路径：DownVision/OK/Nozzle4
         * 图片名称：235959_sip_antenna_flex_OKNG
         * 【下定位ok且不使用*】
         * 相对路径：unuseimages
         * 图片名称：不需要改
         * 【下定位ng*】
         * 相对路径：DownVision/NG/Nozzle4
         * 图片名称：235959_NG
         * 【上定位OKNG】
         * 相对路径：UpVision/OKNG/Cav12
         * 图片名称：235959_sip_antenna_flex_OKNG
         * 【上天线码OKNG】
         * 相对路径：Antenna/OKNG/Cav12
         * 图片名称：235959_sip_antenna_flex_OKNG
         * 【上距离】
         * 相对路径：Measure/Cav12
         * 图片名称：235959_sip_antenna_flex
         * 【上复检】
         * 相对路径：Recheck/Cav12
         * 图片名称：235959_sip_antenna_flex
         * 
         * 注：下定位ok且不使用 指的是拍照后没有载具，直接复位的情况。在每一板载具处理完时，以及复位时，应该移动这些图片
         * 下定位ng 出现时，该图像并不会与任何一个sip板关联，所以也没有SN和时间。时间应自行输入。
         */

        private object groupLock = new object();

        public class GroupInfo
        {
            private string baseImgPath;
            private string sn;
            private string relativeDir;
            private string fileNameSuffix;

            public GroupInfo(string baseImgPath, string sn, string relativeDir, string fileNameSuffix)
            {
                this.baseImgPath = baseImgPath;
                this.sn = sn;
                this.relativeDir = relativeDir;
                this.fileNameSuffix = fileNameSuffix;
            }

            public bool MoveImg()
            {
                try
                {
                    string originDir;
                    string originFileName1;
                    string originFileName2;
                    string destDir;
                    string destFileName1;
                    string destFileName2;
                    //根据sn找到文件夹
                    DateTime imgTime = DateTime.Now;
                    originDir = baseImgPath + "\\" + imgTime.ToString("yyyyMMdd") + "\\" + sn;
                    if (!Directory.Exists(originDir))
                    {
                        //有可能执行group指令的时候已经过了零点了，所以往前推一天再找
                        imgTime.AddDays(-1);
                        originDir = baseImgPath + "\\" + imgTime.ToString("yyyyMMdd") + "\\" + sn;
                    }
                    if (!Directory.Exists(originDir))
                    {
                        return false;
                    }
                    //获取原图、处理图文件名，bmp、png为原图，jpg格式为处理图
                    string[] files = Directory.GetFiles(originDir);
                    DateTime dt = DateTime.Now;
                    if (files.Length != 2)
                    {
                        return false;
                    }
                    if ((files[0].EndsWith("bmp") || files[0].EndsWith("png")) && files[1].EndsWith("jpg"))
                    {
                        originFileName1 = files[0].Substring(files[0].LastIndexOf("\\") + 1);
                        originFileName2 = files[1].Substring(files[1].LastIndexOf("\\") + 1);
                    }
                    else if ((files[1].EndsWith("bmp") || files[1].EndsWith("png")) && files[0].EndsWith("jpg"))
                    {
                        originFileName1 = files[1].Substring(files[1].LastIndexOf("\\") + 1);
                        originFileName2 = files[0].Substring(files[0].LastIndexOf("\\") + 1);
                    }
                    else
                    {
                        return false;
                    }
                    //获取目标文件夹与目标文件名
                    destDir = baseImgPath + "\\" + imgTime.ToString("yyyyMMdd") + "\\" + relativeDir.Replace("/", "\\");
                    destFileName1 = originFileName1.Substring(0, 10) + fileNameSuffix + originFileName1.Substring(originFileName1.LastIndexOf("."));
                    destFileName2 = originFileName2.Substring(0, 10) + fileNameSuffix + originFileName2.Substring(originFileName2.LastIndexOf("."));
                    //移动图片
                    string originFile1 = originDir + "//" + originFileName1;
                    string originFile2 = originDir + "//" + originFileName2;
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    string destFile1 = destDir + "//" + destFileName1;
                    string destFile2 = destDir + "//" + destFileName2;
                    File.Copy(originFile1, destFile1, true);
                    File.Copy(originFile2, destFile2, true);
                    File.Delete(originFile1);
                    File.Delete(originFile2);
                    Directory.Delete(originDir);
                    return true;
                }
                catch (Exception ex)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
                    return false;
                }
            }
        }

        /// <summary>
        /// Group参数暂存位置
        /// </summary>
        public List<GroupInfo> groupInfoList = new List<GroupInfo>();

        /// <summary>
        /// 添加一个sn的分组信息，以供Group指令使用。调用时必须已经拍照完。
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="relativeDir"></param>
        /// <param name="fileNameSuffix"></param>
        public void AddGroupInfo(string sn, string relativeDir, string fileNameSuffix)
        {
            lock (groupLock)
            {
                GroupInfo info = new GroupInfo(_cameraParam.ImgPath, sn, relativeDir, fileNameSuffix);
                groupInfoList.Add(info);
            }
        }

        /// <summary>
        /// 添加一个载具的所有图像sn的分组信息，以供Group指令使用
        /// </summary>
        /// <param name="scanTime"></param>
        /// <param name="carrierSN"></param>
        public void AddCarrierGroupInfo(CarrierStatus carrier)
        {
            for (int i = 0; i < carrier.errorCode.Length; i++)
            {
                string sipSN = carrier.sipSN[i] == "" ? "NoSipSN" : carrier.sipSN[i];
                string cav = "Cav" + (i + 1);
                string preDir = $"{carrier.carrierSN}\\{cav}\\{sipSN}";
                //if (carrier.TLMOkSns[i] != "")
                //{
                //    string relativeDir = "FeederVision\\OK";
                //    string fileNameSuffix = cav + "_Nozzle" + carrier.Alert_Bumper_Nozzle[i] + "_" + sipSN + "_OK";
                //    AddGroupInfo(carrier.TLMOkSns[i], relativeDir, fileNameSuffix);
                //}
                if (carrier.TLNOkSns[i] != "")
                {
                    string relativeDir = "DownVision\\OK";
                    string fileNameSuffix = cav + "_Nozzle" + carrier.Tape_Nozzle[i] + "_" + sipSN + "_OK";
                    AddGroupInfo(carrier.TLNOkSns[i], relativeDir, fileNameSuffix);
                }
                if (carrier.TLTOkSns[i] != "")
                {
                    string relativeDir = "UpVision\\OK";
                    string fileNameSuffix = cav + "_" + sipSN + "_OK";
                    AddGroupInfo(carrier.TLTOkSns[i], relativeDir, fileNameSuffix);
                }
                if (carrier.TLTNgSns[i] != "")
                {
                    string relativeDir = "UpVision\\NG";
                    string fileNameSuffix = cav + "_" + "NG";
                    AddGroupInfo(carrier.TLTNgSns[i], relativeDir, fileNameSuffix);
                }
                if (carrier.TFC1Sns[i] != "")
                {
                    string okng = carrier.TFC1Results[i] ? "OK" : "NG";
                    string relativeDir = "CheckTape\\" + okng;
                    string fileNameSuffix = cav + "_" + sipSN + "_" + okng;
                    AddGroupInfo(carrier.TFC1Sns[i], relativeDir, fileNameSuffix);
                }
                if (carrier.TFC2Sns[i].Count != 0)
                {
                    string relativeDir = "FullSIP";
                    string fileNameSuffix = cav + "_" + sipSN;
                    foreach (var sn in carrier.TFC2Sns[i])
                    {
                        AddGroupInfo(sn, relativeDir, fileNameSuffix);
                    }
                }
            }
        }

        public void ClearGroupDic()
        {
            groupInfoList.Clear();
        }

        /// <summary>
        /// 将图像分组，移动到指定的文件夹，并修改图像名称。可以在新线程中调用已节约ct。
        /// 
        /// 【Group指令格式简介】
        /// Send:
        ///   Group, ProductionNum
        ///     ,ImageFoldeName1,Num1,SN11,...,SN1m
        ///     ,...
        ///     ,ImageFoldeNamen,Numn,SNn1,...,SNnm\r\n
        /// Received:
        ///   Group, Flag, FolderPath1,..,FolderPathn\r\n
        /// 
        /// 【Group指令参数说明】
        /// ProductionNum指要把图像分组到几个文件夹，一般来讲都是合并图像到一个文件夹内，所以用1就可以了。
        /// ProductionNum为1时，表示将SN11,...,SN1m全部移到ImageFoldeName1里面，后面以此类推。
        /// ImageFoldeName1表示要移动到哪个文件夹，可以带反斜杠表示多层文件夹。
        /// Num1表示后面的sn个数，要一致。
        /// FolderPath1表示绝对路径，如D:/Cognex/Images/日期/img/ok
        /// 
        /// 【Group指令特别注意】
        /// ImageFoldeName1是一个相对文件夹，不是绝对路径，例如它可以写"img/ok"，表示D:/Cognex/Images/日期/img/ok
        /// 也就是说，Group指令只能使用相对路径，不能把图片存到任意的位置。
        /// 除此之外，还需要注意指令的长度，如果过长会导致康耐视收不到完整指令。
        /// 所以即使将多个sn分组到多个文件夹，也最好按照文件夹依次group。
        /// 
        /// 【图像重命名】
        /// 图像重命名使用代码完成，利用Group返回的路径找到文件，并修改其名称。
        /// 由于存在多个图像group到同一个文件夹的情况，导致无法区分，所以如果需要重命名，应该每一个sn一条指令。
        /// 即使使用每一个sn一条指令，也最好在一板做完后再执行分组，避免指令之间的影响。
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns>是否成功移动所有图片。无论是否移动成功，都将图片字典清空</returns>
        public void GroupPicture()
        {
            lock (groupLock)
            {
                foreach (GroupInfo info in groupInfoList)
                {
                    info.MoveImg();
                }
                groupInfoList.Clear();
            }
        }

        /// <summary>
        /// 移动所有未使用的下视觉图像（昨天和今天）
        /// </summary>
        public void MoveUnusedImgs(bool moveDownCam)
        {
            lock (groupLock)
            {
                try
                {
                    //使用group指令移动缓存的图片
                    GroupPicture();
                    DateTime time = DateTime.Now;
                    string yesterday = (time - TimeSpan.FromDays(1)).ToString("yyyyMMdd");
                    string dir1 = _cameraParam.ImgPath + "\\" + yesterday;
                    string today = time.ToString("yyyyMMdd");
                    string dir2 = _cameraParam.ImgPath + "\\" + today;
                    string[] imgDirs = new[] { dir1, dir2 };
                    bool moveUnuseImgsOk = true;
                    foreach (var imgDir in imgDirs)
                    {
                        if (!Directory.Exists(imgDir))
                        {
                            continue;
                        }
                        string[] dirs = Directory.GetDirectories(imgDir);
                        foreach (string dir in dirs)
                        {
                            //文件夹名称就是图片对应的SN，只移动TLT、TLN、TFC开头的SN
                            string sn = dir.Substring(dir.LastIndexOf("\\") + 1);
                            if (sn.StartsWith("TLT") || (sn.StartsWith("TLN") && moveDownCam) || sn.StartsWith("TFC"))
                            {
                                string destDirParent = imgDir + "\\UnusedImages";
                                if (!Directory.Exists(destDirParent))
                                {
                                    Directory.CreateDirectory(destDirParent);
                                }
                                string targetDir = destDirParent + "\\" + sn;
                                if (!MoveDirToDir(dir, targetDir))
                                {
                                    moveUnuseImgsOk = false;
                                }
                            }
                        }
                    }
                    if (!moveUnuseImgsOk)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, "移动无用图片失败", En_Logout_Type.Exception, true);
                    }
                }
                catch (Exception ex)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
                }
            }
        }

        public void RestMoveUnusedImgs(bool moveDownCam)
        {
            lock (groupLock)
            {
                try
                {
                    //使用group指令移动缓存的图片
                    GroupPicture();
                    DateTime time = DateTime.Now;
                    string yesterday = (time - TimeSpan.FromDays(1)).ToString("yyyyMMdd");
                    string dir1 = _cameraParam.ImgPath + "\\" + yesterday;
                    string today = time.ToString("yyyyMMdd");
                    string dir2 = _cameraParam.ImgPath + "\\" + today;
                    string[] imgDirs = new[] { dir1, dir2 };
                    bool moveUnuseImgsOk = true;
                    foreach (var imgDir in imgDirs)
                    {
                        if (!Directory.Exists(imgDir))
                        {
                            continue;
                        }
                        string[] dirs = Directory.GetDirectories(imgDir);
                        foreach (string dir in dirs)
                        {
                            //文件夹名称就是图片对应的SN，只移动TLT、TLN、TFC开头的SN
                            string sn = dir.Substring(dir.LastIndexOf("\\") + 1);
                            if (sn.StartsWith("TLT") || (sn.StartsWith("TLN") && moveDownCam) || sn.StartsWith("TFC") || sn.StartsWith("TLM"))
                            {
                                string destDirParent = imgDir + "\\UnusedImages";
                                if (!Directory.Exists(destDirParent))
                                {
                                    Directory.CreateDirectory(destDirParent);
                                }
                                string targetDir = destDirParent + "\\" + sn;
                                if (!MoveDirToDir(dir, targetDir))
                                {
                                    moveUnuseImgsOk = false;
                                }
                            }
                        }
                    }
                    if (!moveUnuseImgsOk)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, "移动无用图片失败", En_Logout_Type.Exception, true);
                    }
                }
                catch (Exception ex)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
                }
            }
        }

        private bool MoveDirToDir(string srcDir, string destDir)
        {
            try
            {
                bool srcExists = Directory.Exists(srcDir);
                bool destExists = Directory.Exists(destDir);
                if (!srcExists)
                {
                    return true;
                }
                string destParentDir = Directory.GetParent(destDir).FullName;
                if (Directory.Exists(destParentDir))
                {
                    Directory.CreateDirectory(destParentDir);
                }
                if (!destExists)
                {
                    Directory.Move(srcDir, destDir);
                }
                else
                {
                    string[] files = Directory.GetFiles(srcDir);
                    foreach (var s in files)
                    {
                        string fileName = new FileInfo(s).Name;
                        File.Move(s, destDir + "\\" + fileName);
                    }
                    string[] dirs = Directory.GetDirectories(srcDir);
                    foreach (var s in dirs)
                    {
                        string dirName = new DirectoryInfo(s).Name;
                        MoveDirToDir(s, destDir + "\\" + dirName);
                    }
                    Directory.Delete(srcDir);
                }
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
                return false;
            }
        }
        public bool IsSaveAllPhotos()
        {
            return _cameraParam.SaveAllPhotos;
        }
        public bool IsOP_DownCdd()
        {
            return _cameraParam.OP_DownCdd;
        }

        #endregion
    }

    public abstract class TcpCtrls
    {
        private SocketParam _param = new SocketParam()
        {
            Ip = "127.0.0.1",
            Port = 10086,
            SendBuffSize = 8192,
            ReceiveBuffSize = 8192,
            SendTimeout = 1000,
            ReceiveTimeout = 1000,
        };
        private readonly object _socketLock = new object();
        private Socket _socket = null;
        private int _connectTimeout = 500;
        private bool _isEnabledAsyncConnected = false;
        public bool IsConnected
        {
            get
            {
                if (_socket == null)
                    return false;
                return _socket.Connected;
            }
        }

        public void SetParam(SocketParam param)
        {
            _param = param;
        }
        public void SetConnectTimeout(int millisecond = 500)
        {
            if (millisecond <= 0)
                _connectTimeout = 500;
            if (millisecond > 3000)
                _connectTimeout = 3000;
            else
                _connectTimeout = millisecond;
        }
        public bool Connect(SocketParam param, bool async = false)
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var ipe = new IPEndPoint(IPAddress.Parse(param.Ip), param.Port);
                _socket.SendBufferSize = param.SendBuffSize;
                _socket.ReceiveBufferSize = param.ReceiveBuffSize;
                _socket.SendTimeout = param.SendTimeout;
                _socket.ReceiveTimeout = param.ReceiveTimeout;
                _socket.NoDelay = true;
                _param = param;
                _isEnabledAsyncConnected = async;
                if (async)
                {
                    IAsyncResult result = _socket.BeginConnect(ipe, null, null);
                    result.AsyncWaitHandle.WaitOne(_connectTimeout);
                }
                else
                    _socket.Connect(ipe);

            }
            catch (Exception ex)
            {
                TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.CriticalError, $"TcpCtrl Init() ex,{ex.ToString() + ex.StackTrace}");
                return false;
            }
            return _socket == null ? false : _socket.Connected;
        }
        public bool Close()
        {
            lock (_socketLock)
            {
                if (_socket == null)
                    return false;
                try
                {
                    if (_socket.Connected)
                        _socket.Disconnect(true);
                    _socket.Close();
                    _socket = null;
                }
                catch (Exception ex)
                {
                    TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.CriticalError, $"TcpCtrl Close() ex,{ex.ToString() + ex.StackTrace}");
                    return false;
                }
                return true;
            }
        }
        public bool ReConnect(int retry = 0)
        {
            try
            {
                lock (_socketLock)
                {
                    Close();
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    int count = 0;
                Retry:
                    if (Connect(_param, _isEnabledAsyncConnected))
                        return true;
                    else if (count < retry)
                    {
                        count++;
                        goto Retry;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl ReConnect() ex,{ex.ToString() + ex.StackTrace}");
                return false;
            }
        }
        public bool SendData(string data, ref string receive, bool needreceive = true)
        {
            try
            {
                int count = 0;
            connect:
                lock (_socketLock)
                {
                    if (_socket == null || !_socket.Connected)
                    {
                        if (!ReConnect(1))
                            return false;
                    }
                    var senddata = Encoding.ASCII.GetBytes(data);
                    if (_socket.Available > 0)
                    {
                        var revdep = new byte[_param.ReceiveBuffSize];
                        _socket.Receive(revdep);
                    }
                    _socket.Send(senddata);
                    //if (needreceive == false) return true;
                    //var receivedata = new byte[_param.ReceiveBuffSize];
                    //if (_socket.Receive(receivedata) > 0)
                    //{
                    //    receive = Encoding.ASCII.GetString(receivedata);
                    //}
                    if (needreceive == false) return true;
                    var receivedata = new byte[_param.ReceiveBuffSize];
                    int datalength = 0;
                    int index = 0;
                    int timeout = _param.ReceiveTimeout;
                    DateTime dt = DateTime.Now;
                    datalength = _socket.Receive(receivedata);
                    Thread.Sleep(10);
                    while (_socket.Available > 0 && DateTime.Now.Subtract(dt).TotalMilliseconds < timeout)
                    {
                        if (_socket.ReceiveBufferSize - datalength <= 0)
                        {
                            TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() _socket.ReceiveBufferSize - datalength <= 0 Actually value = {(_socket.ReceiveBufferSize - datalength).ToString()}");
                            return false;
                        }
                        //参数 数据缓存区  起始位置  数据长度  值的按位组合
                        index = _socket.Receive(receivedata, datalength, _socket.ReceiveBufferSize - datalength, SocketFlags.None);
                        datalength += index;
                        Thread.Sleep(10);
                    }
                    if (DateTime.Now.Subtract(dt).TotalMilliseconds >= timeout)
                    {
                        TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() send {data} receive {receive} ,Timeout {timeout}");
                        return false;
                    }
                    receive = Encoding.ASCII.GetString(receivedata).Trim('\0');
                    if (!_socket.Connected)
                    {
                        if (count < 3)
                        {
                            count++;
                            goto connect;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() send {data} receive {receive} ex,{ex.ToString() + ex.StackTrace}");
                return false;
            }
            return true;
        }
        public bool SendData(string data, ref string receive, int minlen, bool needreceive = true)
        {
            try
            {
                int count = 0;
                if (minlen <= 0) return false;
                connect:
                lock (_socketLock)
                {
                    if (_socket == null || !_socket.Connected)
                    {
                        if (!ReConnect(1))
                            return false;
                    }
                    var senddata = Encoding.ASCII.GetBytes(data);
                    if (_socket.Available > 0)
                    {
                        var revdep = new byte[_param.ReceiveBuffSize];
                        _socket.Receive(revdep);
                    }
                    _socket.Send(senddata);
                    //if (needreceive == false) return true;
                    //var receivedata = new byte[_param.ReceiveBuffSize];
                    //if (_socket.Receive(receivedata) > 0)
                    //{
                    //    receive = Encoding.ASCII.GetString(receivedata);
                    //}
                    if (needreceive == false) return true;
                    var receivedata = new byte[_param.ReceiveBuffSize];
                    int datalength = 0;
                    int index = 0;
                    int timeout = _param.ReceiveTimeout;
                    DateTime dt = DateTime.Now;
                    datalength = _socket.Receive(receivedata);
                    while ((datalength < minlen) && DateTime.Now.Subtract(dt).TotalMilliseconds < timeout)
                    {
                        if (_socket.ReceiveBufferSize - datalength <= 0)
                        {
                            TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() _socket.ReceiveBufferSize - datalength <= 0 Actually value = {(_socket.ReceiveBufferSize - datalength).ToString()}");
                            return false;
                        }
                        //参数 数据缓存区  起始位置  数据长度  值的按位组合
                        index = _socket.Receive(receivedata, datalength, _socket.ReceiveBufferSize - datalength, SocketFlags.None);
                        datalength += index;
                        Thread.Sleep(1);
                    }
                    if (DateTime.Now.Subtract(dt).TotalMilliseconds >= timeout)
                    {
                        TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() send {data} receive {receive} ,Timeout {timeout}");
                        return false;
                    }

                    receive = Encoding.ASCII.GetString(receivedata).Trim('\0');
                    if (!_socket.Connected)
                    {
                        if (count < 3)
                        {
                            count++;
                            goto connect;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() send {data} receive {receive} ex,{ex.ToString() + ex.StackTrace}");
                return false;
            }
            return true;
        }
        public bool SendData(string data, ref string receive, string contains, int retry = 1, bool needreceive = true, int timeout = -1)
        {
            try
            {
                int count = 0;
                lock (_socketLock)
                {
                connect:
                    if (_socket == null || !_socket.Connected)
                    {
                        if (!ReConnect(retry))
                            return false;
                    }
                    var senddata = Encoding.ASCII.GetBytes(data);
                    if (_socket.Available > 0)
                    {
                        var revdep = new byte[_param.ReceiveBuffSize];
                        _socket.Receive(revdep);
                    }
                    _socket.Send(senddata);
                    if (needreceive == false) return true;
                    var receivedata = new byte[_param.ReceiveBuffSize];
                    string revtmp = "";
                    if (timeout == -1)
                    {
                        timeout = _param.ReceiveTimeout;
                    }
                    DateTime dt = DateTime.Now;
                    while (DateTime.Now.Subtract(dt).TotalMilliseconds < timeout)
                    {
                        if (_socket.Receive(receivedata) > 0)
                        {
                            revtmp = Encoding.ASCII.GetString(receivedata);
                            receive += revtmp.Trim('\0');

                            if (receive.Contains(contains))
                                return true;
                        }
                        Thread.Sleep(10);
                    }
                    if (!_socket.Connected)
                    {
                        if (count < 3)
                        {
                            count++;
                            goto connect;
                        }
                    }
                    if (!receive.Contains(contains))
                        return false;
                }
            }
            catch (Exception ex)
            {
                TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() contains send {data} receive {receive} ex,{ex.ToString() + ex.StackTrace}");
                return false;
            }
            return true;
        }
        public bool SendData(byte[] data, byte[] receive, bool needreceive = true)
        {
            try
            {
                int count = 0;
                lock (_socketLock)
                {
                connect:
                    if (_socket == null || !_socket.Connected)
                    {
                        if (!ReConnect(1))
                            return false;
                    }

                    if (_socket.Available > 0)
                    {
                        var revdep = new byte[_param.ReceiveBuffSize];
                        _socket.Receive(revdep);
                    }
                    _socket.Send(data);
                    if (needreceive == false)
                        return true;
                    var receivedata = new byte[_param.ReceiveBuffSize];
                    if (_socket.Receive(receivedata) > 0)
                    {
                        Buffer.BlockCopy(receivedata, 0, receive, 0, receive.Length);
                        return true;
                    }
                    if (!_socket.Connected)
                    {
                        if (count < 3)
                        {
                            count++;
                            goto connect;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceDebugInfo.TraceOuPut(EN_WARN_LEVEL.Error, $"TcpCtrl SendData() send {TraceDebugInfo.GetByteArrayString(data)} receive {TraceDebugInfo.GetByteArrayString(receive)} ex,{ex.ToString() + ex.StackTrace}");
                return false;
            }
            return false;
        }
    }
    public class FeederCDDMode
    {
        /// <summary>
        /// 物料是否OK
        /// </summary>
        public bool[] TapeOk = new bool[2] { false, false };
        /// <summary>
        /// 返回坐标
        /// </summary>
        public CDDResPos[] ResPoss { get; set; } = new CDDResPos[4] { new CDDResPos() { }, new CDDResPos() { }, new CDDResPos() { }, new CDDResPos() { } };


        /// <summary>
        /// 解析返回字符串
        /// </summary>
        /// <returns></returns>
        public bool AnalysisReOrder(string order, string reOrder)
        {
            try
            {
                //判断发送指令和接受指令关键字是否一致
                if (order.Substring(0, 2) != reOrder.Substring(0, 2))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照发送数据与接收数据不吻合！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = reOrder.Split(',');
                if (str.Length != 15)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照接收数据个数不为15！", En_Logout_Type.Other);
                    return false;
                }
                for (int i = 0; i < ResPoss.Length; i++)
                {
                    ResPoss[i].ResPosX = float.Parse(str[3 * i + 3]);
                    ResPoss[i].ResPosY = float.Parse(str[3 * i + 4]);
                    ResPoss[i].ResPosR = float.Parse(str[3 * i + 5]);
                    if ((ResPoss[i].ResPosX == 0 && ResPoss[i].ResPosY == 0 && ResPoss[i].ResPosR == 0))
                    {
                        ResPoss[i].IsOK = false;
                    }
                    else
                    {
                        ResPoss[i].IsOK = true;
                    }
                }
                //Feeder感应有料，视觉显示无料，状态冲突
                if (!ResPoss[0].IsOK && !ResPoss[1].IsOK && !ResPoss[2].IsOK && !ResPoss[3].IsOK)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照无物料！", En_Logout_Type.Other);
                    return false;
                }
                //物料1-吸嘴1与吸嘴2有无料信息冲突，
                if (ResPoss[0].IsOK != ResPoss[1].IsOK)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照物料1数据有误！", En_Logout_Type.Other);
                    return false;
                }
                //物料2-吸嘴1与吸嘴2有无料信息冲突，
                if (ResPoss[2].IsOK != ResPoss[3].IsOK)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照物料2数据有误！", En_Logout_Type.Other);
                    return false;
                }
                //物料1定位OK,数据OK及OK标识
                if (ResPoss[0].IsOK && ResPoss[1].IsOK && str[1] == "OK")
                {
                    TapeOk[0] = true;
                }
                //物料2定位OK,数据OK及OK标识
                if (ResPoss[2].IsOK && ResPoss[3].IsOK && str[2] == "OK")
                {
                    TapeOk[1] = true;
                }
                if (GetTapeOKNum() <= 0)
                {
                    return false;
                }
                //if (tapenum != num)
                //{
                //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照物料个数({num})与光纤感应个数({tapenum})不一致！", En_Logout_Type.Other);
                //    return false;
                //}
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder拍照Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }

        }

        public float[] GetFeederCamResPosConvert()
        {

            return new float[] 
            { ResPoss[0].ResPosX, ResPoss[0].ResPosY, ResPoss[0].ResPosR,
            ResPoss[1].ResPosX, ResPoss[1].ResPosY, ResPoss[1].ResPosR,
            ResPoss[2].ResPosX, ResPoss[2].ResPosY, ResPoss[2].ResPosR,
            ResPoss[3].ResPosX, ResPoss[3].ResPosY, ResPoss[3].ResPosR}; 
        }
        /// <summary>
        /// 获取定位成功的物料个数
        /// </summary>
        /// <returns></returns>
        public int GetTapeOKNum()
        {
            int count = TapeOk.Count(x => x);
            return count;
        }



    }
    public class DownCDDMode
    {
        /// <summary>
        /// 返回坐标
        /// </summary>
        public CDDResPos ResPos { get; set; } = new CDDResPos();


        /// <summary>
        /// 解析返回字符串
        /// </summary>
        /// <returns></returns>
        public bool AnalysisReOrder(string order, string reOrder)
        {
            try
            {
                //判断发送指令和接受指令关键字是否一致
                if (order.Substring(0, 2) != reOrder.Substring(0, 2))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"下视觉拍照发送数据与接收数据不吻合！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = reOrder.Split(',');
                if (str.Length != 5)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"下视觉拍照接收数据个数不为5！", En_Logout_Type.Other);
                    return false;
                }
                ResPos.IsOK = str[1] == "OK";
                ResPos.ResPosX = float.Parse(str[2]);
                ResPos.ResPosY = float.Parse(str[3]);
                ResPos.ResPosR = float.Parse(str[4]);
                if (!ResPos.IsOK)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"下视觉拍照失败！", En_Logout_Type.Other);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"下视觉拍照Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }

        }

        /// <summary>
        /// 坐标转换
        /// </summary>
        /// <returns></returns>
        public float[] ResPosConvert()
        {
            return new float[] { ResPos.ResPosX, ResPos.ResPosY, ResPos.ResPosR };
        }
    }
    public class UpCDDMode
    {
        /// <summary>
        /// 返回坐标
        /// </summary>
        public CDDResPos ResPos { get; set; } = new CDDResPos();


        /// <summary>
        /// 解析返回字符串
        /// </summary>
        /// <returns></returns>
        public bool AnalysisReOrder(string order, string reOrder)
        {
            try
            {
                //判断发送指令和接受指令关键字是否一致
                if (order.Substring(0, 2) != reOrder.Substring(0, 2))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照发送数据与接收数据不吻合！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = reOrder.Split(',');
                if (str.Length != 6)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照接收数据个数不为6！", En_Logout_Type.Other);
                    return false;
                }
                ResPos.IsOK = str[1] == "OK";
                ResPos.ResPosX = float.Parse(str[2]);
                ResPos.ResPosY = float.Parse(str[3]);
                ResPos.ResPosR = float.Parse(str[4]);
                if (!ResPos.IsOK)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照失败！", En_Logout_Type.Other);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }

        }

        public bool AnalysisReOrder(string order, string reOrder,ref int errcode)
        {
            try
            {
                //判断发送指令和接受指令关键字是否一致
                if (order.Substring(0, 2) != reOrder.Substring(0, 2))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照发送数据与接收数据不吻合！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = reOrder.Split(',');
                if (str.Length != 6)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照接收数据个数不为6！", En_Logout_Type.Other);
                    return false;
                }
                ResPos.IsOK = str[1] == "OK";
                ResPos.ResPosX = float.Parse(str[2]);
                ResPos.ResPosY = float.Parse(str[3]);
                ResPos.ResPosR = float.Parse(str[4]);
                errcode = int.Parse(str[5]);
                if (!ResPos.IsOK)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照失败！", En_Logout_Type.Other);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上视觉拍照Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }

        }

        /// <summary>
        /// 坐标转换
        /// </summary>
        /// <returns></returns>
        public float[] ResPosConvert()
        {
            return new float[] { ResPos.ResPosX, ResPos.ResPosY, ResPos.ResPosR };
        }
    }
    public class GetFitPosMode
    {
        /// <summary>
        /// 返回坐标
        /// </summary>
        public CDDResPos ResPos { get; set; } = new CDDResPos();


        /// <summary>
        /// 解析返回字符串
        /// </summary>
        /// <returns></returns>
        public bool AnalysisReOrder(string order, string reOrder)
        {
            try
            {
                //判断发送指令和接受指令关键字是否一致
                if (order.Substring(0, 2) != reOrder.Substring(0, 2))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取贴装位置发送数据与接收数据不吻合！", En_Logout_Type.Other);
                    return false;
                }
                string[] str = reOrder.Split(',');
                if (str.Length != 5)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取贴装位置接收数据个数不为5！", En_Logout_Type.Other);
                    return false;
                }
                ResPos.IsOK = str[1] == "OK";
                ResPos.ResPosX = float.Parse(str[2]);
                ResPos.ResPosY = float.Parse(str[3]);
                ResPos.ResPosR = float.Parse(str[4]);
                if (!ResPos.IsOK)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取贴装位置失败！", En_Logout_Type.Other);
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取贴装位置Tcp接收数据解析失败！", En_Logout_Type.Other);
                return false;
            }

        }
        /// <summary>
        /// 坐标转换
        /// </summary>
        /// <returns></returns>
        public float[] ResPosConvert()
        {
            return new float[] { ResPos.ResPosX, ResPos.ResPosY, ResPos.ResPosR };
        }
    }

    public class CDDResPos
    {
        /// <summary>
        /// 返回X坐标
        /// </summary>
        public float ResPosX { get; set; }
        /// <summary>
        /// 返回Y坐标
        /// </summary>
        public float ResPosY { get; set; }
        /// <summary>
        /// 返回R坐标
        /// </summary>
        public float ResPosR { get; set; }
        /// <summary>
        /// 返回坐标是否OK
        /// </summary>
        public bool IsOK { get; set; }
    }
}
