using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using HandyControl.Controls;
using QA.Business.Component.Camera;
using QA.Business.Component.Motion.Googol;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.Calibration.ViewModels
{
    //[Export(typeof(ICalibrationViewModel))]
    public class NinePointCalibViewModel : Screen, INotifyPropertyChanged, ICalibrationViewModel, IHandle<LoginSuccessMessage>
    {
        #region Field
        private readonly IEventAggregator _eventAggregator;
        private Camera_Component _camera_Component;
        private ParamManager _paramManager;
        private MotionGoogol_Component _mGoogol_Component;
        private CacheParamManager _cacheParamManager;
        private MyUserManager _myUserManager;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "9点标定";

        private ushort _OrderID = 3;
        public ushort OrderID
        {
            get { return _OrderID; }
            set { _OrderID = value; }
        }

        private string _curCamNo1NineCalibPos_1;
        public string CurCamNo1NineCalibPos_1
        {
            get => _curCamNo1NineCalibPos_1;
            set
            {
                _curCamNo1NineCalibPos_1 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_1);
            }
        }

        private string _curCamNo1NineCalibPos_2;
        public string CurCamNo1NineCalibPos_2
        {
            get => _curCamNo1NineCalibPos_2;
            set
            {
                _curCamNo1NineCalibPos_2 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_2);
            }
        }

        private string _curCamNo1NineCalibPos_3;
        public string CurCamNo1NineCalibPos_3
        {
            get => _curCamNo1NineCalibPos_3;
            set
            {
                _curCamNo1NineCalibPos_3 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_3);
            }
        }

        private string _curCamNo1NineCalibPos_4;
        public string CurCamNo1NineCalibPos_4
        {
            get => _curCamNo1NineCalibPos_4;
            set
            {
                _curCamNo1NineCalibPos_4 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_4);
            }
        }

        private string _curCamNo1NineCalibPos_5;
        public string CurCamNo1NineCalibPos_5
        {
            get => _curCamNo1NineCalibPos_5;
            set
            {
                _curCamNo1NineCalibPos_5 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_5);
            }
        }

        private string _curCamNo1NineCalibPos_6;
        public string CurCamNo1NineCalibPos_6
        {
            get => _curCamNo1NineCalibPos_6;
            set
            {
                _curCamNo1NineCalibPos_6 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_6);
            }
        }

        private string _curCamNo1NineCalibPos_7;
        public string CurCamNo1NineCalibPos_7
        {
            get => _curCamNo1NineCalibPos_7;
            set
            {
                _curCamNo1NineCalibPos_7 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_7);
            }
        }

        private string _curCamNo1NineCalibPos_8;
        public string CurCamNo1NineCalibPos_8
        {
            get => _curCamNo1NineCalibPos_8;
            set
            {
                _curCamNo1NineCalibPos_8 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_8);
            }
        }

        private string _curCamNo1NineCalibPos_9;
        public string CurCamNo1NineCalibPos_9
        {
            get => _curCamNo1NineCalibPos_9;
            set
            {
                _curCamNo1NineCalibPos_9 = value;
                NotifyOfPropertyChange(() => CurCamNo1NineCalibPos_9);
            }
        }

        private string _curCamNo2NozzleNo2Pos;
        public string CurCamNo2NozzleNo2Pos
        {
            get => _curCamNo2NozzleNo2Pos;
            set
            {
                _curCamNo2NozzleNo2Pos = value;
                NotifyOfPropertyChange(() => CurCamNo2NozzleNo2Pos);
            }
        }

        private string _curCamNo2NozzleNo3Pos;
        public string CurCamNo2NozzleNo3Pos
        {
            get => _curCamNo2NozzleNo3Pos;
            set
            {
                _curCamNo2NozzleNo3Pos = value;
                NotifyOfPropertyChange(() => CurCamNo2NozzleNo3Pos);
            }
        }

        private string _curCamNo2NozzleNo4Pos;
        public string CurCamNo2NozzleNo4Pos
        {
            get => _curCamNo2NozzleNo4Pos;
            set
            {
                _curCamNo2NozzleNo4Pos = value;
                NotifyOfPropertyChange(() => CurCamNo2NozzleNo4Pos);
            }
        }

        private bool _enableButtons = false;
        public bool EnableButtons
        {
            get => _enableButtons;
            set
            {
                _enableButtons = value;
                NotifyOfPropertyChange(() => EnableButtons);
            }
        }
        #endregion

        #region Constructor
        public NinePointCalibViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _paramManager = IoC.Get<ParamManager>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _myUserManager = IoC.Get<MyUserManager>();

            CurCamNo1NineCalibPos_1 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1);
            CurCamNo1NineCalibPos_2 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2);
            CurCamNo1NineCalibPos_3 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3);
            CurCamNo1NineCalibPos_4 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4);
            CurCamNo1NineCalibPos_5 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5);
            CurCamNo1NineCalibPos_6 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6);
            CurCamNo1NineCalibPos_7 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7);
            CurCamNo1NineCalibPos_8 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8);
            CurCamNo1NineCalibPos_9 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9);
            CurCamNo2NozzleNo2Pos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos);
            CurCamNo2NozzleNo3Pos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos);
            CurCamNo2NozzleNo4Pos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos);
        }
        #endregion

        #region Handle
        public void Handle(LoginSuccessMessage message)
        {
            EnableButtons = _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeManager;
        }
        #endregion

        #region Method
        /// <summary>
        /// 上相机9点标定-点位设置
        /// </summary>
        /// <param name="obj"></param>
        public void SetCalibNinePointPosToUpCameraNo1(object obj)
        {
            int index = Convert.ToInt32(obj);
            if (MessageBox.Show($"确定设置上相机9点标定{index}号坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos;
                if (index == 1)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1[3] = 0;
                    CurCamNo1NineCalibPos_1 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1;
                }
                else if (index == 2)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2[3] = 0;
                    CurCamNo1NineCalibPos_2 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2;
                }
                else if (index == 3)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3[3] = 0;
                    CurCamNo1NineCalibPos_3 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3;
                }
                else if (index == 4)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4[3] = 0;
                    CurCamNo1NineCalibPos_4 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4;
                }
                else if (index == 5)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5[3] = 0;
                    CurCamNo1NineCalibPos_5 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5;
                }
                else if (index == 6)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6[3] = 0;
                    CurCamNo1NineCalibPos_6 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6;
                }
                else if (index == 7)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7[3] = 0;
                    CurCamNo1NineCalibPos_7 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7;
                }
                else if (index == 8)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8[3] = 0;
                    CurCamNo1NineCalibPos_8 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8;
                }
                else if (index == 9)
                {
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9[2] = 0;
                    _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9[3] = 0;
                    CurCamNo1NineCalibPos_9 = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9);
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetCalibNinePointPosToUpCameraNo1({index})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设置上相机9点标定{index}号坐标：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
            }
        }

        /// <summary>
        /// 上相机9点标定-点位移动
        /// </summary>
        public async void MoveSpaceCalibNinePointPosToUpCameraNo1(object obj)
        {
            int index = Convert.ToInt32(obj);
            if (MessageBox.Show($"确定移动到上相机9点标定{index}号坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos;
                if (index == 1)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1;
                }
                else if (index == 2)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2;
                }
                else if (index == 3)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3;
                }
                else if (index == 4)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4;
                }
                else if (index == 5)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5;
                }
                else if (index == 6)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6;
                }
                else if (index == 7)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7;
                }
                else if (index == 8)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8;
                }
                else if (index == 9)
                {
                    pos = _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceCalibNinePointPosToUpCameraNo1({index})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.CleanAlarm())
                    {
                        MessageBox.Error("清除报警失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid))
                    {
                        MessageBox.Error("设置轴速为示教中速失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
                    {
                        MessageBox.Error("Z2移动到0失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetAllCylindersUpDown(true))
                    {
                        MessageBox.Error("所有气缸上失败！");
                        return;
                    }
                    if (!_mGoogol_Component.Station2InSafeArea() && !_mGoogol_Component.MoveY2ToSafePos())
                    {
                        MessageBox.Error("Y2移动到安全位置失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { pos[0], pos[1] }, true))
                    {
                        MessageBox.Error("移动失败！");
                        return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        /// <summary>
        /// 上相机9点标定-执行标定
        /// </summary>
        public async void CalibNinePoint()
        {
            MessageBoxResult mbr = MessageBox.Show($"确定执行上相机9点标定吗 ?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (mbr == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Show("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.CleanAlarm())
                    {
                        MessageBox.Error("清除报警失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid))
                    {
                        MessageBox.Error("设置轴速为示教中速失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
                    {
                        MessageBox.Error("Z2移动到0失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetAllCylindersUpDown(true))
                    {
                        MessageBox.Error("所有气缸上失败！");
                        return;
                    }
                    if (!_mGoogol_Component.Station2InSafeArea() && !_mGoogol_Component.MoveY2ToSafePos())
                    {
                        MessageBox.Error("Y2移动到安全位置失败！");
                        return;
                    }
                    int cmdEncode =/* _paramManager.MESParam.TapeNames == TapeNames.Alert_Bumper ? 7 : */6;
                    if (!_camera_Component.SendScCalib(cmdEncode, 9))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"CalibNinePoint标定启动失败", En_Logout_Type.SpotCheck);
                        MessageBox.Show("标定启动失败");
                        return;
                    }
                    if (!_mGoogol_Component.SetUpLightHX(true))
                    {
                        MessageBox.Error("上相机环形光源打开失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetUpLightTZ(true))
                    {
                        MessageBox.Error("上相机同轴光源打开失败！");
                        return;
                    }
                    float[][] calib9Points = new[] {
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_1,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_2,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_3,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_4,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_5,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_6,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_7,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_8,
                        _cacheParamManager.manualPositionParam.UpCameraNo1NineCalibPos_9,
                    };
                    for (int i = 0; i < calib9Points.Length; i++)
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { calib9Points[i][0], calib9Points[i][1] }, true))
                        {
                            MessageBox.Error($"移动到9点标定{i + 1}号坐标失败！");
                            return;
                        }
                        Thread.Sleep(300);
                        if (!_camera_Component.CalibProcess(cmdEncode, calib9Points[i][0], calib9Points[i][1], 0))
                        {
                            MessageBox.Error($"9点标定{i + 1}号坐标标定失败");
                            return;
                        }
                    }
                    if (!_mGoogol_Component.SetUpLightHX(false))
                    {
                        MessageBox.Error("上相机环形光源关闭失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetUpLightTZ(false))
                    {
                        MessageBox.Error("上相机同轴光源关闭失败！");
                        return;
                    }
                    if (!_camera_Component.SendEcCalib())
                    {
                        MessageBox.Error("发送上相机标定完成指令失败！");
                        return;
                    }
                    MessageBox.Info("上相机标定已完成，请等待视觉软件计算完毕！\n此弹窗可直接关闭");
                });
            }
        }

        /// <summary>
        /// 下相机11点标定-点位设置
        /// </summary>
        /// <param name="obj"></param>
        public void SetCalibElevenPointPosToDownCamera_Nozzle(object obj)
        {
            int index = Convert.ToInt32(obj);
            if (MessageBox.Show($"确定设置{index}号吸嘴下相机标定坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos;
                if (index == 2)
                {
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    CurCamNo2NozzleNo2Pos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos);
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos;
                }
                else if (index == 3)
                {
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    CurCamNo2NozzleNo3Pos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos);
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos;
                }
                else if (index == 4)
                {
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    CurCamNo2NozzleNo4Pos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos);
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetCalibElevenPointPosToDownCamera_Nozzle({index})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设置{index}号吸嘴下相机标定坐标：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
            }
        }

        /// <summary>
        /// 下相机11点标定-点位移动
        /// </summary>
        /// <param name="obj"></param>
        public async void MoveSpaceCalibElevenPointPosToDownCamera_Nozzle(object obj)
        {
            int index = Convert.ToInt32(obj);
            if (MessageBox.Show($"确定移动到{index}号吸嘴下相机标定坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos;
                if (index == 2)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos;
                }
                else if (index == 3)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos;
                }
                else if (index == 4)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceCalibElevenPointPosToDownCamera_Nozzle({index})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.CleanAlarm())
                    {
                        MessageBox.Error("清除报警失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid))
                    {
                        MessageBox.Error("设置轴速为示教中速失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
                    {
                        MessageBox.Error("Z2移动到0失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetAllCylindersUpDown(true))
                    {
                        MessageBox.Error("所有气缸上失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Y1, 0, true))
                    {
                        MessageBox.Error("Y1移动到0失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { pos[0], pos[1] }, true))
                    {
                        MessageBox.Error("移动失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
                    {
                        MessageBox.Error("Z2移动到目标位置失败！");
                        return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        /// <summary>
        /// 下相机11点标定-执行标定
        /// </summary>
        /// <param name="obj"></param>
        public async void CalibElevenPoint_Nozzle(object obj)
        {
            int index = Convert.ToInt32(obj);
            if (MessageBox.Show($"确定执行{index}号吸嘴下相机11点标定吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos;
                if (index == 2)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo2Pos;
                }
                else if (index == 3)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo3Pos;
                }
                else if (index == 4)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCalibNineAddTwo_NozzleNo4Pos;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nCalibElevenPoint_Nozzle({index})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.SetDownLight(true))
                    {
                        MessageBox.Error("下相机光源打开失败！");
                        return;
                    }
                    float movdis = 5.0f;//十一点标定xy偏移5mm
                    float angle = 10.0f;//十一点标定角度偏移10度
                    float centerX = pos[0];
                    float centerY = pos[1];
                    float centerZ = pos[2];
                    float centerR = pos[3];
                    float[][] calib11Points = new[] {
                        new [] { centerX, centerY, centerZ, centerR },
                        new [] { centerX, centerY + movdis, centerZ, centerR },
                        new [] { centerX - movdis, centerY + movdis, centerZ, centerR },
                        new [] { centerX - movdis, centerY, centerZ, centerR },
                        new [] { centerX - movdis, centerY - movdis, centerZ, centerR },
                        new [] { centerX, centerY - movdis, centerZ, centerR },
                        new [] { centerX + movdis, centerY - movdis, centerZ, centerR },
                        new [] { centerX + movdis, centerY, centerZ, centerR },
                        new [] { centerX + movdis, centerY + movdis, centerZ, centerR },
                        new [] { centerX, centerY, centerZ, centerR - angle },
                        new [] { centerX, centerY, centerZ, centerR + angle },
                    };
                    var axisR = En_AxisNum.R1 + (index - 1);
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R(axisR, new[] { calib11Points[0][0], calib11Points[0][1], calib11Points[0][3] }, true))
                    {
                        MessageBox.Error($"吸嘴轴移动到十一点标定点位第1个xyr失败！");
                        return;
                    }
                    Thread.Sleep(300);//防止连续运动导致标定片偏移
                    if (!_mGoogol_Component.SetCylinderUpDown(index, false))
                    {
                        MessageBox.Error($"气缸下失败！");
                        return;
                    }
                    Thread.Sleep(300);//防止连续运动导致标定片偏移
                    //int cmdSerialNumber = index + (_paramManager.MESParam.TapeNames == TapeNames.Alert_Bumper ? 7 : 5);
                    int cmdSerialNumber = index + 5;
                    if (!_camera_Component.SendScCalib(cmdSerialNumber, 11))
                    {
                        MessageBox.Error("标定启动失败");
                        return;
                    }
                    for (int i = 0; i < calib11Points.Length; i++)
                    {
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(axisR, calib11Points[i], true))
                        {
                            MessageBox.Error($"吸嘴轴移动到十一点标定点位第{i + 1}个xyzr失败！");
                            return;
                        }
                        Thread.Sleep(300);
                        if (!_camera_Component.CalibProcess(cmdSerialNumber, calib11Points[i][0], calib11Points[i][1], calib11Points[i][3]))
                        {
                            MessageBox.Error($"十一点标定第{i + 1}个点标定失败");
                            return;
                        }
                    }
                    if (!_mGoogol_Component.SetDownLight(false))
                    {
                        MessageBox.Error("下相机光源关闭失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetCylinderUpDown(index, true))
                    {
                        MessageBox.Error($"气缸上失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
                    {
                        MessageBox.Error($"Z2移动到0失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(axisR, 0, true))
                    {
                        MessageBox.Error($"R{index}移动到0失败！");
                        return;
                    }
                    if (!_camera_Component.SendEcCalib())
                    {
                        MessageBox.Error("发送{index}号吸嘴下相机11点标定完成指令失败！");
                        return;
                    }
                    MessageBox.Info($"{index}号吸嘴下相机11点标定已完成，请等待视觉软件计算完毕！\n此弹窗可直接关闭");
                });
            }
        }

        /// <summary>
        /// 结束标定，整合所有标定
        /// </summary>
        /// <param name="obj"></param>
        public void EndCalibration()
        {
            MessageBoxResult mbr = MessageBox.Show($"确定结束标定吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (mbr == MessageBoxResult.OK)
            {
                _camera_Component.EndCalibUN();
                MessageBox.Info("已发送结束标定指令，请等待视觉软件计算完毕！\n此弹窗可直接关闭");
            }
        }
        #endregion
    }
}
