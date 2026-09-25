using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using HandyControl.Controls;
using QA.Business.Component.Camera;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.ViewModels
{
    [Export("SpotCheckCameraPageViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckCameraPageViewModel : Screen, INotifyPropertyChanged, ISpotPageViewModel
    {
        #region Field
        private IEventAggregator _eventAggregator = null;
        private IWindowManager _windowManager = null;
        private MotionGoogol_Component _mGoogol_Component;
        private CacheParamManager _cacheParamManager;
        private Camera_Component _camera_Component;
        private ParamManager _paramManager;
        private PLC_Component _plc_Component;
        private StepStatus _stepStatus;
        private MotionGoogolParam _mGoogolParam;

        private bool _stopTest = false;

        //private string _basePath = AppDomain.CurrentDomain.BaseDirectory + "CamTest";
        private string _basePath = @"D:\QKProject\Data\CamTest";

        private int nozzleId = 1;

        #endregion

        #region Property
        public override string DisplayName { get; set; } = "相机点检";

        private ushort _OrderID = 5;
        public ushort OrderID
        {
            get { return _OrderID; }
            set { _OrderID = value; }
        }

        private float _testCount = 0;
        public float TestCount
        {
            get => _testCount;
            set
            {
                _testCount = value;
                NotifyOfPropertyChange(() => TestCount);
            }
        }

        private float _axisWaitTime = 0;
        public float AxisWaitTime
        {
            get => _axisWaitTime;
            set
            {
                _axisWaitTime = value;
                NotifyOfPropertyChange(() => AxisWaitTime);
            }
        }

        private string _upCamStaticTestPos;
        public string UpCamStaticTestPos
        {
            get => _upCamStaticTestPos;
            set
            {
                _upCamStaticTestPos = value;
                NotifyOfPropertyChange(() => UpCamStaticTestPos);
            }
        }

        private string _downCamStaticTestPos;
        public string DownCamStaticTestPos
        {
            get => _downCamStaticTestPos;
            set
            {
                _downCamStaticTestPos = value;
                NotifyOfPropertyChange(() => DownCamStaticTestPos);
            }
        }

        private string _upCamDynamicTestStartPos;
        public string UpCamDynamicTestStartPos
        {
            get => _upCamDynamicTestStartPos;
            set
            {
                _upCamDynamicTestStartPos = value;
                NotifyOfPropertyChange(() => UpCamDynamicTestStartPos);
            }
        }

        private string _upCamDynamicTestEndPos;
        public string UpCamDynamicTestEndPos
        {
            get => _upCamDynamicTestEndPos;
            set
            {
                _upCamDynamicTestEndPos = value;
                NotifyOfPropertyChange(() => UpCamDynamicTestEndPos);
            }
        }

        private string _downCamDynamicTestStartPos;
        public string DownCamDynamicTestStartPos
        {
            get => _downCamDynamicTestStartPos;
            set
            {
                _downCamDynamicTestStartPos = value;
                NotifyOfPropertyChange(() => DownCamDynamicTestStartPos);
            }
        }

        private string _downCamDynamicTestEndPos;
        public string DownCamDynamicTestEndPos
        {
            get => _downCamDynamicTestEndPos;
            set
            {
                _downCamDynamicTestEndPos = value;
                NotifyOfPropertyChange(() => DownCamDynamicTestEndPos);
            }
        }

        private string _upAxisTestStartPos;
        public string UpCamAxisTestStartPos
        {
            get => _upAxisTestStartPos;
            set
            {
                _upAxisTestStartPos = value;
                NotifyOfPropertyChange(() => UpCamAxisTestStartPos);
            }
        }

        private string _upAxisTestEndPos;
        public string UpCamAxisTestEndPos
        {
            get => _upAxisTestEndPos;
            set
            {
                _upAxisTestEndPos = value;
                NotifyOfPropertyChange(() => UpCamAxisTestEndPos);
            }
        }

        private string _downAxisTestStartPos;
        public string DownCamAxisTestStartPos
        {
            get => _downAxisTestStartPos;
            set
            {
                _downAxisTestStartPos = value;
                NotifyOfPropertyChange(() => DownCamAxisTestStartPos);
            }
        }

        private string _downAxisTestEndPos;
        public string DownCamAxisTestEndPos
        {
            get => _downAxisTestEndPos;
            set
            {
                _downAxisTestEndPos = value;
                NotifyOfPropertyChange(() => DownCamAxisTestEndPos);
            }
        }





        private string _sendStr;
        public string SendStr
        {
            get => _sendStr;
            set
            {
                _sendStr = value;
                NotifyOfPropertyChange(() => SendStr);
            }
        }

        #endregion

        #region Constructor
        public SpotCheckCameraPageViewModel()
        {
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _mGoogolParam = IoC.Get<MotionGoogolParam>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _paramManager = IoC.Get<ParamManager>();
            _stepStatus = IoC.Get<StepStatus>();
            UpCamStaticTestPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCamStaticTestPos);
            DownCamStaticTestPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.DownCamStaticTestPos);
            UpCamDynamicTestStartPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCamDynamicTestStartPos);
            UpCamDynamicTestEndPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCamDynamicTestEndPos);
            DownCamDynamicTestStartPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.DownCamDynamicTestStartPos);
            DownCamDynamicTestEndPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.DownCamDynamicTestEndPos);

            UpCamAxisTestStartPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCamAxisTestStartPos);
            UpCamAxisTestEndPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCamAxisTestEndPos);
            DownCamAxisTestStartPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.DownCamAxisTestStartPos);
            DownCamAxisTestEndPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.DownCamAxisTestEndPos);

            TestCount = _cacheParamManager.manualPositionParam.TestCount;
            AxisWaitTime = _cacheParamManager.manualPositionParam.AxisWaitTime;

            //if (!Directory.Exists(_basePath))
            //{
            //    Directory.CreateDirectory(_basePath);
            //}
            //if (!Directory.Exists(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd")))
            //{
            //    Directory.CreateDirectory(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd"));
            //}
        }
        #endregion

        #region Method

        /// <summary>
        /// 获取动静态测试点位
        /// </summary>
        /// <param name="isUpCam">是否是上相机</param>
        /// <param name="isStart">是否是起始点位</param>
        /// <returns></returns>
        private float[] GetCamPos(bool isUpCam, bool isStart)
        {
            if (isUpCam)
            {
                if (isStart) return _cacheParamManager.manualPositionParam.UpCamDynamicTestStartPos;
                return _cacheParamManager.manualPositionParam.UpCamDynamicTestEndPos;
            }
            else
            {
                if (isStart) return _cacheParamManager.manualPositionParam.DownCamDynamicTestStartPos;
                return _cacheParamManager.manualPositionParam.DownCamDynamicTestEndPos;
            }
        }
        /// <summary>
        /// 获取轴精度点位
        /// </summary>
        /// <param name="isUpCam">是否是上相机</param>
        /// <param name="isStart">是否是起始点位</param>
        /// <returns></returns>
        private float[] GetAxisPos(bool isUpCam, bool isStart)
        {
            if (isUpCam)
            {
                if (isStart) return _cacheParamManager.manualPositionParam.UpCamAxisTestStartPos;
                return _cacheParamManager.manualPositionParam.UpCamAxisTestEndPos;
            }
            else
            {
                if (isStart) return _cacheParamManager.manualPositionParam.DownCamAxisTestStartPos;
                return _cacheParamManager.manualPositionParam.DownCamAxisTestEndPos;
            }
        }

        /// <summary>
        /// 设置点位字符串
        /// </summary>
        /// <param name="isUpCam"></param>
        /// <param name="isStart"></param>
        /// <param name="pos"></param>

        private void SetPosStr(bool isUpCam, bool isStart, float[] pos)
        {
            if (isUpCam)
            {
                if (isStart)
                {
                    UpCamDynamicTestStartPos = NLogTrace.GetFloatArrayString(pos);
                }
                else
                {
                    UpCamDynamicTestEndPos = NLogTrace.GetFloatArrayString(pos);
                }
            }
            else
            {
                if (isStart)
                {
                    DownCamDynamicTestStartPos = NLogTrace.GetFloatArrayString(pos);
                }
                else
                {
                    DownCamDynamicTestEndPos = NLogTrace.GetFloatArrayString(pos);
                }
            }
        }

        private void SetAxisPosStr(bool isUpCam, bool isStart, float[] pos)
        {
            if (isUpCam)
            {
                if (isStart)
                {
                    UpCamAxisTestStartPos = NLogTrace.GetFloatArrayString(pos);
                }
                else
                {
                    UpCamAxisTestEndPos = NLogTrace.GetFloatArrayString(pos);
                }
            }
            else
            {
                if (isStart)
                {
                    DownCamAxisTestStartPos = NLogTrace.GetFloatArrayString(pos);
                }
                else
                {
                    DownCamAxisTestEndPos = NLogTrace.GetFloatArrayString(pos);
                }
            }
        }

        /// <summary>
        /// 设置动静态测试点位
        /// </summary>
        /// <param name="isUpCam"></param>
        /// <param name="isStart"></param>
        private void SetCamTestPos(bool isUpCam, bool isStart)
        {
            if (MessageBox.Show("确定设置坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = GetCamPos(isUpCam, isStart);
                if (isUpCam)
                {
                    pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                }
                else
                {
                    pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                }
                SetPosStr(isUpCam, isStart, pos);
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设定坐标为：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
            }
        }

        /// <summary>
        /// 设置轴精度测试点位
        /// </summary>
        /// <param name="isUpCam"></param>
        /// <param name="isStart"></param>
        private void SetAxisTestPos(bool isUpCam, bool isStart)
        {
            if (MessageBox.Show("确定设置坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = GetAxisPos(isUpCam, isStart);
                if (isUpCam)
                {
                    pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                }
                else
                {
                    pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                }
                SetAxisPosStr(isUpCam, isStart, pos);
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设定坐标为：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
            }
        }

        /// <summary>
        /// 测试动静态点位移动
        /// </summary>
        /// <param name="isUpCam"></param>
        /// <param name="isStart"></param>

        private async void MoveCamTestPos(bool isUpCam, bool isStart = true)
        {
            if (MessageBox.Show("确定空移吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    float[] pos = GetCamPos(isUpCam, isStart);
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid)) return;
                    _mGoogol_Component.SetAllCylindersUpDown(true);
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    if (isUpCam)
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { pos[0], pos[1] }, true, true)) return;
                    }
                    else
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { pos[0], pos[1] }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1, pos[3], true, true)) return;
                        _mGoogol_Component.SetCylinderUpDown(1, false);
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        /// <summary>
        /// 测试轴精度点位移动
        /// </summary>
        /// <param name="isUpCam"></param>
        /// <param name="isStart"></param>

        private async void MoveAxisTestPos(bool isUpCam, bool isStart = true)
        {
            if (MessageBox.Show("确定空移吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    float[] pos = GetAxisPos(isUpCam, isStart);
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid)) return;
                    _mGoogol_Component.SetAllCylindersUpDown(true);
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    if (isUpCam)
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { pos[0], pos[1] }, true, true)) return;
                    }
                    else
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { pos[0], pos[1] }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1, pos[3], true, true)) return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }


        /// <summary>
        /// 动静态测试
        /// </summary>
        /// <param name="isUpCam"></param>
        /// <param name="isStatic"></param>
        public async void DoTest(bool isUpCam, bool isStatic)
        {
            if (MessageBox.Show("确定执行测试吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    _stopTest = false;
                    // 移动
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid)) return;
                    _mGoogol_Component.SetAllCylindersUpDown(true);
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    // 拍照位
                    float[] posStart = GetCamPos(isUpCam, true);
                    // 移动位
                    float[] posEnd = GetCamPos(isUpCam, false);
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    if (isUpCam)
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { posStart[0], posStart[1] }, true, true)) return;
                    }
                    else
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { posStart[0], posStart[1] }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posStart[2], true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1, posStart[3], true, true)) return;
                        _mGoogol_Component.SetCylinderUpDown(nozzleId, false);
                    }
                    if (isStatic)
                    {
                        Thread.Sleep(500);
                    }
                    // 开始测试
                    List<float[]> testData = new List<float[]>();
                    List<DateTime> testTime = new List<DateTime>();
                    for (int i = 0; i < TestCount; i++)
                    {
                        if (_stopTest)
                        {
                            _stopTest = false;
                            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"停止操作", En_Logout_Type.SpotCheck);
                            MessageBox.Warning("已停止操作！");
                            break;
                        }
                        if (!isStatic)
                        {
                            if (isUpCam)
                            {
                                if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { posEnd[0], posEnd[1] }, true, true)) return;
                                if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { posStart[0], posStart[1] }, true, true)) return;
                            }
                            else
                            {
                                //_mGoogol_Component.SetCylinderUpDown(nozzleId, true);
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { posEnd[0], posStart[1] }, true, true)) return;
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posEnd[2], true, true)) return;
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1, posEnd[3], true, true)) return;

                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { posStart[0], posStart[1] }, true, true)) return;
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posStart[2], true, true)) return;
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1, posStart[3], true, true)) return;
                                //_mGoogol_Component.SetCylinderUpDown(nozzleId, false);

                            }
                            Thread.Sleep(200);
                        }
                        float[] partxya;
                        if (isUpCam)
                        {
                            if (!_camera_Component.UpCdd(1, posStart[0], posStart[1], 0))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"定位吸嘴上物料失败,Index:{i}", En_Logout_Type.SpotCheck);
                                MessageBox.Warning($"定位吸嘴上物料失败,Index:{i}");
                                return;
                            }
                            partxya = _camera_Component.upCDDMode.ResPosConvert();
                        }
                        else
                        {
                            if (!_camera_Component.DownCdd(nozzleId, posStart[0], posStart[1],posStart[3]))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"定位吸嘴上物料失败,Index:{i}", En_Logout_Type.SpotCheck);
                                MessageBox.Warning($"定位吸嘴上物料失败,Index:{i}");
                                return;
                            }
                            partxya = _camera_Component.downCDDMode.ResPosConvert();
                        }
                        testData.Add(partxya);
                        testTime.Add(DateTime.Now);
                    }
                    // 记录数据
                    if (!Directory.Exists(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd")))
                    {
                        Directory.CreateDirectory(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd"));
                    }
                    string s = isUpCam ? "_UpCam" : "_DownCam";
                    s += isStatic ? "_Static" : "_Dynamic";
                    if (!Directory.Exists(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMdd") + s))
                    {
                        Directory.CreateDirectory(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMdd") + s);
                    }
                    string resultPath = _basePath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMdd") + s + "\\" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fff") + ".csv";
                    using (StreamWriter sw = new StreamWriter(resultPath, true))
                    {
                        sw.WriteLine("序号,时间,X,Y,R");
                        for (int i = 0; i < testData.Count; i++)
                        {
                            sw.WriteLine((i + 1).ToString() + "," + testTime[i].ToString("yyyy/MM/dd HH:mm:ss:fff") + "," +
                               testData[i][0].ToString("f3") + "," + testData[i][1].ToString("f3") + "," + testData[i][2].ToString("f3"));
                        }
                    }
                    MessageBox.Success("已写入文件！");
                });
            }
        }

        /// <summary>
        /// 轴精度测试
        /// </summary>
        /// <param name="isUpCam"></param>
        public async void DoAxisTest(bool isUpCam)
        {
            if (MessageBox.Show("确定执行测试吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    _stopTest = false;
                    // 移动
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.High)) return;
                    _mGoogol_Component.SetAllCylindersUpDown(true);
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    // 拍照位/静止位
                    float[] posStart = GetAxisPos(isUpCam, true);
                    // 移动位
                    float[] posEnd = GetAxisPos(isUpCam, false);
                    if (isUpCam)
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { posStart[0], posStart[1] }, true, true)) return;
                    }
                    else
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { posStart[0], posStart[1] }, true, true)) return;
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posStart[2], true, true)) return;
                    }
                    int waitTime = (int)(AxisWaitTime * 1000);
                    Thread.Sleep(waitTime);
                    // 开始测试
                    for (int i = 0; i < TestCount; i++)
                    {
                        if (_stopTest)
                        {
                            _stopTest = false;
                            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"停止操作", En_Logout_Type.SpotCheck);
                            MessageBox.Warning("已停止操作！");
                            break;
                        }
                        if (isUpCam)
                        {
                            if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[] { posEnd[0], posEnd[1] }, true, true)) return;
                            if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[] { posStart[0], posStart[1] }, true, true)) return;
                        }
                        else
                        {
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(new float[] { posEnd[0], posEnd[1], posEnd[2] }, true, true)) return;
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(new float[] { posStart[0], posStart[1], posStart[2] }, true, true)) return;
                        }
                        En_StationNo stationNo = isUpCam ? En_StationNo.StationNo1 : En_StationNo.StationNo2;

                        Thread.Sleep(waitTime);
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }
        /// <summary>
        /// 上相机静态测试
        /// </summary>
        public void DoUpCamStaticTest()
        {
            DoTest(true, true);
        }
        /// <summary>
        /// 下相机吸嘴1静态测试
        /// </summary>
        public void DoDownCamStaticTestNozzel1()
        {
            nozzleId = 1;
            DoTest(false, true);
        }
        /// <summary>
        /// 下相机吸嘴2静态测试
        /// </summary>
        public void DoDownCamStaticTestNozzel2()
        {
            nozzleId = 2;
            DoTest(false, true);
        }

        /// <summary>
        /// 设置上相机起始点位
        /// </summary>
        public void SetUpCamDynamicTestStartPos()
        {
            SetCamTestPos(true, true);
        }
        /// <summary>
        /// 移动到上相机起始点位
        /// </summary>
        public void MoveUpCamDynamicTestStartPos()
        {
            MoveCamTestPos(true, true);
        }
        /// <summary>
        /// 设置上相机结束点位
        /// </summary>
        public void SetUpCamDynamicTestEndPos()
        {
            SetCamTestPos(true, false);
        }
        /// <summary>
        /// 移动到上相机结束点位
        /// </summary>
        public void MoveUpCamDynamicTestEndPos()
        {
            MoveCamTestPos(true, false);
        }
        /// <summary>
        /// 上相机动态测试
        /// </summary>
        public void DoUpCamDynamicTest()
        {
            DoTest(true, false);
        }
        /// <summary>
        /// 设置相机轴-轴精度开始点位
        /// </summary>
        public void SetUpCamAxisTestStartPos()
        {
            SetAxisTestPos(true, true);
        }
        /// <summary>
        /// 移动到相机轴-轴精度开始点位
        /// </summary>
        public void MoveUpCamAxisTestStartPos()
        {
            MoveAxisTestPos(true, true);
        }
        /// <summary>
        /// 设置相机轴-轴精度结束点位
        /// </summary>
        public void SetUpCamAxisTestEndPos()
        {
            SetAxisTestPos(true, false);
        }
        /// <summary>
        /// 移动到相机轴-轴精度结束点位
        /// </summary>
        public void MoveUpCamAxisTestEndPos()
        {
            MoveAxisTestPos(true, false);
        }
        /// <summary>
        /// 设置吸嘴轴-轴精度开始点位
        /// </summary>
        public void SetDownCamAxisTestStartPos()
        {
            SetAxisTestPos(false, true);
        }
        /// <summary>
        /// 移动到吸嘴轴-轴精度开始点位
        /// </summary>
        public void MoveDownCamAxisTestStartPos()
        {
            MoveAxisTestPos(false, true);
        }
        /// <summary>
        /// 设置吸嘴轴-轴精度结束点位
        /// </summary>
        public void SetDownCamAxisTestEndPos()
        {
            SetAxisTestPos(false, false);
        }
        /// <summary>
        /// 移动到吸嘴轴-轴精度结束点位
        /// </summary>
        public void MoveDownCamAxisTestEndPos()
        {
            MoveAxisTestPos(false, false);
        }
        /// <summary>
        /// 相机轴精度测试
        /// </summary>
        public void DoUpAxisTest()
        {
            DoAxisTest(true);
        }
        /// <summary>
        /// 设置下相机起始点位
        /// </summary>
        public void SetDownCamDynamicTestStartPos()
        {
            SetCamTestPos(false, true);
        }
        /// <summary>
        /// 移动到下相机起始点位
        /// </summary>
        public void MoveDownCamDynamicTestStartPos()
        {
            MoveCamTestPos(false, true);
        }
        /// <summary>
        /// 设置下相机结束点位
        /// </summary>
        public void SetDownCamDynamicTestEndPos()
        {
            SetCamTestPos(false, false);
        }
        /// <summary>
        /// 移动到下相机结束点位
        /// </summary>
        public void MoveDownCamDynamicTestEndPos()
        {
            MoveCamTestPos(false, false);
        }
        /// <summary>
        /// 吸嘴1动态测试
        /// </summary>
        public void DoDownCamDynamicTestNozzel1()
        {
            nozzleId = 1;
            DoTest(false, false);
        }
        /// <summary>
        /// 吸嘴2动态测试
        /// </summary>
        public void DoDownCamDynamicTestNozzel2()
        {
            nozzleId = 2;
            DoTest(false, false);
        }
        /// <summary>
        /// 吸嘴轴轴精度测试
        /// </summary>
        public void DoDownAxisTest()
        {
            DoAxisTest(false);
        }

        /// <summary>
        /// 飞达1上相机静态测试
        /// </summary>
        public void DoFeeder1CamStaticTest()
        {
            FeederCamTest(FeederId.左飞达, true);
        }

        /// <summary>
        /// 飞达1上相机动态测试
        /// </summary>
        public void DoFeeder1CamDynamicTest()
        {
            FeederCamTest(FeederId.左飞达, false);
        }

        /// <summary>
        /// 飞达1上相机气缸精度
        /// </summary>
        public void DoFeeder1CylinderTest()
        {
            FeederCylinderTest(FeederId.左飞达);
        }

        /// <summary>
        /// 飞达2上相机静态测试
        /// </summary>
        public void DoFeeder2CamStaticTest()
        {
            FeederCamTest(FeederId.右飞达, true);
        }

        /// <summary>
        /// 飞达2上相机动态测试
        /// </summary>
        public void DoFeeder2CamDynamicTest()
        {
            FeederCamTest(FeederId.右飞达, false);
        }

        /// <summary>
        /// 飞达2上相机气缸精度
        /// </summary>
        public void DoFeeder2CylinderTest()
        {
            FeederCylinderTest(FeederId.右飞达);
        }

        /// <summary>
        /// 飞达1上相机测试,等待时间为轴精度停止时间，测试次数为测试次数
        /// </summary>
        /// <param name="isUpCam"></param>
        public async void FeederCamTest(FeederId id, bool isStatic)
        {
            if (MessageBox.Show("确定执行测试吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    _stopTest = false;
                    // 移动气缸
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    _plc_Component.FeederCDDCylinderSS(id);
                    if (isStatic)
                    {
                        Thread.Sleep(500);
                    }
                    // 开始测试
                    List<float[]> testData = new List<float[]>();
                    List<DateTime> testTime = new List<DateTime>();
                    for (int i = 0; i < TestCount; i++)
                    {
                        if (_stopTest)
                        {
                            _stopTest = false;
                            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"停止操作", En_Logout_Type.SpotCheck);
                            MessageBox.Warning("已停止操作！");
                            break;
                        }
                        if (!isStatic)
                        {
                            if (id == FeederId.左飞达)
                            {
                                _plc_Component.FeederCDDCylinderSS(FeederId.右飞达);
                                _plc_Component.FeederCDDCylinderSS(FeederId.左飞达);
                            }
                            else
                            {
                                _plc_Component.FeederCDDCylinderSS(FeederId.左飞达);
                                _plc_Component.FeederCDDCylinderSS(FeederId.右飞达);
                            }
                            Thread.Sleep(200);
                        }
                        if (id == FeederId.左飞达)
                        {
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder1Pos, true))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[1]}", En_Logout_Type.Alarm, true);
                                return;
                            }

                        }
                        else
                        {
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[1]}", En_Logout_Type.Alarm, true);
                                return;
                            }
                        }
                        float[] partxya;
                        if (!_camera_Component.FeederCdd())
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"定位吸嘴上物料失败,Index:{i}", En_Logout_Type.SpotCheck);
                            MessageBox.Warning($"定位吸嘴上物料失败,Index:{i}");
                            return;
                        }

                        partxya = _camera_Component.feederCDDMode.GetFeederCamResPosConvert();
                        testData.Add(partxya);
                        testTime.Add(DateTime.Now);
                    }
                    // 记录数据
                    if (!Directory.Exists(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd")))
                    {
                        Directory.CreateDirectory(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd"));
                    }
                    string s = FeederId.左飞达 == id ? "_LeftFeederCam" : "_RightFeederCam";
                    s += isStatic ? "_Static" : "_Dynamic";
                    if (!Directory.Exists(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMdd") + s))
                    {
                        Directory.CreateDirectory(_basePath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMdd") + s);
                    }
                    string resultPath = _basePath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMdd") + s + "\\" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-fff") + ".csv";
                    using (StreamWriter sw = new StreamWriter(resultPath, true))
                    {
                        sw.WriteLine("序号,时间,料1吸嘴1X,料1吸嘴1Y,料1吸嘴1R,料1吸嘴2X,料1吸嘴2Y,料1吸嘴2R," +
                            "料2吸嘴1X,料2吸嘴1Y,料2吸嘴1R,料2吸嘴2X,料2吸嘴2Y,料2吸嘴2R");
                        for (int i = 0; i < testData.Count; i++)
                        {
                            sw.WriteLine((i + 1).ToString() + "," + testTime[i].ToString("yyyy/MM/dd HH:mm:ss:fff") + "," +
                               testData[i][0].ToString("f3") + "," + testData[i][1].ToString("f3") + "," + testData[i][2].ToString("f3") + "," +
                               testData[i][3].ToString("f3") + "," + testData[i][4].ToString("f3") + "," + testData[i][5].ToString("f3") + "," +
                               testData[i][6].ToString("f3") + "," + testData[i][7].ToString("f3") + "," + testData[i][8].ToString("f3") + "," +
                               testData[i][9].ToString("f3") + "," + testData[i][10].ToString("f3") + "," + testData[i][11].ToString("f3"));
                        }
                    }
                    MessageBox.Success("已写入文件！");
                });
            }
        }

        /// <summary>
        /// 飞达上相机轴精度测试,等待时间为轴精度停止时间，测试次数为测试次数
        /// </summary>
        /// <param name="isUpCam"></param>
        public async void FeederCylinderTest(FeederId id)
        {
            if (MessageBox.Show("确定执行测试吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    _stopTest = false;
                    // 移动
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    _plc_Component.FeederCDDCylinderSS(id);
                    int waitTime = (int)(AxisWaitTime * 1000);
                    Thread.Sleep(waitTime);
                    // 开始测试
                    for (int i = 0; i < TestCount; i++)
                    {
                        if (_stopTest)
                        {
                            _stopTest = false;
                            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"停止操作", En_Logout_Type.SpotCheck);
                            MessageBox.Warning("已停止操作！");
                            break;
                        }
                        if (id == FeederId.左飞达)
                        {
                            _plc_Component.FeederCDDCylinderSS(FeederId.右飞达);
                            _plc_Component.FeederCDDCylinderSS(FeederId.左飞达);
                        }
                        else
                        {
                            _plc_Component.FeederCDDCylinderSS(FeederId.左飞达);
                            _plc_Component.FeederCDDCylinderSS(FeederId.右飞达);
                        }

                        Thread.Sleep(waitTime);
                    }
                    MessageBox.Success($"{id}轴相机动静态测试完成！");
                });
            }
        }

        /// <summary>
        /// 停止测试
        /// </summary>
        public void StopTest()
        {
            _stopTest = true;
        }

        /// <summary>
        /// 查看数据
        /// </summary>
        public void ReadData()
        {
            //string dir = AppDomain.CurrentDomain.BaseDirectory + "CamTest";
            string dir = @"D:\QKProject\Data\CamTest";

            if (Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start(dir);
            }
            else
            {
                MessageBox.Warning("未检测到数据！");
            }
        }
        /// <summary>
        /// 设置次数
        /// </summary>
        /// <param name="obj"></param>
        public void NumUDTestCountChanged(object obj)
        {
            NumericUpDown val = (NumericUpDown)obj;

            _cacheParamManager.manualPositionParam.TestCount = (int)val.Value;

            _cacheParamManager.SaveManualPositionParam();
        }
        /// <summary>
        /// 设置停顿时间
        /// </summary>
        /// <param name="obj"></param>
        public void NumUDTestCavityChanged(object obj)
        {
            NumericUpDown val = (NumericUpDown)obj;

            _cacheParamManager.manualPositionParam.AxisWaitTime = (float)val.Value;

            _cacheParamManager.SaveManualPositionParam();
        }

        public void SendCameraTest()
        {
            string resstr = string.Empty;
            if (!_camera_Component.SendData(SendStr, ref resstr))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"视觉通信失败，指令:{SendStr}", En_Logout_Type.Alarm, true);
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"视觉通信返回:{resstr}", En_Logout_Type.Run, true);
            }
        }

        public async Task TakeThrowTapeTestAsync()
        {
            //判断哪些吸嘴要取料
            int SelNozzleNo ;
            MessageBoxResult messageBoxResult = MessageBox.Show("选择吸嘴，是->吸嘴1，否->吸嘴2，取消->双吸嘴", "提示信息", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            await Task.Run(() =>
            {
                //初始化
                if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                {
                    MessageBox.Warning("轴系在运动，等静止再操作！");
                    return;
                }
                if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Work) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上抬全部气缸失败(Nâng toàn bộ xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo1运动到[X1,Y1](0,0)失败(Chiến dịch StationNo1 thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    SelNozzleNo = 1;
                    GetOneTape(SelNozzleNo);
                }
                else if (messageBoxResult == MessageBoxResult.No)
                {
                    SelNozzleNo = 2;
                    GetOneTape(SelNozzleNo);
                }
                else
                {
                    for (int i = 0; i < TestCount; i++)
                    {
                        //上视觉拍照
                        /*******新增Feeder拍照*******/
                        FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                        float[] posxya;
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(feederId == FeederId.左飞达 ? _cacheParamManager.manualPositionParam.AxisFeeder1Pos : _cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                        {
                            MessageBox.Error($"X2移动到0失败！");
                            return;
                        }
                        //等待物料到位
                        if (!_stepStatus.TrigFeederConveyTape(true))
                        {
                            return;
                        }
                        if (!_camera_Component.FeederCdd())
                        {
                            MessageBox.Error($"飞达拍照失败！");
                            return;
                        }
                        if (_camera_Component.feederCDDMode.GetTapeOKNum() == 2)
                        {
                            for (SelNozzleNo = 1; SelNozzleNo <= 2; SelNozzleNo++)
                            {
                                float[] pos = _stepStatus.FeederSingleTapePos(SelNozzleNo, (SelNozzleNo - 1) % 2);
                                posxya = _camera_Component.GetPickPos(SelNozzleNo);
                                pos[0] = posxya[0];
                                pos[1] = posxya[1];
                                pos[3] = posxya[2];
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2R((En_AxisNum)(SelNozzleNo + 4), new float[3] { pos[0], pos[1], pos[3] }, true, true) || _mGoogol_Component.Exit())
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick XYR坐标失败，Axis[X2,Y2]:{NLogTrace.GetFloatArrayString(pos)}(Chuyển đến Feeder Pick XYR Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                    return;
                                }
                                //单移R2轴
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R2, pos[3], false, true) || _mGoogol_Component.Exit())
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick R2坐标失败，Axis[R2]:{NLogTrace.GetFloatArrayString(pos)}(Chuyển đến Feeder Pick XYR Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                    return;
                                }
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, false) || _mGoogol_Component.Exit())
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick Z坐标失败，Axis[Z2]:{pos[2].ToString("f2")}(Chuyển đến Feeder Pick Z Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                    return;
                                }
                                if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Mở rộng xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                                    return;
                                }
                                if (!_mGoogol_Component.SetVacuum(SelNozzleNo, true, true) || _mGoogol_Component.Exit())
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}吸失败(Xi lanh hút thất bại)", En_Logout_Type.SpotCheck, true);
                                    return;
                                }
                                //等待一定时间
                                Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
                                if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, true) || _mGoogol_Component.Exit())
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                                    return;
                                }
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                                    return;
                                }
                            }
                            //下视觉拍照
                            if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                            {
                                MessageBox.Warning("轴系在运动，等静止再操作！");
                                return;
                            }
                            if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Work) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上抬全部气缸失败(Nâng toàn bộ xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo1运动到[X1,Y1](0,0)失败(Chiến dịch StationNo1 thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                        }
                        else if (_camera_Component.feederCDDMode.GetTapeOKNum() == 1)
                        {
                            //先取吸嘴1
                            SelNozzleNo = 1;
                            float[] pos = _stepStatus.FeederSingleTapePos(SelNozzleNo, (SelNozzleNo - 1) % 2);
                            posxya = _camera_Component.GetPickPos(SelNozzleNo);
                            pos[0] = posxya[0];
                            pos[1] = posxya[1];
                            pos[3] = posxya[2];
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2R((En_AxisNum)(SelNozzleNo + 4), new float[3] { pos[0], pos[1], pos[3] }, true, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick XYR坐标失败，Axis[X2,Y2]:{NLogTrace.GetFloatArrayString(pos)}(Chuyển đến Feeder Pick XYR Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, false) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick Z坐标失败，Axis[Z2]:{pos[2].ToString("f2")}(Chuyển đến Feeder Pick Z Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Mở rộng xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.SetVacuum(SelNozzleNo, true, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}吸失败(Xi lanh hút thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            //等待一定时间
                            Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
                            if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            //飞达再拍一次
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(feederId == FeederId.左飞达 ? _cacheParamManager.manualPositionParam.AxisFeeder1Pos : _cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                            {
                                MessageBox.Error($"X2移动到0失败！");
                                return;
                            }
                            //等待物料到位
                            if (!_stepStatus.TrigFeederConveyTape(true))
                            {
                                return;
                            }
                            if (!_camera_Component.FeederCdd())
                            {
                                MessageBox.Error($"飞达拍照失败！");
                                return;
                            }
                            //再单取吸嘴2
                            SelNozzleNo = 2;
                            posxya = _camera_Component.GetPickPos(SelNozzleNo);
                            pos[0] = posxya[0];
                            pos[1] = posxya[1];
                            pos[3] = posxya[2];
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2R((En_AxisNum)(SelNozzleNo + 4), new float[3] { pos[0], pos[1], pos[3] }, true, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick XYR坐标失败，Axis[X2,Y2]:{NLogTrace.GetFloatArrayString(pos)}(Chuyển đến Feeder Pick XYR Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, false) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick Z坐标失败，Axis[Z2]:{pos[2].ToString("f2")}(Chuyển đến Feeder Pick Z Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Mở rộng xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            Thread.Sleep(50);
                            if (!_mGoogol_Component.SetVacuum(SelNozzleNo, true, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}吸失败(Xi lanh hút thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            //等待一定时间
                            Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
                            if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }

                        }
                        else
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"飞达无料", En_Logout_Type.SpotCheck, true);
                            return;
                        }
                        for (SelNozzleNo = 1; SelNozzleNo <= 2; SelNozzleNo++)
                        {
                            float[] pos = _stepStatus.FeederSingleTapePos(SelNozzleNo, (SelNozzleNo - 1) % 2);
                            pos = _stepStatus.DownCameraPos(SelNozzleNo);
                            En_AxisNum en_AxisNum;
                            if (SelNozzleNo == 1)
                            {
                                en_AxisNum = En_AxisNum.R1;
                            }
                            else
                            {
                                en_AxisNum = En_AxisNum.R2;
                            }
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(en_AxisNum, pos, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运行到吸嘴下视觉位置失败，Axis[X2,Y2,Z2,R1,R2]:{NLogTrace.GetFloatArrayString(pos)}(Chạy đến vị trí trực quan dưới miệng hút không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            //单移R2轴
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R2, pos[3], false, true) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick R2坐标失败，Axis[R2]:{NLogTrace.GetFloatArrayString(pos)}(Chuyển đến Feeder Pick XYR Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_mGoogol_Component.SetAllCylindersUpDown(false) || _mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Thất bại dưới xi lanh)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            if (!_camera_Component.DownCdd(SelNozzleNo, pos[0], pos[1], pos[3]))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"定位{SelNozzleNo.ToString()}号吸嘴上的物料失败，Axis[X2,Y2,Z2,R{SelNozzleNo.ToString()}]:{NLogTrace.GetFloatArrayString(pos)}(Định vị vật liệu trên miệng hút thất bại)", En_Logout_Type.SpotCheck, true);
                                MessageBox.Warning("下视觉定位失败");
                                return;
                            }
                        }
                        if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"全部气缸上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                            return;
                        }
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                            return;
                        }
                        //抛料
                        HashSet<int> throwTapeNozzleNoSet = new HashSet<int>();
                        SelNozzleNo = 1;
                        throwTapeNozzleNoSet.Add(SelNozzleNo);
                        SelNozzleNo = 2;
                        throwTapeNozzleNoSet.Add(SelNozzleNo);
                        if (!_stepStatus.ThrowTapes(throwTapeNozzleNoSet))
                        {
                            MessageBox.Error("抛料失败！");
                            return;
                        }
                        if (!_mGoogol_Component.SetAllCylindersUpDown(true))
                        {
                            MessageBox.Error("所有气缸上失败！");
                            return;
                        }
                    }
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true))
                {
                    MessageBox.Error("Z2回零失败！");
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[] { 0, 120 }, true))
                {
                    MessageBox.Error("回安全位置失败！");
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[] { 0, 0 }, true))
                {
                    MessageBox.Error("回原点失败！");
                    return;
                }
                MessageBox.Show("测试完成！");

            });

        }
        public bool GetOneTape(int SelNozzleNo)
        {
            HashSet<int> throwTapeNozzleNoSet = new HashSet<int>();
            for (int i = 0; i < TestCount; i++)
            {
                //上视觉拍照
                /*******新增Feeder拍照*******/
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(feederId == FeederId.左飞达 ? _cacheParamManager.manualPositionParam.AxisFeeder1Pos : _cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                {
                    MessageBox.Error($"X2移动到0失败！");
                    return false;
                }
                //等待物料到位
                if (!_stepStatus.TrigFeederConveyTape(true))
                {
                    return false;
                }
                if (!_camera_Component.FeederCdd())
                {
                    MessageBox.Error($"飞达拍照失败！");
                    return false;
                }
                float[] pos = _stepStatus.FeederSingleTapePos(SelNozzleNo, (SelNozzleNo - 1) % 2);
                float[] posxya = _camera_Component.GetPickPos(SelNozzleNo);
                pos[0] = posxya[0];
                pos[1] = posxya[1];
                pos[3] = posxya[2];
                if (!_mGoogol_Component.MoveAbsoluteX2Y2R((En_AxisNum)(SelNozzleNo + 4), new float[3] { pos[0], pos[1], pos[3] }, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick XYR坐标失败，Axis[X2,Y2]:{NLogTrace.GetFloatArrayString(pos)}(Chuyển đến Feeder Pick XYR Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, false) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick Z坐标失败，Axis[Z2]:{pos[2].ToString("f2")}(Chuyển đến Feeder Pick Z Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Mở rộng xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.SetVacuum(SelNozzleNo, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}吸失败(Xi lanh hút thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                //等待一定时间
                Thread.Sleep(50);
                if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                //下视觉拍照
                if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                {
                    MessageBox.Warning("轴系在运动，等静止再操作！");
                    return false;
                }
                if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Work) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上抬全部气缸失败(Nâng toàn bộ xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo1运动到[X1,Y1](0,0)失败(Chiến dịch StationNo1 thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                pos = _stepStatus.DownCameraPos(SelNozzleNo);
                En_AxisNum en_AxisNum;
                if (SelNozzleNo == 1)
                {
                    en_AxisNum = En_AxisNum.R1;
                }
                else
                {
                    en_AxisNum = En_AxisNum.R2;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(en_AxisNum, pos, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运行到吸嘴下视觉位置失败，Axis[X2,Y2,Z2,R1,R2]:{NLogTrace.GetFloatArrayString(pos)}(Chạy đến vị trí trực quan dưới miệng hút không thành công)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Thất bại dưới xi lanh)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                Thread.Sleep(50);
                if (!_camera_Component.DownCdd(SelNozzleNo, pos[0], pos[1], pos[3]))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"定位{SelNozzleNo.ToString()}号吸嘴上的物料失败，Axis[X2,Y2,Z2,R{SelNozzleNo.ToString()}]:{NLogTrace.GetFloatArrayString(pos)}(Định vị vật liệu trên miệng hút thất bại)", En_Logout_Type.SpotCheck, true);
                    MessageBox.Warning("下视觉定位失败");
                    return false;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                    return false;
                }
                //抛料
                throwTapeNozzleNoSet.Add(SelNozzleNo);
                if (!_stepStatus.ThrowTapes(throwTapeNozzleNoSet))
                {
                    MessageBox.Error("抛料失败！");
                    return false;
                }
                if (!_mGoogol_Component.SetAllCylindersUpDown(true))
                {
                    MessageBox.Error("所有气缸上失败！");
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true))
                {
                    MessageBox.Error("Z2回零失败！");
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[] { 0, 120 }, true))
                {
                    MessageBox.Error("回安全位置失败！");
                    return false;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[] { 0, 0 }, true))
                {
                    MessageBox.Error("回原点失败！");
                    return false;
                }

            }
            return true;
        }
        #endregion
    }
}
