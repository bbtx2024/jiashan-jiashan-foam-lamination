using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq.Expressions;
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
using QA.Business.Message;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.Calibration.ViewModels
{
    [Export(typeof(ICalibrationViewModel))]
    public class JointCalibViewModel : Screen, INotifyPropertyChanged, ICalibrationViewModel, IHandle<LoginSuccessMessage>
    {
        #region Field
        private readonly IEventAggregator _eventAggregator;
        private Camera_Component _camera_Component;
        private ParamManager _paramManager;
        private MotionGoogolParam _mGoogolParam;
        private MotionGoogol_Component _mGoogol_Component;
        private CacheParamManager _cacheParamManager;
        private MyUserManager _myUserManager;
        private PLC_Component _plc_Component;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "联合标定";
        public ushort OrderID { get; set; } = 1;
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
        private ObservableCollection<NozzleCalibModel> _nozzleCalibModelList = new ObservableCollection<NozzleCalibModel>();
        public ObservableCollection<NozzleCalibModel> NozzleCalibModelList
        {
            get => _nozzleCalibModelList;
            set
            {
                _nozzleCalibModelList = value;
                NotifyOfPropertyChange(() => NozzleCalibModelList);
            }
        }
        #endregion

        #region Constructor
        public JointCalibViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _paramManager = IoC.Get<ParamManager>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _myUserManager = IoC.Get<MyUserManager>();
            _mGoogolParam = IoC.Get<MotionGoogolParam>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();

            NozzleCalibModel nozzleCalibModel = new NozzleCalibModel()
            {
                NozzleId = NozzleId.一号吸嘴,
                CurFeeder1JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos),
                CurFeeder2JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos),
                CurCarrierJointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo1),
                CurNozzleNo1SucPiecePos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1),
                AxisCalibJoint_NozzleNoPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos),
            };
            NozzleCalibModel nozzleCalibModel2 = new NozzleCalibModel()
            {
                NozzleId = NozzleId.二号吸嘴,
                CurFeeder1JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos),
                CurFeeder2JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos),
                CurCarrierJointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo2),
                CurNozzleNo1SucPiecePos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2),
                AxisCalibJoint_NozzleNoPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos),
            };
            NozzleCalibModelList.Add(nozzleCalibModel);
            NozzleCalibModelList.Add(nozzleCalibModel2);
        }
        #endregion

        #region Handle
        public void Handle(LoginSuccessMessage message)
        {
            EnableButtons = _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeManager;
        }
        #endregion

        #region Method


        #region 点位设置及移动
        public void SetFeeder1CalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定设置{nozzleCalibModel.NozzleId}Feeder1取放标定片位置吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                    nozzleCalibModel.CurFeeder1JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos);
                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R2];
                    nozzleCalibModel.CurFeeder1JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos);
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetFeeder1CalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"{nozzleCalibModel.NozzleId}Feeder1取放标定片坐标为：{nozzleCalibModel.CurFeeder1JointCalibPos}", En_Logout_Type.SpotCheck);
            }
        }
        public async void MoveSpaceFeeder1CalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定空移{nozzleCalibModel.NozzleId}Feeder1取放标定片位置吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos;
                En_AxisNum en_AxisNumR = En_AxisNum.R1;
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {

                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos;
                    en_AxisNumR = En_AxisNum.R2;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceFeeder1CalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                await Task.Run(() =>
            {
                if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                {
                    MessageBox.Error("轴系在运动，等静止再操作！");
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
                if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[3] { pos[0], pos[1], pos[3] }, true))
                {
                    MessageBox.Error("移动失败！");
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
                {
                    MessageBox.Error($"Z2移动到{pos[2]}失败！");
                    return;
                }
                MessageBox.Success("移动完毕！");
            });
            }
        }
        public void SetFeeder2CalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定设置{nozzleCalibModel.NozzleId}Feeder2取放标定片位置吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                    nozzleCalibModel.CurFeeder2JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos);
                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R2];
                    nozzleCalibModel.CurFeeder2JointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos);
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetFeeder2CalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"{nozzleCalibModel.NozzleId}Feeder2取放标定片位置坐标为：{nozzleCalibModel.CurFeeder2JointCalibPos}", En_Logout_Type.SpotCheck);
            }
        }
        public async void MoveSpaceFeeder2CalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定空移{nozzleCalibModel.NozzleId}Feeder2取放标定片位置吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos;
                En_AxisNum en_AxisNumR = En_AxisNum.R1;
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {

                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos;
                    en_AxisNumR = En_AxisNum.R2;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceFeeder2CalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("轴系在运动，等静止再操作！");
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
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[3] { pos[0], pos[1], pos[3] }, true))
                    {
                        MessageBox.Error("移动失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
                    {
                        MessageBox.Error($"Z2移动到{pos[2]}失败！");
                        return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        public void SetFovPlateInCarrierPosToUpCamera(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定设置{nozzleCalibModel.NozzleId}上相机载具标定片坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo1[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo1[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    nozzleCalibModel.CurCarrierJointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo1);
                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo2[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                    _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo2[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                    nozzleCalibModel.CurCarrierJointCalibPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo2);
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetFovPlateInCarrierPosToUpCamera({nozzleCalibModel.NozzleId})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设置{nozzleCalibModel.NozzleId}上相机载具标定片坐标：{nozzleCalibModel.CurCarrierJointCalibPos}", En_Logout_Type.SpotCheck);
            }
        }

        public async void MoveSpaceFovPlateInCarrierPosToUpCamera(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定空移{nozzleCalibModel.NozzleId}上相机载具标定片坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo1;
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {

                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo2;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceFovPlateInCarrierPosToUpCamera({nozzleCalibModel.NozzleId})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("轴系在运动，等静止再操作！");
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
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(pos, true))
                    {
                        MessageBox.Error("移动失败！");
                        return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        public void SetNozzleNo1SucPieceCalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定设置{nozzleCalibModel.NozzleId}载具取放标定片位置吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                    nozzleCalibModel.CurNozzleNo1SucPiecePos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1);
                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R2];
                    nozzleCalibModel.CurNozzleNo1SucPiecePos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2);
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetNozzleNo1SucPieceCalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设置{nozzleCalibModel.NozzleId}载具取放标定片位置：{nozzleCalibModel.CurNozzleNo1SucPiecePos}", En_Logout_Type.SpotCheck);
            }
        }

        public async void MoveSpaceNozzleNo1SucPieceCalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定空移{nozzleCalibModel.NozzleId}载具取放标定片位置吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1;
                En_AxisNum en_AxisNumR = En_AxisNum.R1;
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {

                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2;
                    en_AxisNumR = En_AxisNum.R2;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceNozzleNo1SucPieceCalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("轴系在运动，等静止再操作！");
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
                    if (!_mGoogol_Component.SafeAvoid())
                    {
                        MessageBox.Error("相机轴安全避让失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[3] { pos[0], pos[1], pos[3] }, true))
                    {
                        MessageBox.Error("移动失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
                    {
                        MessageBox.Error($"Z2移动到{pos[2]}失败！");
                        return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        public void SetNozzleNo1CalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定设置{nozzleCalibModel.NozzleId}下视觉标定坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                    nozzleCalibModel.AxisCalibJoint_NozzleNoPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos);
                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R2];
                    nozzleCalibModel.AxisCalibJoint_NozzleNoPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos);
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetNozzleNo1CalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设置{nozzleCalibModel.NozzleId}下视觉标定坐标为：{nozzleCalibModel.AxisCalibJoint_NozzleNoPos}", En_Logout_Type.SpotCheck);
            }
        }


        public async void MoveSpaceNozzleCalibPos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定空移{nozzleCalibModel.NozzleId}下视觉标定坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos;
                En_AxisNum en_AxisNumR = En_AxisNum.R1;
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {

                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    pos = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos;
                    en_AxisNumR = En_AxisNum.R2;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceNozzleCalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("轴系在运动，等静止再操作！");
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
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[3] { pos[0], pos[1], pos[3] }, true))
                    {
                        MessageBox.Error("移动失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
                    {
                        MessageBox.Error($"Z2移动到{pos[2]}失败！");
                        return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }
        public void SetTTNNozzlePos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定设置{nozzleCalibModel.NozzleId}训练坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.TTNNozzleNo1Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.TTNNozzleNo1Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.TTNNozzleNo1Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.TTNNozzleNo1Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                    nozzleCalibModel.TTNNozzleNoPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.TTNNozzleNo1Pos);
                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    _cacheParamManager.manualPositionParam.TTNNozzleNo2Pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                    _cacheParamManager.manualPositionParam.TTNNozzleNo2Pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                    _cacheParamManager.manualPositionParam.TTNNozzleNo2Pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                    _cacheParamManager.manualPositionParam.TTNNozzleNo2Pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R2];
                    nozzleCalibModel.TTNNozzleNoPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.TTNNozzleNo2Pos);
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nSetNozzleNo1CalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设置{nozzleCalibModel.NozzleId}下视觉标定坐标为：{nozzleCalibModel.TTNNozzleNoPos}", En_Logout_Type.SpotCheck);
            }
        }

        public async void MoveSpaceTTNNozzlePos(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定空移{nozzleCalibModel.NozzleId}下视觉标定坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = _cacheParamManager.manualPositionParam.TTNNozzleNo1Pos;
                En_AxisNum en_AxisNumR = En_AxisNum.R1;
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {

                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    pos = _cacheParamManager.manualPositionParam.TTNNozzleNo2Pos;
                    en_AxisNumR = En_AxisNum.R2;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceNozzleCalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Error("轴系在运动，等静止再操作！");
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
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[3] { pos[0], pos[1], pos[3] }, true))
                    {
                        MessageBox.Error("移动失败！");
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
                    {
                        MessageBox.Error($"Z2移动到{pos[2]}失败！");
                        return;
                    }
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        /// <summary>
        /// 吸嘴训练
        /// </summary>
        /// <param name="nozzleCalibModel"></param>
        public async void StarTTN(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定开始训练{nozzleCalibModel.NozzleId}吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = _cacheParamManager.manualPositionParam.TTNNozzleNo1Pos;
                En_AxisNum en_AxisNumR = En_AxisNum.R1;
                if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                {

                }
                else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                {
                    pos = _cacheParamManager.manualPositionParam.TTNNozzleNo2Pos;
                    en_AxisNumR = En_AxisNum.R2;
                }
                else
                {
                    MessageBox.Error($"程序有误，需修改软件！\nMoveSpaceNozzleCalibPos({nozzleCalibModel.NozzleId})");
                    return;
                }
                await Task.Run(() =>
            {
                if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                {
                    MessageBox.Error("轴系在运动，等静止再操作！");
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
                if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[3] { pos[0], pos[1], pos[3] }, true))
                {
                    MessageBox.Error("移动失败！");
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
                {
                    MessageBox.Error($"Z2移动到{pos[2]}失败！");
                    return;
                }
                if (!_mGoogol_Component.SetCylinderUpDown((int)nozzleCalibModel.NozzleId, false))
                {
                    MessageBox.Error($"{nozzleCalibModel.NozzleId}气缸下失败！");
                    return;
                }
                Thread.Sleep(2000);
                if (!_camera_Component.StarTTN(nozzleCalibModel.NozzleId, pos[0], pos[1], pos[3], 60000))
                {
                    MessageBox.Error("发送联合标定失败,详情查看日志");
                    return;
                }
            });
            }
        }
        #endregion

        /// <summary>
        /// 取放标定板
        /// </summary>
        /// <param name="pick"></param>
        /// <param name="nozzleId"></param>
        private void pickandplace(bool pick, NozzleId nozzleId, float[] pos, string posName)
        {
            En_AxisNum en_AxisNumR = En_AxisNum.R1;
            if (nozzleId == NozzleId.一号吸嘴)
            {

            }
            else if (nozzleId == NozzleId.二号吸嘴)
            {
                en_AxisNumR = En_AxisNum.R2;
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

            if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[] { pos[0], pos[1], pos[3] }, true))
            {
                MessageBox.Error($"吸嘴轴移动到{nozzleId}{posName}X,Y,R坐标点位失败！");
                return;
            }
            if (!_mGoogol_Component.SetCylinderUpDown((int)nozzleId, false))
            {
                MessageBox.Error($"{nozzleId}气缸下失败！");
                return;
            }
            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true))
            {
                MessageBox.Error($"吸嘴轴移动Z到{pos}坐标点位失败！");
                return;
            }
            Thread.Sleep(100);
            if (pick)
            {
                if (!_mGoogol_Component.SetVacuum((int)nozzleId, true))
                {
                    MessageBox.Error($"{nozzleId}开吸关吹失败！");
                    return;
                }
                Thread.Sleep(1000);
            }
            else
            {

                if (!_mGoogol_Component.SetVacuum((int)nozzleId, false))
                {
                    MessageBox.Error($"{nozzleId}关吸开吹失败！");
                    return;
                }
                Thread.Sleep(1000);
                if (!_mGoogol_Component.SetVacuum((int)nozzleId, false, false))
                {
                    MessageBox.Error($"{nozzleId}关吹失败！");
                    return;
                }
            }
            if (!_mGoogol_Component.SetCylinderUpDown((int)nozzleId, true))
            {
                MessageBox.Error($"{nozzleId}气缸上失败！");
                return;
            }
            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            {
                MessageBox.Error("Z2移动到0失败！");
                return;
            }
        }

        public async void SetAutoJointCalib(NozzleCalibModel nozzleCalibModel)
        {
            if (MessageBox.Show($"确定开始{nozzleCalibModel.NozzleId}自动联合标定吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        En_AxisNum en_AxisNumR = En_AxisNum.R1;
                        float[] feeder1Pos = _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder1Pos;
                        float[] feeder2Pos = _cacheParamManager.manualPositionParam.AxisNozzle1CalibFeeder2Pos;
                        float[] nozzlePiecePos = _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1;
                        float[] carrierCddPos = _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo1;
                        float[] downPos = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos;
                        if (nozzleCalibModel.NozzleId == NozzleId.一号吸嘴)
                        {

                        }
                        else if (nozzleCalibModel.NozzleId == NozzleId.二号吸嘴)
                        {
                            en_AxisNumR = En_AxisNum.R2;
                            feeder1Pos = _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder1Pos;
                            feeder2Pos = _cacheParamManager.manualPositionParam.AxisNozzle2CalibFeeder2Pos;
                            nozzlePiecePos = _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2;
                            carrierCddPos = _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo2;
                            downPos = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo2Pos;
                        }
                        if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                        {
                            MessageBox.Error("轴系在运动，等静止再操作！");
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

                        //一号吸嘴 发Calib,1  收rCalib,1
                        //二号吸嘴 发Calib,2  收rCalib,2


                        //----------------------------------------------------------------------------------第一步：将标定板放在吸嘴上
                        if (!_mGoogol_Component.SetVacuum((int)nozzleCalibModel.NozzleId, true, true))
                        {
                            MessageBox.Error($"{nozzleCalibModel.NozzleId}开吸失败！");
                            return;
                        }
                        Thread.Sleep(1000);
                        if (!_mGoogol_Component.GetMaterialReady((int)nozzleCalibModel.NozzleId))
                        {
                            MessageBox.Error($"{nozzleCalibModel.NozzleId}真空吸异常！");
                            return;
                        }
                        //----------------------------------------------------------------------------------第二步：标定流程开始
                        //标定流程开始，上位机给视觉发送指令：Calib,1,SC,0,0,0,0,0
                        //视觉软件返回指令：rCalib,1,SC,OK/NG
                        if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "SC", 0, 0, 0, 0, 0))
                        {
                            MessageBox.Error("发送开始联合标定失败");
                            return;
                        }
                        //----------------------------------------------------------------------------------第三步：Feeder相机拍Feeder1棋牌格
                        //吸嘴轴把棋盘格放在Feeder1标定取放位
                        //放标定板
                        pickandplace(false, nozzleCalibModel.NozzleId, feeder1Pos, "Feeder1取放标定片");
                        //吸嘴轴移动到Feeder拍照位避让相机拍照,相机气缸缩(原点)
                        float[] feederPhtotPos =  _cacheParamManager.manualPositionParam.AxisFeeder1Pos ;
                        _plc_Component.FeederCDDCylinderSS(FeederId.左飞达);
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(feederPhtotPos, true))
                        {
                            MessageBox.Error($"吸嘴轴移动到Feeder拍照坐标X,Y失败！");
                            return;
                        }
                        //上位机发送指令Calib,1,HBF,2,0,[X],[Y],[R](X,Y,R是Feeder1标定取放位)
                        //视觉软件返回指令：rCalib,1,HBF,OK / NG
                        if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "HBF", 2, 0, feeder1Pos[0], feeder1Pos[1], feeder1Pos[3]))
                        {
                            MessageBox.Error("发送联合标定失败,详情查看日志");
                            return;
                        }
                        //----------------------------------------------------------------------------------第四步：Feeder相机拍Feeder2棋牌格
                        //吸嘴轴从Feeder1标定取放位把棋盘格放在Feeder2标定取放位
                        //取标定片
                        pickandplace(true, nozzleCalibModel.NozzleId, feeder1Pos, "Feeder1取放标定片");
                        //放标定片
                        pickandplace(false, nozzleCalibModel.NozzleId, feeder2Pos, "Feeder2取放标定片");
                        feederPhtotPos = _cacheParamManager.manualPositionParam.AxisFeeder2Pos;
                        //吸嘴轴移动到Feeder拍照位避让相机拍照,相机气缸伸(动点)
                        _plc_Component.FeederCDDCylinderSS(FeederId.右飞达);
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2(feederPhtotPos, true))
                        {
                            MessageBox.Error($"吸嘴轴移动到Feeder拍照坐标X,Y失败！");
                            return;
                        }
                        //上位机发送指令Calib,1,HBF,3,0,[X],[Y],[R](X,Y,R是Feeder2标定取放位)
                        //视觉软件返回指令：rCalib,1,HBF,OK/NG
                        if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "HBF", 3, 0, feeder2Pos[0], feeder2Pos[1], feeder2Pos[3]))
                        {
                            MessageBox.Error("发送联合标定失败,详情查看日志");
                            return;
                        }
                        //----------------------------------------------------------------------------------第五步：载具定位相机拍载具棋牌格
                        //吸嘴轴从Feeder2标定取放位把棋盘格放在载具标定取放位
                        //取标定片
                        pickandplace(true, nozzleCalibModel.NozzleId, feeder2Pos, "Feeder2取放标定片");
                        if (!_mGoogol_Component.SafeAvoid())
                        {
                            MessageBox.Error("相机轴安全避让失败！");
                            return;
                        }
                        //放标定片
                        pickandplace(false, nozzleCalibModel.NozzleId, nozzlePiecePos, "载具取放标定片");
                        //拍照轴移动到载具标定拍照位
                        if (!_mGoogol_Component.Station2InSafeArea() && !_mGoogol_Component.MoveY2ToSafePos())
                        {
                            MessageBox.Error("Y2移动到安全位置失败！");
                            return;
                        }
                        if (!_mGoogol_Component.MoveAbsoluteX1Y1(carrierCddPos, true))
                        {
                            MessageBox.Error($"拍照轴移动到载具标定拍照位坐标X,Y失败！");
                            return;
                        }
                        //上位机发送载具标定位拍照位的坐标(待定)

                        //上位机发送指令Calib,1,HBF,4,0,[X],[Y],[R],[X2],[Y2](X, Y, R是载具标定取放位)
                        //视觉软件返回指令：rCalib,1,HBF,OK/NG
                        if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "HBF", 4, 0, nozzlePiecePos[0], nozzlePiecePos[1], nozzlePiecePos[3], carrierCddPos[0], carrierCddPos[1]))
                        {
                            MessageBox.Error("发送联合标定失败,详情查看日志");
                            return;
                        }
                        //----------------------------------------------------------------------------------第六步：下视觉12点标定
                        //吸嘴轴从载具标定取放位取棋盘格移动到下视觉标定拍照位
                        if (!_mGoogol_Component.SafeAvoid())
                        {
                            MessageBox.Error("相机轴安全避让失败！");
                            return;
                        }
                        //取标定片
                        pickandplace(true, nozzleCalibModel.NozzleId, nozzlePiecePos, "载具取放标定片");
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, new float[] { downPos[0], downPos[1], downPos[3] }, true))
                        {
                            MessageBox.Error($"吸嘴轴移动到{nozzleCalibModel.NozzleId}下视觉标定位X,Y,R坐标点位失败！");
                            return;
                        }
                        if (!_mGoogol_Component.SetCylinderUpDown((int)nozzleCalibModel.NozzleId, false))
                        {
                            MessageBox.Error($"{nozzleCalibModel.NozzleId}气缸下失败！");
                            return;
                        }
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, downPos[2], true))
                        {
                            MessageBox.Error($"吸嘴轴移动到{nozzleCalibModel.NozzleId}下视觉标定位Z坐标点位失败！");
                            return;
                        }
                        Thread.Sleep(1000);
                        //上位机发送指令Calib,1,HBM,1,0,[X],[Y],[R](X, Y, R是下视觉标定拍照位) 
                        //视觉软件返回指令：rCalib,1,HBM,OK/NG
                        if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "HBM", 1, 0, downPos[0], downPos[1], downPos[3]))
                        {
                            MessageBox.Error("发送联合标定失败,详情查看日志");
                            return;
                        }
                        float movdis = 5.0f;//十二点标定xy偏移5mm
                        float angle = 15.0f;//十二点标定角度偏移15度
                        float[][] calibPoints = new[] {
                                                            new [] { 0f, 0f,  0f },
                                                            new [] { movdis, 0f, 0f },
                                                            new [] { 0f, movdis,  0f },
                                                            new [] { -movdis, 0f,  0f },
                                                            new [] { -movdis, 0f, 0f },
                                                            new [] { 0f, -movdis,  0f },
                                                            new [] { 0f, -movdis,  0f },
                                                            new [] { movdis, 0f,  0f },
                                                            new [] { movdis, 0f,  0f },
                                                            new [] { -movdis, movdis, 0f },//XY回到原点,R旋转
                                                            new [] { 0f, 0f,  angle },
                                                            new [] { 0f, 0f,  angle },
                                                             new [] { 0f, 0f,  angle },
                                                            new [] { 0f, 0f,  angle },
                                                        };

                        //上位机发送指令Calib,1,HNM,1,1(1-12),[X],[Y],[R](X, Y, R是下视觉标定拍照位)    吸嘴吸着标定片做9点标定加上旋转标定，如共9+3点，则1-9为平移、10-12为旋转
                        //视觉软件返回指令：rCalib,1,HNM,OK/NG
                        float[] pos = new float[] { downPos[0], downPos[1], downPos[3] };
                        for (int i = 0; i < calibPoints.Length; i++)
                        {
                            pos[0] += calibPoints[i][0];
                            pos[1] += calibPoints[i][1];
                            pos[2] += calibPoints[i][2];
                            if (!_mGoogol_Component.MoveAbsoluteX2Y2R(en_AxisNumR, pos, true))
                            {
                                MessageBox.Error($"吸嘴轴移动到十二点标定点位第{i + 1}个xyr失败！");
                                return;
                            }
                            Thread.Sleep(3000);
                            if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "HNM", 1, i + 1, pos[0], pos[1], pos[2]))
                            {
                                MessageBox.Error("发送联合标定失败,详情查看日志");
                                return;
                            }
                            Thread.Sleep(1000);
                        }
                        if (!_mGoogol_Component.SetCylinderUpDown((int)nozzleCalibModel.NozzleId, true))
                        {
                            MessageBox.Error($"{nozzleCalibModel.NozzleId}气缸上失败！");
                            return;
                        }
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
                        {
                            MessageBox.Error("Z2移动到0失败！");
                            return;
                        }
                        //----------------------------------------------------------------------------------第七步：标定流程结束
                        //上位机发送多点标定结束指令： Calib,1,HNME,1,0,0,0,0  视觉返回指令：rCalib,1,HNME,OK/NG
                        if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "HNME", 1, 0, 0, 0, 0, 600000))
                        {

                        }
                        MessageBox.Info("等待视觉完成,点击确定！");
                        Thread.Sleep(1000);
                        //上位机发送标定结束指令： Calib,1,EC,0,0,0,0,0  视觉返回指令：rCalib,1,EC,OK/NG
                        if (!_camera_Component.SendJointCalib(nozzleCalibModel.NozzleId, "EC", 0, 0, 0, 0, 0, 600000))
                        {
                        }
                        MessageBox.Info("联合标定成功");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Error($"联合标定过程中出现异常：{ex}");
                    }
                    finally
                    {

                    }
                });
            }
            //if (MessageBox.Show($"确定开始自动联合标定吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            //{
            //    await Task.Run(() =>
            //    {
            //        try
            //        {
            //            //点1上视觉->从点1吸取标定片->标定片放到点2->
            //            //点2上视觉->从点2吸取标定片->标定片放到点3->
            //            //点3上视觉->从点3吸取标定片->标定片放到点4->
            //            //点4上视觉->从点4吸取标定片->下视觉11点标定
            //            if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
            //            {
            //                MessageBox.Error("轴系在运动，等静止再操作！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.CleanAlarm())
            //            {
            //                MessageBox.Error("清除报警失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid))
            //            {
            //                MessageBox.Error("设置轴速为示教中速失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetUpLightHX(true))
            //            {
            //                MessageBox.Error("上相机环形光源打开失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetUpLightTZ(true))
            //            {
            //                MessageBox.Error("上相机同轴光源打开失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetDownLight(false))
            //            {
            //                MessageBox.Error("下相机光源关闭失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            //            {
            //                MessageBox.Error("Z2移动到0失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetAllCylindersUpDown(true))
            //            {
            //                MessageBox.Error("所有气缸上失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.Station2InSafeArea() && !_mGoogol_Component.MoveY2ToSafePos())
            //            {
            //                MessageBox.Error("Y2移动到安全位置失败！");
            //                return;
            //            }
            //            if (!_camera_Component.SendJointScCalib())
            //            {
            //                MessageBox.Error("发送开始联合标定失败");
            //                return;
            //            }
            //            var pickPoses = new[]
            //            {
            //                _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo1,
            //                _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo2,
            //                _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo3,
            //                _cacheParamManager.manualPositionParam.AxisNozzleNo1SucPiecePosNo4,
            //            };
            //            var upCamPoses = new[]
            //            {
            //                _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo1,
            //                _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo2,
            //                _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo3,
            //                _cacheParamManager.manualPositionParam.AxisCarrierJointCalibPosNo4,
            //            };
            //            for (int i = 0; i < 4; i++)
            //            {
            //                //1.上相机移动并识别标定片
            //                if (!_mGoogol_Component.MoveAbsoluteX1Y1(upCamPoses[i], true))
            //                {
            //                    MessageBox.Error($"相机轴移动到第{i + 1}个标定片点位失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);
            //                if (!_camera_Component.CalibProcess(i + 1, pickPoses[i][0], pickPoses[i][1], pickPoses[i][3]))
            //                {
            //                    MessageBox.Error($"标定第{i + 1}个标定片点位吸嘴轴坐标失败！");
            //                    return;
            //                }
            //                int a = 7;
            //                if (_paramManager.MESParam.TapeNames == TapeNames.Flex_GND_Tape)
            //                {
            //                    a = 6;
            //                }
            //                if (!_camera_Component.CalibProcessSendSet(i + 1, a, upCamPoses[i][0], upCamPoses[i][1], upCamPoses[i][3]))
            //                {
            //                    MessageBox.Error($"标定第{i + 1}个标定片点位相机轴坐标并视觉标定失败！");
            //                    return;
            //                }
            //                //2.吸嘴吸取标定片
            //                if (!_mGoogol_Component.SafeAvoid(pickPoses[i][0], true))
            //                {
            //                    MessageBox.Error("相机轴安全避让失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.MoveAbsoluteX2Y2R(En_AxisNum.R1, new[] { pickPoses[i][0], pickPoses[i][1], pickPoses[i][3] }, true))
            //                {
            //                    MessageBox.Error($"吸嘴轴移动到第{i + 1}个标定片点位xyr失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.SetCylinderUpDown(1, false))
            //                {
            //                    MessageBox.Error($"气缸下失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pickPoses[i][2], true))
            //                {
            //                    MessageBox.Error($"吸嘴轴移动到第{i + 1}个标定片点位z失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.SetVacuum(1, true, true))
            //                {
            //                    MessageBox.Error($"开吸失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);
            //                if (!_mGoogol_Component.SetCylinderUpDown(1, true))
            //                {
            //                    MessageBox.Error($"气缸上失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);//防止连续运动导致标定片偏移
            //                if (!_mGoogol_Component.GetMaterialReady(1))
            //                {
            //                    MessageBox.Error($"吸嘴1未吸到标定片！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            //                {
            //                    MessageBox.Error($"Z2移动到0失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);//防止连续运动导致标定片偏移
            //                if (i != 3)
            //                {
            //                    //3.将标定片放到下一个位置
            //                    if (!_mGoogol_Component.SafeAvoid(pickPoses[i + 1][0], true))
            //                    {
            //                        MessageBox.Error("相机轴安全避让失败！");
            //                        return;
            //                    }
            //                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R(En_AxisNum.R1, new[] { pickPoses[i + 1][0], pickPoses[i + 1][1], pickPoses[i + 1][3] }, true))
            //                    {
            //                        MessageBox.Error($"吸嘴轴移动到第{i + 2}个标定片点位xyr失败！");
            //                        return;
            //                    }
            //                    Thread.Sleep(300);//防止连续运动导致标定片偏移
            //                    if (!_mGoogol_Component.SetCylinderUpDown(1, false))
            //                    {
            //                        MessageBox.Error($"气缸下失败！");
            //                        return;
            //                    }
            //                    Thread.Sleep(300);//防止连续运动导致标定片偏移
            //                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pickPoses[i + 1][2], true))
            //                    {
            //                        MessageBox.Error($"吸嘴轴移动到第{i + 2}个标定片点位z失败！");
            //                        return;
            //                    }
            //                    Thread.Sleep(300);
            //                    if (!_mGoogol_Component.SetVacuum(1, false))
            //                    {
            //                        MessageBox.Error($"关吸开吹失败！");
            //                        return;
            //                    }
            //                    Thread.Sleep(500);
            //                    if (!_mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.Z2, -2, true))
            //                    {
            //                        MessageBox.Error($"Z2抬高2mm失败！");
            //                        return;
            //                    }
            //                    if (!_mGoogol_Component.SetVacuum(1, false, false))
            //                    {
            //                        MessageBox.Error($"关吹失败！");
            //                        return;
            //                    }
            //                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            //                    {
            //                        MessageBox.Error($"Z2移动到0失败！");
            //                        return;
            //                    }
            //                    if (!_mGoogol_Component.SetCylinderUpDown(1, true))
            //                    {
            //                        MessageBox.Error($"气缸上失败！");
            //                        return;
            //                    }
            //                    if (!_mGoogol_Component.Station2InSafeArea() && !_mGoogol_Component.MoveY2ToSafePos())
            //                    {
            //                        MessageBox.Error("Y2移动到安全位置失败！");
            //                        return;
            //                    }
            //                }
            //            }
            //            //4.十一点标定
            //            if (!_mGoogol_Component.SetUpLightHX(false))
            //            {
            //                MessageBox.Error("上相机环形光源关闭失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetUpLightTZ(false))
            //            {
            //                MessageBox.Error("上相机同轴光源关闭失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.SetDownLight(true))
            //            {
            //                MessageBox.Error("下相机光源打开失败！");
            //                return;
            //            }
            //            float movdis = 5.0f;//十一点标定xy偏移5mm
            //            float angle = 10.0f;//十一点标定角度偏移10度
            //            float centerX = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[0];
            //            float centerY = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[1];
            //            float centerZ = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[2];
            //            float centerR = _cacheParamManager.manualPositionParam.AxisCalibJoint_NozzleNo1Pos[3];
            //            float[][] calib11Points = new[] {
            //                new [] { centerX, centerY, centerZ, centerR },
            //                new [] { centerX, centerY + movdis, centerZ, centerR },
            //                new [] { centerX - movdis, centerY + movdis, centerZ, centerR },
            //                new [] { centerX - movdis, centerY, centerZ, centerR },
            //                new [] { centerX - movdis, centerY - movdis, centerZ, centerR },
            //                new [] { centerX, centerY - movdis, centerZ, centerR },
            //                new [] { centerX + movdis, centerY - movdis, centerZ, centerR },
            //                new [] { centerX + movdis, centerY, centerZ, centerR },
            //                new [] { centerX + movdis, centerY + movdis, centerZ, centerR },
            //                new [] { centerX, centerY, centerZ, centerR - angle },
            //                new [] { centerX, centerY, centerZ, centerR + angle },
            //            };
            //            if (!_mGoogol_Component.MoveAbsoluteX2Y2R(En_AxisNum.R1, new[] { calib11Points[0][0], calib11Points[0][1], calib11Points[0][3] }, true))
            //            {
            //                MessageBox.Error($"吸嘴轴移动到十一点标定点位第1个xyr失败！");
            //                return;
            //            }
            //            Thread.Sleep(300);//防止连续运动导致标定片偏移
            //            if (!_mGoogol_Component.SetCylinderUpDown(1, false))
            //            {
            //                MessageBox.Error($"气缸下失败！");
            //                return;
            //            }
            //            Thread.Sleep(300);//防止连续运动导致标定片偏移
            //            for (int i = 0; i < calib11Points.Length; i++)
            //            {
            //                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(En_AxisNum.R1, calib11Points[i], true))
            //                {
            //                    MessageBox.Error($"吸嘴轴移动到十一点标定点位第{i + 1}个xyzr失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);
            //                if (!_camera_Component.CalibProcess(5, calib11Points[i][0], calib11Points[i][1], calib11Points[i][3]))
            //                {
            //                    MessageBox.Error($"十一点标定第{i + 1}个点标定失败");
            //                    return;
            //                }
            //            }
            //            if (!_mGoogol_Component.SetDownLight(false))
            //            {
            //                MessageBox.Error("下相机光源关闭失败！");
            //                return;
            //            }
            //            //Alert_Bumper才启用Fedder相机
            //            if (_paramManager.MESParam.TapeNames == TapeNames.Alert_Bumper)
            //            {
            //                //5.Feeder相机标定
            //                if (!_mGoogol_Component.SetFeederUpLight(true))
            //                {
            //                    MessageBox.Error("Feeder上相机光源打开失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            //                {
            //                    MessageBox.Error($"Z2移动到0失败！");
            //                    return;
            //                }

            //                float[] feederPos = _cacheParamManager.manualPositionParam.AxisCalibFeederPos;
            //                if (!_mGoogol_Component.MoveAbsoluteX2Y2R(En_AxisNum.R1, new[] { feederPos[0], feederPos[1], feederPos[3] }, true))
            //                {
            //                    MessageBox.Error($"吸嘴轴移动到Feeder标定点位xyr失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);//防止连续运动导致标定片偏移
            //                if (!_mGoogol_Component.SetCylinderUpDown(1, false))
            //                {
            //                    MessageBox.Error($"气缸下失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);//防止连续运动导致标定片偏移
            //                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, feederPos[2], true))
            //                {
            //                    MessageBox.Error($"吸嘴轴移动到Feeder标定点位Z失败！");
            //                    return;
            //                }
            //                Thread.Sleep(300);
            //                if (!_mGoogol_Component.SetVacuum(1, false))
            //                {
            //                    MessageBox.Error($"关吸开吹失败！");
            //                    return;
            //                }
            //                Thread.Sleep(500);
            //                if (!_mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.Z2, -2, true))
            //                {
            //                    MessageBox.Error($"Z2抬高2mm失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.SetVacuum(1, false, false))
            //                {
            //                    MessageBox.Error($"关吹失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            //                {
            //                    MessageBox.Error($"Z2移动到0失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.SetCylinderUpDown(1, true))
            //                {
            //                    MessageBox.Error($"气缸上失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(_cacheParamManager.manualPositionParam.AxisFeederPos, true))
            //                {
            //                    MessageBox.Error($"X2移动到0失败！");
            //                    return;
            //                }
            //                if (!_camera_Component.CalibProcess(6, feederPos[0], feederPos[1], feederPos[3]))
            //                {
            //                    MessageBox.Error($"Feeder标定相机轴坐标并视觉标定失败！");
            //                    return;
            //                }
            //                if (!_mGoogol_Component.SetFeederUpLight(false))
            //                {
            //                    MessageBox.Error("Feeder上相机光源打开失败！");
            //                    return;
            //                }
            //            }
            //            //6.结束处理

            //            if (!_mGoogol_Component.SetCylinderUpDown(1, true))
            //            {
            //                MessageBox.Error($"气缸上失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true))
            //            {
            //                MessageBox.Error($"Z2移动到0失败！");
            //                return;
            //            }
            //            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1, 0, true))
            //            {
            //                MessageBox.Error($"R1移动到0失败！");
            //                return;
            //            }
            //            if (!_camera_Component.SendEcCalib())
            //            {
            //                MessageBox.Error("发送结束联合标定失败！");
            //                return;
            //            }
            //            MessageBox.Success("联合标定已完成，请等待视觉软件计算完毕！\n此弹窗可直接关闭");
            //        }
            //        catch (Exception ex)
            //        {
            //            MessageBox.Error($"联合标定过程中出现异常：{ex}");
            //        }
            //        finally
            //        {
            //            _mGoogol_Component.SetUpLightHX(false);
            //            _mGoogol_Component.SetUpLightTZ(false);
            //            _mGoogol_Component.SetDownLight(false);
            //        }
            //    });
            //}
        }

        #endregion
    }


    public class NozzleCalibModel : INotifyPropertyChanged
    {
        #region NotifyOfPropertyChange
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            MemberExpression member = (MemberExpression)property.Body;
            string propName = member.Member.Name;
            NotifyOfPropertyChange(propName);
        }
        #endregion
        private NozzleId _nozzleId;
        /// <summary>
        /// 吸嘴ID
        /// </summary>
        public NozzleId NozzleId
        {
            get => _nozzleId;
            set
            {
                _nozzleId = value;
                NotifyOfPropertyChange(() => NozzleId);
            }
        }
        private string _curFeeder1JointCalibPos;
        /// <summary>
        /// Feeder1标定取放位
        /// </summary>
        public string CurFeeder1JointCalibPos
        {
            get => _curFeeder1JointCalibPos;
            set
            {
                _curFeeder1JointCalibPos = value;
                NotifyOfPropertyChange(() => CurFeeder1JointCalibPos);
            }
        }
        private string _curFeeder2JointCalibPos;
        /// <summary>
        /// Feeder2标定取放位
        /// </summary>
        public string CurFeeder2JointCalibPos
        {
            get => _curFeeder2JointCalibPos;
            set
            {
                _curFeeder2JointCalibPos = value;
                NotifyOfPropertyChange(() => CurFeeder2JointCalibPos);
            }
        }
        private string _curCarrierJointCalibPos;
        /// <summary>
        /// 载具标定拍照位
        /// </summary>
        public string CurCarrierJointCalibPos
        {
            get => _curCarrierJointCalibPos;
            set
            {
                _curCarrierJointCalibPos = value;
                NotifyOfPropertyChange(() => CurCarrierJointCalibPos);
            }
        }
        /// <summary>
        /// 载具标定取放位
        /// </summary>
        private string _curNozzleNo1SucPiecePos;
        public string CurNozzleNo1SucPiecePos
        {
            get => _curNozzleNo1SucPiecePos;
            set
            {
                _curNozzleNo1SucPiecePos = value;
                NotifyOfPropertyChange(() => CurNozzleNo1SucPiecePos);
            }
        }
        /// <summary>
        /// 下视觉标定拍照位
        /// </summary>
        private string _axisCalibJoint_NozzleNoPos;
        public string AxisCalibJoint_NozzleNoPos
        {
            get => _axisCalibJoint_NozzleNoPos;
            set
            {
                _axisCalibJoint_NozzleNoPos = value;
                NotifyOfPropertyChange(() => AxisCalibJoint_NozzleNoPos);
            }
        }
        /// <summary>
        /// 下视觉训练吸嘴拍照位
        /// </summary>
        private string _tTNNozzleNoPos;
        public string TTNNozzleNoPos
        {
            get => _tTNNozzleNoPos;
            set
            {
                _tTNNozzleNoPos = value;
                NotifyOfPropertyChange(() => TTNNozzleNoPos);
            }
        }

    }

}
