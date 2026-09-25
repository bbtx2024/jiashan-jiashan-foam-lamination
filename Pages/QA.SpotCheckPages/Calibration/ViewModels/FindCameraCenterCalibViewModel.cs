using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Component.Camera;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Station;
using QA.Business.Steps;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using IComponent = QA.Business.Interfaces.IComponent;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.Calibration.ViewModels
{
    [Export(typeof(ICalibrationViewModel))]
    public class FindCameraCenterCalibViewModel : Screen, INotifyPropertyChanged, ICalibrationViewModel, IHandle<List<IComponent>>
    {
        #region Field
        private readonly IEventAggregator _eventAggregator;
        private MotionGoogol_Component _mGoogol_Component;
        private PLC_Component _plc_Component;
        private Camera_Component _camera_Component;
        private CacheParamManager _cacheParamManager;
        private MyUserManager _myUserManager;
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private bool isMoving = false;//指示一套流程有没有做完
        private bool startNextTest = false;//指示是否进行下一轮批量单取测试
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "示教点位标定";

        public ushort OrderID { get; set; } = 0;

        private float _XOffset = 0;
        public float XOffset
        {
            get => _XOffset;
            set
            {
                if (value > 0.5f)
                {
                    value = 0.5f;
                }
                if (value < -0.5f)
                {
                    value = -0.5f;
                }
                _XOffset = value;
                NotifyOfPropertyChange(() => XOffset);
            }
        }

        private float _YOffset = 0;
        public float YOffset
        {
            get => _YOffset;
            set
            {
                if (value > 0.5f)
                {
                    value = 0.5f;
                }
                if (value < -0.5f)
                {
                    value = -0.5f;
                }
                _YOffset = value;
                NotifyOfPropertyChange(() => YOffset);
            }
        }

        private float _ZOffset = 0;
        public float ZOffset
        {
            get => _ZOffset;
            set
            {
                if (value > 0.5f)
                {
                    value = 0.5f;
                }
                if (value < -0.5f)
                {
                    value = -0.5f;
                }
                _ZOffset = value;
                NotifyOfPropertyChange(() => ZOffset);
            }
        }

        private float _ROffset1 = 0;
        public float ROffset1
        {
            get => _ROffset1;
            set
            {
                if (value > 10f)
                {
                    value = 10f;
                }
                if (value < -10f)
                {
                    value = -10f;
                }
                _ROffset1 = value;
                NotifyOfPropertyChange(() => ROffset1);
            }
        }

        private float _ROffset2 = 0;
        public float ROffset2
        {
            get => _ROffset2;
            set
            {
                if (value > 10f)
                {
                    value = 10f;
                }
                if (value < -10f)
                {
                    value = -10f;
                }
                _ROffset2 = value;
                NotifyOfPropertyChange(() => ROffset2);
            }
        }

        private string[] _posStrs = new string[25];
        public string[] PosStrs
        {
            get => _posStrs;
            set
            {
                _posStrs = value;
                NotifyOfPropertyChange(() => PosStrs);
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

        private bool _enableButtons2 = false;
        public bool EnableButtons2
        {
            get => _enableButtons2;
            set
            {
                _enableButtons2 = value;
                NotifyOfPropertyChange(() => EnableButtons2);
            }
        }
        #endregion

        #region Constructor
        public FindCameraCenterCalibViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _myUserManager = IoC.Get<MyUserManager>();
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            FreshAllPoses();
        }
        #endregion

        #region Handle
        public void Handle(List<IComponent> message)
        {
            var _baseBiz = IoC.Get<IBaseBiz>() as BaseBiz;
            EnableButtons = _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeManager;
            EnableButtons2 = _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeManager && !_baseBiz.AutoRun && !isMoving;
        }
        #endregion

        #region Method

        private void FreshAllPoses()
        {
            PosStrs[0] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos).TrimEnd(',');
            PosStrs[1] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos).TrimEnd(',');
            PosStrs[2] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos).TrimEnd(',');
            PosStrs[3] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos).TrimEnd(',');
            PosStrs[4] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos).TrimEnd(',');
            PosStrs[5] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos).TrimEnd(',');
            PosStrs[6] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos).TrimEnd(',');
            PosStrs[7] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos).TrimEnd(',');
            PosStrs[8] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos).TrimEnd(',');
            PosStrs[9] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos).TrimEnd(',');
            PosStrs[10] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos).TrimEnd(',');
            PosStrs[11] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos).TrimEnd(',');
            PosStrs[12] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos).TrimEnd(',');
            PosStrs[13] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos).TrimEnd(',');
            PosStrs[14] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos).TrimEnd(',');
            PosStrs[15] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNgSiloPos).TrimEnd(',');
            PosStrs[16] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder1Pos).TrimEnd(',');
            PosStrs[17] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2Pos).TrimEnd(',');

            PosStrs[18] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_NoPos).TrimEnd(',');

            PosStrs[19] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos).TrimEnd(',');
            PosStrs[20] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos).TrimEnd(',');
            PosStrs[21] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos).TrimEnd(',');
            PosStrs[22] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos).TrimEnd(',');


            PosStrs[23] = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNgSiloPos2).TrimEnd(',');
            NotifyOfPropertyChange(() => PosStrs);
        }

        public void SetAsCurrentPos(object obj)
        {
            int idx = Convert.ToInt32(obj);
            if (idx != 14 && !_mGoogol_Component.Station2InSafeArea())
            {
                MessageBox.Error("当前吸嘴轴不在安全位置，无法设定！");
                return;
            }
            switch (idx)
            {
                case 0:
                    if (MessageBox.Show($"确定将当前坐标设为左飞达吸嘴1左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R1],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"左飞达吸嘴1左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos = pos;
                    }
                    break;
                case 1:
                    if (MessageBox.Show($"确定将当前坐标设为左飞达吸嘴1右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R1],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"左飞达吸嘴1右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos = pos;
                    }
                    break;
                case 2:
                    if (MessageBox.Show($"确定将当前坐标设为左飞达吸嘴2左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"左飞达吸嘴2左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos = pos;
                    }
                    break;
                case 3:
                    if (MessageBox.Show($"确定将当前坐标设为左飞达吸嘴2右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"左飞达吸嘴2右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos = pos;
                    }
                    break;
                case 4:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴3左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R3],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴3左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos = pos;
                    //}
                    break;
                case 5:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴3右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R3],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴3右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos = pos;
                    //}
                    break;
                case 6:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴4左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R4],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴4左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos = pos;
                    //}
                    break;
                case 7:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴4右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R4],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴4右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos = pos;
                    //}
                    break;
                case 8:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴13同时取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R1],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R3],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴13同时取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos = pos;
                    //}
                    break;
                case 9:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴24同时取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R4],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴24同时取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos = pos;
                    //}
                    break;
                case 10:
                    if (MessageBox.Show($"确定将当前坐标设为吸嘴1下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R1],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴1下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos = pos;
                    }
                    break;
                case 11:
                    if (MessageBox.Show($"确定将当前坐标设为吸嘴2下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴2下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos = pos;
                    }
                    break;
                case 12:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴3下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R3],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴3下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos = pos;
                    //}
                    break;
                case 13:
                    //if (MessageBox.Show($"确定将当前坐标设为吸嘴4下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    //{
                    //    var pos = new[] {
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                    //        _mGoogol_Component.CurPos[(byte)En_AxisNum.R4],
                    //    };
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴4下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                    //    _cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos = pos;
                    //}
                    break;
                case 14:
                    if (MessageBox.Show($"确定将当前坐标设为扫载具码坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X1],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"扫载具码坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos = pos;
                    }
                    break;
                case 15:
                    if (MessageBox.Show($"确定将当前坐标设为1号吸嘴NG物料抛料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"NG物料抛料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNgSiloPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisNgSiloPos = pos;
                    }
                    break;
                case 16:
                    if (MessageBox.Show($"确定将当前坐标设为Feeder1拍照位坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Feeder1拍照位坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder1Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder1Pos = pos;
                    }
                    break;
                case 17:
                    if (MessageBox.Show($"确定将当前坐标设为Feeder2拍照位坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Feeder2拍照位坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2Pos = pos;
                    }
                    break;

                case 18:
                    if (MessageBox.Show($"确定将当前坐标设为下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R1],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_NoPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_NoPos = pos;
                    }
                    break;
                case 19:
                    if (MessageBox.Show($"确定将当前坐标设为右飞达吸嘴1左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R1],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"右飞达吸嘴1左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos = pos;
                    }
                    break;
                case 20:
                    if (MessageBox.Show($"确定将当前坐标设为右飞达吸嘴1右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R1],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"右飞达吸嘴1右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos = pos;
                    }
                    break;
                case 21:
                    if (MessageBox.Show($"确定将当前坐标设为右飞达吸嘴2左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"右飞达吸嘴2左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos = pos;
                    }
                    break;
                case 22:
                    if (MessageBox.Show($"确定将当前坐标设为右飞达吸嘴2右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.R2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"右飞达吸嘴2右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos = pos;
                    }
                    break;
                case 23:
                    if (MessageBox.Show($"确定将当前坐标设为2号吸嘴NG物料抛料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.X2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2],
                            _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2],
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"2号吸嘴NG物料抛料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNgSiloPos2).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisNgSiloPos2 = pos;
                    }
                    break;
                default:
                    break;
            }
            _cacheParamManager.SaveAllParam();
            FreshAllPoses();
        }

        public async void MoveToPos(object obj)
        {
            await Task.Run(() =>
            {
                try
                {
                    isMoving = true;
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("有轴正在运动，无法移动！");
                        return;
                    }
                    int idx = Convert.ToInt32(obj);
                    switch (idx)
                    {
                        case 0:
                            if (MessageBox.Show($"确定移动到左飞达吸嘴1左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 1:
                            if (MessageBox.Show($"确定移动到左飞达吸嘴1右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 2:
                            if (MessageBox.Show($"确定移动到左飞达吸嘴2左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R2, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 3:
                            if (MessageBox.Show($"确定移动到左飞达吸嘴2右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R2, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 4:
                            if (MessageBox.Show($"确定移动到吸嘴3左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R3, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 5:
                            if (MessageBox.Show($"确定移动到吸嘴3右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R3, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 6:
                            if (MessageBox.Show($"确定移动到吸嘴4左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R4, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 7:
                            if (MessageBox.Show($"确定移动到吸嘴4右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R4, _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 8:
                            if (MessageBox.Show($"确定移动到吸嘴13同时取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2RR(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos, true, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 9:
                            if (MessageBox.Show($"确定移动到吸嘴34同时取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2RR(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos, false, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 10:
                            if (MessageBox.Show($"确定移动到吸嘴1下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 11:
                            if (MessageBox.Show($"确定移动到吸嘴2下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R2, _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 12:
                            if (MessageBox.Show($"确定移动到吸嘴3下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R3, _cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 13:
                            if (MessageBox.Show($"确定移动到吸嘴4下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R4, _cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos, true))
                                //{
                                //    MessageBox.Error("移动失败！");
                                //    return;
                                //}
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 14:
                            if (MessageBox.Show($"确定移动到扫载具码坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX1Y1(_cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 15:
                            if (MessageBox.Show($"确定移动到1号吸嘴NG物料抛料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisNgSiloPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 16:
                            if (MessageBox.Show($"确定移动到Feeder1拍照坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder1Pos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 17:
                            if (MessageBox.Show($"确定移动到Feeder2拍照坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;

                        case 18:
                            if (MessageBox.Show($"确定移动到下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2RAll(_cacheParamManager.manualPositionParam.AxisDownCamera_NoPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 19:
                            if (MessageBox.Show($"确定移动到右飞达吸嘴1左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 20:
                            if (MessageBox.Show($"确定移动到右飞达吸嘴1右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 21:
                            if (MessageBox.Show($"确定移动到右飞达吸嘴2左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R2, _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 22:
                            if (MessageBox.Show($"确定移动到右飞达吸嘴2右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R2, _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        case 23:
                            if (MessageBox.Show($"确定移动到2号吸嘴NG物料抛料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                            {
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
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisNgSiloPos2, true))
                                {
                                    MessageBox.Error("移动失败！");
                                    return;
                                }
                                MessageBox.Success("移动完毕！");
                            }
                            break;
                        default:
                            break;
                    }
                }
                finally
                {
                    isMoving = false;
                }
            });
        }

        public void AddOffset(object obj)
        {
            int idx = Convert.ToInt32(obj);
            switch (idx)
            {
                case 0:
                    if (MessageBox.Show($"确定补偿左飞达吸嘴1左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴1左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[0] += XOffset;
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[1] += YOffset;
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[2] += ZOffset;
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[3] += ROffset1;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 1:
                    if (MessageBox.Show($"确定补偿左飞达吸嘴1右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴1右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1RightPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 2:
                    if (MessageBox.Show($"确定补偿左飞达吸嘴2左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴2左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2LeftPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 3:
                    if (MessageBox.Show($"确定补偿左飞达吸嘴2右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴2右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle2RightPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 4:
                    if (MessageBox.Show($"确定补偿吸嘴3左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴3左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 5:
                    if (MessageBox.Show($"确定补偿吸嘴3右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴3右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3RightPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 6:
                    if (MessageBox.Show($"确定补偿吸嘴4左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴4左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4LeftPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 7:
                    if (MessageBox.Show($"确定补偿吸嘴4右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴4右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle4RightPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 8:
                    if (MessageBox.Show($"确定补偿吸嘴13同时取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos[3] + ROffset1,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos[4] + ROffset2,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴13同时取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle13Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 9:
                    if (MessageBox.Show($"确定补偿吸嘴24同时取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos[3] + ROffset1,
                            _cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos[4] + ROffset2,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴24同时取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeederPickNozzle24Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 10:
                    if (MessageBox.Show($"确定补偿吸嘴1下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴1下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_No1Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 11:
                    if (MessageBox.Show($"确定补偿吸嘴2下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴2下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_No2Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 12:
                    if (MessageBox.Show($"确定补偿吸嘴3下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴3下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_No3Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 13:
                    if (MessageBox.Show($"确定补偿吸嘴4下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴4下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_No4Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 14:
                    if (MessageBox.Show($"确定补偿扫载具码坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos[1] + YOffset,
                        };
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"扫载具码坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisScannerCarrierBarcodePos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 15:
                    if (MessageBox.Show($"确定补偿1号吸嘴NG物料抛料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisNgSiloPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisNgSiloPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisNgSiloPos[2] + ZOffset,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"1号吸嘴NG物料抛料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNgSiloPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisNgSiloPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 16:
                    if (MessageBox.Show($"确定补偿Feeder1拍照坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeeder1Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder1Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder1Pos[2] + ZOffset,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"NG物料抛料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder1Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder1Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 17:
                    if (MessageBox.Show($"确定补偿Feeder2拍照坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeeder2Pos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2Pos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2Pos[2] + ZOffset,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"NG物料抛料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2Pos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2Pos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;

                case 18:
                    if (MessageBox.Show($"确定补偿下视觉坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisDownCamera_NoPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_NoPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_NoPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_NoPos[3] + ROffset1,
                            _cacheParamManager.manualPositionParam.AxisDownCamera_NoPos[4] + ROffset2,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴1下视觉坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisDownCamera_NoPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisDownCamera_NoPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 19:
                    if (MessageBox.Show($"确定补偿右飞达吸嘴1左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴1左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[0] += XOffset;
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[1] += YOffset;
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[2] += ZOffset;
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1LeftPos[3] += ROffset1;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 20:
                    if (MessageBox.Show($"确定补偿右飞达吸嘴1右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴1右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle1RightPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 21:
                    if (MessageBox.Show($"确定补偿右飞达吸嘴2左取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴2左取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2LeftPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 22:
                    if (MessageBox.Show($"确定补偿左飞达吸嘴2右取料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos[2] + ZOffset,
                            _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos[3] + ROffset1,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"吸嘴2右取料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisFeeder2PickNozzle2RightPos = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                case 23:
                    if (MessageBox.Show($"确定补偿2号吸嘴NG物料抛料坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                    {
                        var pos = new[] {
                            _cacheParamManager.manualPositionParam.AxisNgSiloPos2[0] + XOffset,
                            _cacheParamManager.manualPositionParam.AxisNgSiloPos2[1] + YOffset,
                            _cacheParamManager.manualPositionParam.AxisNgSiloPos2[2] + ZOffset,
                        };
                        if (pos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                        {
                            MessageBox.Error("补偿后吸嘴轴不在安全位置，无法设定！");
                            return;
                        }
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"2号吸嘴NG物料抛料坐标：{NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNgSiloPos2).TrimEnd(',')}->{NLogTrace.GetFloatArrayString(pos).TrimEnd(',')}", En_Logout_Type.SystemParam);
                        _cacheParamManager.manualPositionParam.AxisNgSiloPos2 = pos;
                        XOffset = YOffset = ZOffset = ROffset1 = ROffset2 = 0;
                    }
                    break;
                default:
                    break;
            }
            _cacheParamManager.SaveAllParam();
            FreshAllPoses();
        }

        public async void NozzlePickTest(object obj)
        {
            await Task.Run(() =>
            {
                try
                {
                    isMoving = true;
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("有轴正在运动，无法移动！");
                        return;
                    }
                    int idx = Convert.ToInt32(obj);
                    switch (idx)
                    {
                        case 0:
                            SingleNozzlePickTest(1, true);
                            break;
                        case 1:
                            SingleNozzlePickTest(1, false);
                            break;
                        case 2:
                            SingleNozzlePickTest(2, true);
                            break;
                        case 3:
                            SingleNozzlePickTest(2, false);
                            break;
                        case 4:
                            SingleNozzlePickTest(3, true);
                            break;
                        case 5:
                            SingleNozzlePickTest(3, false);
                            break;
                        case 6:
                            SingleNozzlePickTest(4, true);
                            break;
                        case 7:
                            SingleNozzlePickTest(4, false);
                            break;
                        case 8:
                            DoubleNozzlesPickTest(true);
                            break;
                        case 9:
                            DoubleNozzlesPickTest(false);
                            break;
                        default:
                            break;
                    }
                }
                finally
                {
                    isMoving = false;
                }
            });
        }

        private void SingleNozzlePickTest(int nozzleNo, bool isLeft)
        {
            if (MessageBox.Show($"确定执行吸嘴{nozzleNo}{(isLeft ? "左" : "右")}取料测试吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (nozzleNo == 2 || nozzleNo == 4)
                {
                    MessageBox.Error("禁止2、4吸嘴取料测试！");
                    return;
                }
                if (!_mGoogol_Component.CleanAlarm())
                {
                    MessageBox.Error("清除报警失败！");
                    return;
                }
                if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Work))
                {
                    MessageBox.Error("设置轴速为工作速度失败！");
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
                if (!_cacheParamManager.HomeUiParam.Enable.UseVacSucCheck || _mGoogol_Component.IsNozzleOpenSuction(nozzleNo))
                {
                    if (!_stepStatus.ThrowTapes(new HashSet<int> { nozzleNo }))
                    {
                        MessageBox.Error("抛料失败！");
                        return;
                    }
                }
                if (!_stepStatus.TrigFeederConveyTape(true))
                {
                    MessageBox.Error("触发飞达送料失败！");
                    return;
                }
                //等一会，防止两边出料速度不一样导致判断错误
                Thread.Sleep(200);
                if (isLeft && !_plc_Component.IsFeederTapeReady(0))
                {
                    MessageBox.Error("飞达左侧无物料！");
                    return;
                }
                if (!isLeft && !_plc_Component.IsFeederTapeReady(1))
                {
                    MessageBox.Error("飞达右侧无物料！");
                    return;
                }
                if (!FeederCDD(true))
                {
                    MessageBox.Error("飞达拍照失败！");
                    return;
                }
                if (!_camera_Component.IsCDDOk)
                {
                    if (_camera_Component.CDDNG("Feeder拍照"))
                    {
                        if (!FeederCDD(true))
                        {
                            MessageBox.Error("飞达拍照失败！");
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Error("飞达拍照失败！");
                        return;
                    }
                }
            CDD:
                //初始化需要计算坐标的吸嘴
                List<int> nozzleNos = new List<int> { nozzleNo };
                //如果Feedeer物料为两个时，需再添加一个吸嘴
                int index = 0;
                if (_camera_Component.TapeNum == 2)
                {
                    if (nozzleNo == 1)
                    {
                        if (isLeft)
                        {
                            nozzleNos = new List<int> { 1, 3 };
                        }
                        else
                        {
                            nozzleNos = new List<int> { 3, 1 };
                            index = 1;
                        }
                    }
                    if (nozzleNo == 3)
                    {
                        if (isLeft)
                        {
                            nozzleNos = new List<int> { 3, 1 };
                        }
                        else
                        {
                            nozzleNos = new List<int> { 1, 3 };
                            index = 1;
                        }
                    }
                }
                //计算取料坐标
                if (!_camera_Component.GM(nozzleNos.ToArray(), "Foam"))
                {
                    if (_camera_Component.CDDNG("Feeder拍照"))
                    {
                        if (!FeederCDD(true))
                        {
                            MessageBox.Error("飞达拍照失败！");
                            return;
                        }
                        goto CDD;
                    }
                    MessageBox.Error("计算取料坐标失败！");
                    return;
                }
                var pickPos = _stepStatus.FeederSingleTapePos(nozzleNo, isLeft ? 0 : 1);
                if (_camera_Component.xyafloats.Count < index + 1)
                {
                    MessageBox.Error($"计算取料坐标失败，返回結果个数不足{index + 1}！");
                    return;
                }
                var floats = _camera_Component.xyafloats[index].ToArray();
                if (floats != null)
                {
                    pickPos[0] = floats[0];
                    pickPos[1] = floats[1];
                    pickPos[3] = floats[2];
                }

                //移动到默认取料位
                float[] feederPos = _stepStatus.FeederSingleTapePos(nozzleNo);
                if (floats != null)
                {
                    feederPos[0] = floats[0];
                    feederPos[1] = floats[1];
                    feederPos[3] = floats[2];
                }


                //if (pickPos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                if (feederPos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                {
                    MessageBox.Error("取料点位不在安全位置！");
                    return;
                }
                //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R((En_AxisNum)(nozzleNo + 4), pickPos, true))
                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R((En_AxisNum)(nozzleNo + 4), feederPos, true))
                {
                    MessageBox.Error("移动至取料点位失败！");
                    return;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, false))
                {
                    MessageBox.Error("气缸下失败！");
                    return;
                }
                if (!_mGoogol_Component.SetVacuum(nozzleNo, true, true))
                {
                    MessageBox.Error("开吸失败！");
                    return;
                }
                //等待一定时间
                Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
                //飞达吹气开
                //根据x轴当前位置，确定现在取的是左边还是右边。注意点位使用视觉引导，所以误差定在±5
                bool left = nozzleNo == 1
                    ? Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[0]) < 5
                    : Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos[0]) < 5;
                if (!_mGoogol_Component.SetFeederVacuum(left, true) || _mGoogol_Component.Exit())
                {
                    MessageBox.Error("飞达吹气开失败！");
                    return;
                }
                //等待一定时间
                Thread.Sleep(_paramManager.OtherSettingParam.GetTapeFeederBreakTime);
                //飞达吹气关
                if (!_mGoogol_Component.SetFeederVacuum(left, false) || _mGoogol_Component.Exit())
                {
                    MessageBox.Error("飞达吹气关失败！");
                    return;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, true))
                {
                    MessageBox.Error("气缸上失败！");
                    return;
                }
                if (_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                {
                    if (!_mGoogol_Component.GetMaterialReady(nozzleNo))
                    {
                        _mGoogol_Component.SetVacuum(nozzleNo, true, false);
                        _mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true);
                        _mGoogol_Component.SetCylinderUpDown(nozzleNo, true);
                        MessageBox.Error("未能吸取到物料！");
                        return;
                    }
                }
                var downCamPos = _stepStatus.DownCameraPos(nozzleNo +1);
                if (downCamPos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                {
                    MessageBox.Error("下视觉点位不在安全位置！");
                    return;
                }
                if (!_mGoogol_Component.SetDownLight(true))
                {
                    MessageBox.Error("下相机光源打开失败！");
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R((En_AxisNum)(nozzleNo + 4), downCamPos, false))
                {
                    _mGoogol_Component.SetDownLight(false);
                    MessageBox.Error("开始移动至下视觉点位失败！");
                    return;
                }
                if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, false))
                {
                    _mGoogol_Component.SetDownLight(false);
                    MessageBox.Error("气缸下失败！");
                    return;
                }
                Thread.Sleep(100);
                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R((En_AxisNum)(nozzleNo + 4), downCamPos, true))
                {
                    _mGoogol_Component.SetDownLight(false);
                    MessageBox.Error("移动至下视觉点位失败！");
                    return;
                }
                Thread.Sleep(_stepStatus.ParamManager.CameraParam.DownCamWaitTime);
                string TLNSN = $"TLN_Nozzle{nozzleNo}_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                bool downCamOk = _camera_Component.TLN(TLNSN, nozzleNo, "Foam", 0, "Foam->SIP", downCamPos[0], downCamPos[1], downCamPos[3], out float[] partxya, out string[] data, out float[] xya);
                if (!_mGoogol_Component.SetDownLight(false))
                {
                    MessageBox.Error("下相机光源关闭失败！");
                    return;
                }
                if (!_stepStatus.ThrowTapes(new HashSet<int> { nozzleNo }))
                {
                    MessageBox.Error("抛料失败！");
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[] { 0, 120 }, true))
                {
                    MessageBox.Error("回安全位置失败！");
                    return;
                }
                if (downCamOk)
                {
                    MessageBox.Success("测试完毕，下视觉定位成功！");
                }
                else
                {
                    MessageBox.Error("测试完毕，下视觉定位失败！");
                }
            }
        }

        /// <summary>
        /// 相机拍照
        /// </summary>
        /// <param name="isdodge">是否避让相机</param>
        /// <returns></returns>
        private bool FeederCDD(bool isdodge)
        {
            if (isdodge)
            {
                //获取参数，判断移动到哪一个避位点
                FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                switch (feederId)
                {
                    case FeederId.左飞达:
                        //避让相机
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder1Pos, true))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到Feeder1拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder1Pos[1]}", En_Logout_Type.Alarm, true);
                            return false;
                        }
                        break;
                    case FeederId.右飞达:
                        //避让相机
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->X2,Y2运动到Feeder2拍照位失败(Di chuyển đến X2 gốc không thành công)，Axis[X2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[0]},Axis[Y2]:{_cacheParamManager.manualPositionParam.AxisFeeder2Pos[1]}", En_Logout_Type.Alarm, true);
                            return false;
                        }
                        break;
                    default:
                        break;
                }
            }
            //Feeder物料个数
            int tapeNum = 0;
            //等待物料到位
            if (!_stepStatus.TrigFeederConveyTape(true))
            {
                return false;
            }
            if (!_mGoogol_Component.SetFeederUpLight(true))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->Feeder相机光打开失败(Tắt ánh sáng vòng trên máy ảnh không thành công)", En_Logout_Type.Alarm, true);
                return false;
            }
            int CDDnum = -1;
        CDD:
            _stepStatus.TLMSn = $"TLM_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
            if (!_camera_Component.TLM(_stepStatus.TLMSn, "Foam", tapeNum))
            {
                CDDnum++;
                if (CDDnum < _stepStatus.ParamManager.CameraParam.FeederNGCDDnum)
                {
                    goto CDD;
                }
                if (_camera_Component.CDDNG("Feeder拍照"))
                {
                    goto CDD;
                }
                return false;
            }
            if (!_mGoogol_Component.SetFeederUpLight(false))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"UpCam->Feeder相机光关闭失败(Tắt ánh sáng vòng trên máy ảnh không thành công)", En_Logout_Type.Alarm, true);
                return false;
            }
            return true;
        }

        private void DoubleNozzlesPickTest(bool is13)
        {
            //if (MessageBox.Show($"确定执行吸嘴{(is13 ? "13" : "24")}同取测试吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            //{
            //    if (true)
            //    {
            //        //没有适配，不能测试
            //        MessageBox.Error("禁止同取测试！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.CleanAlarm())
            //    {
            //        MessageBox.Error("清除报警失败！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Work))
            //    {
            //        MessageBox.Error("设置轴速为工作速度失败！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            //    {
            //        MessageBox.Error("Z2移动到0失败！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.SetAllCylindersUpDown(true))
            //    {
            //        MessageBox.Error("所有气缸上失败！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.Station2InSafeArea() && !_mGoogol_Component.MoveY2ToSafePos())
            //    {
            //        MessageBox.Error("Y2移动到安全位置失败！");
            //        return;
            //    }
            //    int[] nozzleNos = is13 ? new[] { 1, 3 } : new[] { 2, 4 };
            //    if (!_cacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
            //    {
            //        if (!_stepStatus.ThrowTapes(new HashSet<int>(nozzleNos)))
            //        {
            //            MessageBox.Error("抛料失败！");
            //            return;
            //        }
            //    }
            //    else
            //    {
            //        HashSet<int> throwTapeNozzleNoSet = new HashSet<int>();
            //        for (int i = 0; i < 2; i++)
            //        {
            //            if (_mGoogol_Component.IsNozzleOpenSuction(nozzleNos[i]))
            //            {
            //                throwTapeNozzleNoSet.Add(nozzleNos[i]);
            //            }
            //        }
            //        if (throwTapeNozzleNoSet.Count > 0 && !_stepStatus.ThrowTapes(throwTapeNozzleNoSet))
            //        {
            //            MessageBox.Error("抛料失败！");
            //            return;
            //        }
            //    }
            //    int tapeNum = 0;
            //    if (!_stepStatus.TrigFeederConveyTape(true, ref tapeNum))
            //    {
            //        MessageBox.Error("触发飞达送料失败！");
            //        return;
            //    }
            //    if (!_plc_Component.IsFeederTapeReady(0))
            //    {
            //        MessageBox.Error("飞达左侧无物料！");
            //        return;
            //    }
            //    if (!_plc_Component.IsFeederTapeReady(1))
            //    {
            //        MessageBox.Error("飞达右侧无物料！");
            //        return;
            //    }
            //    if (!FeederCDD(true))
            //    {
            //        MessageBox.Error("飞达拍照失败！");
            //        return;
            //    }
            //    var pickPos = _stepStatus.FeederDoubleTapePos(is13);
            //    if (pickPos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
            //    {
            //        MessageBox.Error("取料点位不在安全位置！");
            //        return;
            //    }
            //    //if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2RR(pickPos, is13, true))
            //    //{
            //    //    MessageBox.Error("移动至取料点位失败！");
            //    //    return;
            //    //}
            //    if (!_mGoogol_Component.SetTwoCylindersUpDown(is13, false))
            //    {
            //        MessageBox.Error("气缸下失败！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.SetVacuum(nozzleNos[0], true, true) || !_mGoogol_Component.SetVacuum(nozzleNos[1], true, true))
            //    {
            //        MessageBox.Error("开吸失败！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.SetTwoCylindersUpDown(is13, true))
            //    {
            //        MessageBox.Error("气缸上失败！");
            //        return;
            //    }
            //    if (_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
            //    {
            //        if (!_mGoogol_Component.GetMaterialReady(nozzleNos[0]) || !_mGoogol_Component.GetMaterialReady(nozzleNos[1]))
            //        {
            //            _mGoogol_Component.SetVacuum(nozzleNos[0], true, false);
            //            _mGoogol_Component.SetVacuum(nozzleNos[1], true, false);
            //            _mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true);
            //            _mGoogol_Component.SetTwoCylindersUpDown(is13, true);
            //            MessageBox.Error("未能吸取到物料！");
            //            return;
            //        }
            //    }
            //    for (int i = 0; i < 2; i++)
            //    {
            //        var downCamPos = _stepStatus.DownCameraPos(nozzleNos[i]);
            //        if (downCamPos[1] >= _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
            //        {
            //            MessageBox.Error($"下视觉点位{nozzleNos[i]}不在安全位置！");
            //            return;
            //        }
            //    }
            //    if (!_mGoogol_Component.SetDownLight(true))
            //    {
            //        MessageBox.Error("下相机光源打开失败！");
            //        return;
            //    }
            //    bool[] downCamOk = new bool[2];
            //    for (int i = 0; i < 2; i++)
            //    {
            //        var downCamPos = _stepStatus.DownCameraPos(nozzleNos[i]);
            //        if (i == 0)
            //        {
            //            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R((En_AxisNum)(nozzleNos[i] + 4), downCamPos, false))
            //            {
            //                _mGoogol_Component.SetDownLight(false);
            //                MessageBox.Error("开始移动至下视觉点位失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetTwoCylindersUpDown(is13, false))
            //            {
            //                _mGoogol_Component.SetDownLight(false);
            //                MessageBox.Error("气缸下失败！");
            //                return;
            //            }
            //            Thread.Sleep(100);
            //        }
            //        if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R((En_AxisNum)(nozzleNos[i] + 4), downCamPos, true))
            //        {
            //            _mGoogol_Component.SetDownLight(false);
            //            MessageBox.Error("移动至下视觉点位失败！");
            //            return;
            //        }
            //        Thread.Sleep(_stepStatus.ParamManager.CameraParam.DownCamWaitTime);
            //        string TLNSN = $"TLN_Nozzle{nozzleNos[0]}_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
            //        downCamOk[i] = _camera_Component.TLN(TLNSN, nozzleNos[i], "Foam", 0, "Foam->SIP", downCamPos[0], downCamPos[1], downCamPos[3], out float[] partxya, out string[] data, out float[] xya);
            //    }
            //    if (!_mGoogol_Component.SetDownLight(false))
            //    {
            //        MessageBox.Error("下相机光源关闭失败！");
            //        return;
            //    }
            //    if (!_stepStatus.ThrowTapes(new HashSet<int> { nozzleNos[0], nozzleNos[1] }))
            //    {
            //        MessageBox.Error("抛料失败！");
            //        return;
            //    }
            //    if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[] { 0, 120 }, true))
            //    {
            //        MessageBox.Error("回安全位置失败！");
            //        return;
            //    }
            //    if (downCamOk[0] && downCamOk[1])
            //    {
            //        MessageBox.Success("测试完毕，下视觉定位成功！");
            //    }
            //    else
            //    {
            //        MessageBox.Error($"测试完毕，下视觉定位{nozzleNos[0]}号吸嘴{(downCamOk[0] ? "成功" : "失败")}，{nozzleNos[1]}号吸嘴{(downCamOk[1] ? "成功" : "失败")}！");
            //    }
            //}
        }

        public async void AllNozzlesPickTest()
        {
            await Task.Run(() =>
            {
                try
                {
                    var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
                    startNextTest = true;
                    isMoving = true;
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("有轴正在运动，无法移动！");
                        return;
                    }
                    if (_stepStatus.GetEnableNozzleCount() == 0)
                    {
                        MessageBox.Error("没有启用的吸嘴！");
                        return;
                    }
                    if (MessageBox.Show($"确定执行批量单取测试吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                    {
                        return;
                    }
                    #region 预处理
                    if (!_mGoogol_Component.CleanAlarm())
                    {
                        MessageBox.Error("清除报警失败！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Work))
                    {
                        MessageBox.Error("设置轴速为工作速度失败！");
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
                    #endregion
                    while (true)
                    {
                    #region 取料
                    PickMaterialFromFeederStep:
                        if (!_mGoogol_Component.GetMaterialsReady(out _stepStatus.PickTapeOk))
                        {
                            MessageBox.Error("获取四个吸嘴吸准备信号失败！");
                            return;
                        }
                        if (!_cacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                        {
                            _stepStatus.PickTapeOk[0] = false;
                            _stepStatus.PickTapeOk[1] = false;
                            _stepStatus.PickTapeOk[2] = false;
                            _stepStatus.PickTapeOk[3] = false;
                        }
                        int sucNum = 0;
                        for (int nozzleNo = 1; nozzleNo <= 4; nozzleNo++)
                        {
                            if (_stepStatus.UseNozzle(nozzleNo) && _stepStatus.PickTapeOk[nozzleNo - 1])
                            {
                                sucNum++;
                            }
                        }
                        bool[] needGetTape = new bool[4];
                        int useNozzleCount = _stepStatus.GetEnableNozzleCount();
                        if (sucNum >= useNozzleCount)
                        {
                            goto DownCameraIdentifyMaterialStep;
                        }
                        for (int nozzleNo = 1; nozzleNo <= 4; nozzleNo++)
                        {
                            if (_stepStatus.UseNozzle(nozzleNo) && !_stepStatus.PickTapeOk[nozzleNo - 1])
                            {
                                needGetTape[nozzleNo - 1] = true;
                            }
                        }
                        int firstNozzleNo = 0;
                        int lastNozzleNo = 0;
                        for (int nozzleNo = 1; nozzleNo <= 4; nozzleNo++)
                        {
                            if (needGetTape[nozzleNo - 1])
                            {
                                if (firstNozzleNo == 0)
                                {
                                    firstNozzleNo = nozzleNo;
                                }
                                lastNozzleNo = nozzleNo;
                            }
                        }
                        for (int nozzleNo = 1; nozzleNo <= 4; nozzleNo++)
                        {
                            if (needGetTape[nozzleNo - 1])
                            {
                                //强制执行拍照
                                if (!FeederCDD(true))
                                {
                                    MessageBox.Error("飞达拍照失败！");
                                    return;
                                }
                            CDD:
                                //初始化需要计算坐标的吸嘴
                                List<int> nozzleNos = new List<int> { nozzleNo };
                                int nozzleNo1 = nozzleNo;
                                //如果Feedeer物料为两个时，需再添加一个吸嘴
                                if (_camera_Component.TapeNum == 2)
                                {
                                    for (int nozzleNo2 = nozzleNo1 + 1; nozzleNo2 <= 4; nozzleNo2++, nozzleNo1++)
                                    {
                                        if (needGetTape[nozzleNo2 - 1])
                                        {
                                            nozzleNos.Add(nozzleNo2);
                                        }
                                    }
                                }
                                //计算取料坐标
                                if (!_camera_Component.GM(nozzleNos.ToArray(), "Foam"))
                                {
                                    if (_camera_Component.CDDNG("Feeder拍照"))
                                    {
                                        if (!FeederCDD(true))
                                        {
                                            MessageBox.Error("飞达拍照失败！");
                                            return;
                                        }
                                        goto CDD;
                                    }
                                    MessageBox.Error("计算取料坐标失败！");
                                    return;
                                }
                                //遍历吸嘴取料
                                for (int i = 0; i < nozzleNos.Count; i++)
                                {
                                    nozzleNo = nozzleNos[i];
                                    bool first = nozzleNo == firstNozzleNo;
                                    bool last = nozzleNo == lastNozzleNo;
                                    var floats = _camera_Component.xyafloats[i].ToArray();
                                    if (!MoveToFeederSingleTape(nozzleNo, true, first, floats))
                                    {
                                        MessageBox.Error("预移动取料位失败！");
                                        return;
                                    }
                                    int tapeNum = 0;
                                    if (!_stepStatus.TrigFeederConveyTape(true))
                                    {
                                        MessageBox.Error("物料未到位！");
                                        return;
                                    }
                                    if (_cacheParamManager.HomeUiParam.Enable.KeepNoTapeOnFeeder
                                        && _stepStatus.GetEnableNozzleCount() % 2 == 0
                                        && last
                                        && _plc_Component.IsFeederTapeReady(0) && _plc_Component.IsFeederTapeReady(1))
                                    {
                                        int throwTapeNozzleNo = 0;
                                        if (!_cacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                                        {
                                            for (int nozzleNo0 = 1; nozzleNo0 <= 4; nozzleNo0++)
                                            {
                                                if (_stepStatus.UseNozzle(nozzleNo0) && _mGoogol_Component.IsNozzleOpenSuction(nozzleNo0))
                                                {
                                                    throwTapeNozzleNo = nozzleNo0;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!_mGoogol_Component.GetMaterialsReady(out bool[] sucStatus))
                                            {
                                                MessageBox.Error("获取四个吸嘴吸准备信号失败！");
                                                return;
                                            }
                                            int extraThrowTapeNozzleNo = nozzleNo % 2 == 0 ? nozzleNo - 1 : nozzleNo + 1;
                                            if (sucStatus[extraThrowTapeNozzleNo - 1])
                                            {
                                                throwTapeNozzleNo = extraThrowTapeNozzleNo;
                                            }
                                            else
                                            {
                                                for (int nozzleNo0 = 1; nozzleNo0 <= 4; nozzleNo0++)
                                                {
                                                    if (_stepStatus.UseNozzle(nozzleNo0) && sucStatus[nozzleNo0 - 1])
                                                    {
                                                        throwTapeNozzleNo = nozzleNo0;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (throwTapeNozzleNo != 0)
                                        {
                                            if (!_stepStatus.ThrowTapes(new HashSet<int> { throwTapeNozzleNo }))
                                            {
                                                MessageBox.Error("清除吸嘴多余物料失败！");
                                                return;
                                            }
                                            goto PickMaterialFromFeederStep;
                                        }
                                    }
                                    if (!MoveToFeederSingleTape(nozzleNo, true, false, floats))
                                    {
                                        return;
                                    }
                                    if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, false))
                                    {
                                        MessageBox.Error("气缸下失败！");
                                        return;
                                    }
                                    if (!_mGoogol_Component.SetVacuum(nozzleNo, true, true))
                                    {
                                        MessageBox.Error("开吸失败！");
                                        return;
                                    }
                                    //等待一定时间
                                    Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
                                    //飞达吹气开
                                    //根据x轴当前位置，确定现在取的是左边还是右边。注意点位使用视觉引导，所以误差定在±5
                                    bool left = nozzleNo == 1
                                        ? Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[0]) < 5
                                        : Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos[0]) < 5;
                                    if (!_mGoogol_Component.SetFeederVacuum(left, true) || _mGoogol_Component.Exit())
                                    {
                                        MessageBox.Error("飞达吹气开失败！");
                                        return;
                                    }
                                    //等待一定时间
                                    Thread.Sleep(_paramManager.OtherSettingParam.GetTapeFeederBreakTime);
                                    //飞达吹气关
                                    if (!_mGoogol_Component.SetFeederVacuum(left, false) || _mGoogol_Component.Exit())
                                    {
                                        MessageBox.Error("飞达吹气关失败！");
                                        return;
                                    }
                                    if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, true))
                                    {
                                        MessageBox.Error("气缸上失败！");
                                        return;
                                    }
                                    if (_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                                    {
                                        if (!_mGoogol_Component.GetMaterialReady(nozzleNo))
                                        {
                                            if (!_mGoogol_Component.SetVacuum(nozzleNo, false))
                                            {
                                                MessageBox.Error("气缸关吸开吹失败！");
                                                return;
                                            }
                                            Thread.Sleep(_stepStatus.ParamManager.OtherSettingParam.OpenBreakTime);
                                            if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false))
                                            {
                                                MessageBox.Error("气缸关吹失败！");
                                                return;
                                            }
                                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
                                            {
                                                MessageBox.Error("Z2运动到0失败！");
                                                return;
                                            }
                                            if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, true))
                                            {
                                                MessageBox.Error("气缸上失败！");
                                                return;
                                            }
                                            var alarm = nozzleNo == 1 ? _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", 106) :
                                                _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleNo}号吸嘴取料失败(Miệng hút lấy thất bại)", 107);
                                            if (MessageBox.Show($"{nozzleNo}号吸嘴取料失败，需要人工清掉不粘板上所有tape！\n确定处理完毕继续吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                            {
                                                _baseBiz.RemoveRunAlarm(alarm);
                                                goto PickMaterialFromFeederStep;
                                            }
                                            else
                                            {
                                                _baseBiz.RemoveRunAlarm(alarm);
                                                return;
                                            }
                                        }
                                    }

                                    _stepStatus.TLMSns[nozzleNos[i]] = _stepStatus.TLMSn;
                                }
                                nozzleNo = nozzleNo1;
                                _stepStatus.PickTapeOk[nozzleNo - 1] = true;
                            }
                        }
                    #endregion
                    #region 下视觉
                    DownCameraIdentifyMaterialStep:
                        HashSet<int> throwTapeNozzleNoSet = new HashSet<int>();
                        if (!_mGoogol_Component.SetDownLight(true))
                        {
                            MessageBox.Error("下视觉光源打开失败！");
                            return;
                        }
                        bool isFirstNozzle = true;
                        for (int nozzleIdx = 0; nozzleIdx < 4; nozzleIdx++)
                        {
                            if (_stepStatus.PickTapeOk[nozzleIdx] && _stepStatus.UseNozzle(nozzleIdx + 1))
                            {
                                float[] downCamPos = _stepStatus.DownCameraPos(nozzleIdx +1);
                                if (isFirstNozzle)
                                {
                                    isFirstNozzle = false;
                                    if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1 + nozzleIdx, downCamPos, false))
                                    {
                                        MessageBox.Error($"运行到第一个取料吸嘴{nozzleIdx + 1}下视觉失败！");
                                        return;
                                    }
                                }
                                else
                                {
                                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1 + nozzleIdx, downCamPos[3], false))
                                    {
                                        MessageBox.Error($"运行到吸嘴{nozzleIdx + 1}下视觉R失败！");
                                        return;
                                    }
                                }
                            }
                        }
                        if (!_mGoogol_Component.SetAllCylindersUpDown(false, false))
                        {
                            MessageBox.Error("所有气缸开始下失败！");
                            return;
                        }
                        bool firstDownCam = true;
                        for (int nozzleIdx = 0; nozzleIdx < 4; nozzleIdx++)
                        {
                            if (_stepStatus.PickTapeOk[nozzleIdx] && _stepStatus.UseNozzle(nozzleIdx + 1))
                            {
                                float[] downCamPos = _stepStatus.DownCameraPos(nozzleIdx + 1);
                                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1 + nozzleIdx, downCamPos, true))
                                {
                                    MessageBox.Error($"运行到{nozzleIdx + 1}号吸嘴失败！");
                                    return;
                                }
                                if (firstDownCam)
                                {
                                    if (!_mGoogol_Component.IsAllCylindersUpDownReady(false))
                                    {
                                        MessageBox.Error("所有气缸下失败！");
                                        return;
                                    }
                                    Thread.Sleep(100);
                                    firstDownCam = false;
                                }
                                Thread.Sleep(_stepStatus.ParamManager.CameraParam.DownCamWaitTime);
                                string TLNSN = $"TLN_Nozzle{nozzleIdx + 1}_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                                _stepStatus.TLNSns[nozzleIdx] = TLNSN;
                                if (!_camera_Component.TLN(TLNSN, nozzleIdx + 1, "Foam", 0, "Foam->SIP", downCamPos[0], downCamPos[1], downCamPos[3], out float[] partxya, out string[] data, out float[] xya))
                                {
                                    _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx]++;
                                    int oneNgCount = _stepStatus.ParamManager.OtherSettingParam.OneNozzleGetTapeFailCount;
                                    int oneNgCountNow = _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx];
                                    if (oneNgCountNow >= oneNgCount)
                                    {
                                        var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"{nozzleIdx + 1}号吸嘴连续{oneNgCountNow}次下视觉NG", 110);
                                        if (MessageBox.Show($"{nozzleIdx + 1}号吸嘴连续{oneNgCountNow}次下视觉NG！\n确定继续吗？", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                        {
                                            _baseBiz.RemoveRunAlarm(alarm);
                                            _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx] = 0;
                                            _stepStatus.NozzleInhaleNotReadyTimes2[nozzleIdx] = 0;
                                        }
                                        else
                                        {
                                            _baseBiz.RemoveRunAlarm(alarm);
                                            return;
                                        }
                                    }
                                    int allNgCount = _stepStatus.ParamManager.OtherSettingParam.NozzlesGetTapeFailCount;
                                    int allNgCountNow = 0;
                                    for (int i = 0; i < 4; i++)
                                    {
                                        allNgCountNow += _stepStatus.NozzleInhaleNotReadyTimes2[i];
                                    }
                                    if (allNgCountNow >= allNgCount)
                                    {
                                        string nozzleNgInfo = $"N1:{_stepStatus.NozzleInhaleNotReadyTimes2[0]} N2:{_stepStatus.NozzleInhaleNotReadyTimes2[1]} N3:{_stepStatus.NozzleInhaleNotReadyTimes2[2]} N4:{_stepStatus.NozzleInhaleNotReadyTimes2[3]}";
                                        var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, $"所有吸嘴连续{allNgCountNow}次下视觉NG", 110);
                                        if (MessageBox.Show($"所有吸嘴连续{allNgCountNow}次下视觉NG！\n确定继续吗？\n{nozzleNgInfo}", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                        {
                                            _baseBiz.RemoveRunAlarm(alarm);
                                            for (int i = 0; i < 4; i++)
                                            {
                                                _stepStatus.NozzleInhaleNotReadyTimes2[i] = 0;
                                            }
                                        }
                                        else
                                        {
                                            _baseBiz.RemoveRunAlarm(alarm);
                                            return;
                                        }
                                    }
                                }
                                else
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"DownCam->定位{nozzleIdx + 1}号吸嘴上的物料成功，Axis[X2,Y2,Z2,R{nozzleIdx + 1}]:{NLogTrace.GetFloatArrayString(downCamPos)}", En_Logout_Type.Run, true);
                                    _stepStatus.NozzleInhaleNotReadyTimes1[nozzleIdx] = 0;
                                }
                                throwTapeNozzleNoSet.Add(nozzleIdx + 1);
                            }
                        }
                        if (!_mGoogol_Component.SetDownLight(false))
                        {
                            MessageBox.Error("下视觉光源关闭失败！");
                            return;
                        }
                        if (!_mGoogol_Component.SetAllCylindersUpDown(true))
                        {
                            MessageBox.Error("所有气缸上失败！");
                            return;
                        }
                        #endregion
                        #region 抛料
                        bool throwCountOver0 = throwTapeNozzleNoSet.Count > 0;
                        if (throwTapeNozzleNoSet.Count > 0 && !_stepStatus.ThrowTapes(throwTapeNozzleNoSet))
                        {
                            MessageBox.Error("抛料失败！");
                            return;
                        }
                        if (throwCountOver0 && !startNextTest)
                        {
                            if (!_mGoogol_Component.SetAllCylindersUpDown(true))
                            {
                                MessageBox.Error("所有气缸上失败！");
                                return;
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
                            return;
                        }
                        #endregion
                    }
                }
                finally
                {
                    isMoving = false;
                }
            });
        }

        private bool MoveToFeederSingleTape(int nozzleNo, bool waitXYZREnd, bool moveAllR, float[] floats)
        {
            if (moveAllR)
            {
                //所有启用的吸嘴转到取料角度，防止与其他吸嘴碰到，不用等待结束
                for (int nozzleNo0 = 1; nozzleNo0 <= 4; nozzleNo0++)
                {
                    if (nozzleNo0 == nozzleNo)
                    {
                        continue;
                    }
                    if (_stepStatus.UseNozzle(nozzleNo0))
                    {
                        float feederPosR = _stepStatus.FeederSingleTapePos(nozzleNo0, 0)[3];
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis((En_AxisNum)nozzleNo0 + 4, feederPosR, false))
                        {
                            MessageBox.Error($"运动到取料吸嘴R失败：[R{nozzleNo0}]{feederPosR.ToString("f3")}");
                            return false;
                        }
                    }
                }
            }

            float[] feederPos;
            //移动到视觉结果的坐标
            feederPos = _stepStatus.FeederSingleTapePos(nozzleNo);
            if (floats != null)
            {
                feederPos[0] = floats[0];
                feederPos[1] = floats[1];
                feederPos[3] = floats[2];
            }
            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1 + nozzleNo - 1, feederPos, waitXYZREnd))
            {
                MessageBox.Error($"运动到{nozzleNo}号吸嘴 Feeder Pick 坐标失败");
                return false;
            }
            return true;
        }

        public void StopAllNozzlesPickTest()
        {
            startNextTest = false;
        }

        public void Test()
        {
            _camera_Component.CavGroup("C1", "12", "F1", "1");
        }

        #endregion
    }
}
