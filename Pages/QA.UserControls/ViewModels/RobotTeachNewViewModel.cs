/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-31
 * 说明：（示教界面运动控制逻辑）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Message;
using QA.UserControls.Interfaces;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.UserControls.ViewModels
{
    //public enum EN_WeldingPen
    //{
    //    StationNo1,
    //    StationNo2
    //}

    public class Position_Axias : PropertyChangedBase
    {

        /// <summary>
        /// X坐标
        /// </summary>
        private float _position_X1;
        public float Position_X1
        {
            get { return _position_X1; }
            set
            {
                _position_X1 = value;
                NotifyOfPropertyChange(() => Position_X1);
            }
        }

        /// <summary>
        /// Y坐标
        /// </summary>
        private float _position_Y1;
        public float Position_Y1
        {
            get { return _position_Y1; }
            set
            {
                _position_Y1 = value;
                NotifyOfPropertyChange(() => Position_Y1);
            }
        }

        /// <summary>
        /// X坐标
        /// </summary>
        private float _position_X2;
        public float Position_X2
        {
            get { return _position_X2; }
            set
            {
                _position_X2 = value;
                NotifyOfPropertyChange(() => Position_X2);
            }
        }

        /// <summary>
        /// Y坐标
        /// </summary>
        private float _position_Y2;
        public float Position_Y2
        {
            get { return _position_Y2; }
            set
            {
                _position_Y2 = value;
                NotifyOfPropertyChange(() => Position_Y2);
            }
        }

        /// <summary>
        /// Z坐标
        /// </summary>
        private float _position_Z2;
        public float Position_Z2
        {
            get { return _position_Z2; }
            set
            {
                _position_Z2 = value;
                NotifyOfPropertyChange(() => Position_Z2);
            }
        }

        /// <summary>
        /// Z坐标
        /// </summary>
        private float _position_R1;
        public float Position_R1
        {
            get { return _position_R1; }
            set
            {
                _position_R1 = value;
                NotifyOfPropertyChange(() => Position_R1);
            }
        }

        /// <summary>
        /// Z坐标
        /// </summary>
        private float _position_R2;
        public float Position_R2
        {
            get { return _position_R2; }
            set
            {
                _position_R2 = value;
                NotifyOfPropertyChange(() => Position_R2);
            }
        }

        /// <summary>
        /// Z坐标
        /// </summary>
        private float _position_R3;
        public float Position_R3
        {
            get { return _position_R3; }
            set
            {
                _position_R3 = value;
                NotifyOfPropertyChange(() => Position_R3);
            }
        }

        /// <summary>
        /// Z坐标
        /// </summary>
        private float _position_R4;
        public float Position_R4
        {
            get { return _position_R4; }
            set
            {
                _position_R4 = value;
                NotifyOfPropertyChange(() => Position_R4);
            }
        }
    }

    public enum AxisStroke
    {
        Axis_X1_MaxStroke = 355,
        Axis_Y2_MaxStroke = 480,

    }

    [Export("RobotTeachNewViewModel", typeof(IUserControl))]
    public class RobotTeachNewViewModel : Screen, IUserControl, IHandle<RobotRealStateInfo>/*,IHandle<List<IComponent>>*/
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private MotionGoogol_Component _mGoogol_Component;
        private PLC_Component _plc_Component;

        private readonly float _axisX1MaxStroke = (float)AxisStroke.Axis_X1_MaxStroke;//核心参数不能乱改，容易撞
        private readonly float _axisY2MaxStroke = (float)AxisStroke.Axis_Y2_MaxStroke;//核心参数不能乱改，容易撞
        private readonly float _axisY1Y2SafeMaxDis = 250 + 140;

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        public override string DisplayName { get; set; } = "示教操作";

        private En_SpeedType[] _speedTypeRecord = new En_SpeedType[] { En_SpeedType.Low, En_SpeedType.Low };

        private ObservableCollection<En_SpeedType> _speedTypes = new ObservableCollection<En_SpeedType>();
        public ObservableCollection<En_SpeedType> SpeedTypes
        {
            get => _speedTypes;
            set
            {
                _speedTypes = value;
                NotifyOfPropertyChange(() => SpeedTypes);
            }
        }

        private ObservableCollection<En_StationNo> _weldingPens = new ObservableCollection<En_StationNo>();
        public ObservableCollection<En_StationNo> WeldingPens
        {
            get => _weldingPens;
            set
            {
                _weldingPens = value;
                NotifyOfPropertyChange(() => WeldingPens);
            }
        }

        private En_StationNo _curWeldingPen = En_StationNo.StationNo1;
        public En_StationNo CurWeldingPen
        {
            get { return _curWeldingPen; }
            set
            {
                _curWeldingPen = value;
                NotifyOfPropertyChange(() => CurWeldingPen);

                if (value == En_StationNo.StationNo1)
                    CurSpeedType = _speedTypeRecord[0];
                else if (value == En_StationNo.StationNo2)
                    CurSpeedType = _speedTypeRecord[1];

                if (_curWeldingPen == En_StationNo.StationNo1)
                {
                    Station1Visable = Visibility.Visible;
                    Station2Visable = Visibility.Hidden;
                }
                else
                {
                    Station1Visable = Visibility.Hidden;
                    Station2Visable = Visibility.Visible;
                }
            }
        }

        //速度类型
        private En_SpeedType _curSpeedType = En_SpeedType.Low;
        public En_SpeedType CurSpeedType
        {
            get { return _curSpeedType; }
            set
            {
                _curSpeedType = value;

                if (CurWeldingPen == En_StationNo.StationNo1)
                    _speedTypeRecord[0] = value;
                else if (CurWeldingPen == En_StationNo.StationNo2)
                    _speedTypeRecord[1] = value;

                NotifyOfPropertyChange(() => CurSpeedType);
            }
        }

        //是否点动
        private bool _isDotMode = false;
        public bool IsDotMove
        {
            get { return _isDotMode; }
            set
            {
                _isDotMode = value;
                NotifyOfPropertyChange(() => IsDotMove);
            }
        }

        //点动步长
        private float _dotStepLenth = 0.01f;
        public float DotStepLenth
        {
            get { return _dotStepLenth; }
            set
            {
                _dotStepLenth = value;
                NotifyOfPropertyChange(() => DotStepLenth);
            }
        }

        private Position_Axias _position_Axias = new Position_Axias();
        public Position_Axias Position_Axias
        {
            get => _position_Axias;
            set
            {
                _position_Axias = value;
                NotifyOfPropertyChange(() => _position_Axias);
            }
        }

        private Visibility _Station1Visable = Visibility.Visible;
        public Visibility Station1Visable
        {
            get => _Station1Visable;
            set
            {
                _Station1Visable = value;
                NotifyOfPropertyChange(() => Station1Visable);
            }
        }

        private Visibility _Station2Visable = Visibility.Visible;
        public Visibility Station2Visable
        {
            get => _Station2Visable;
            set
            {
                _Station2Visable = value;
                NotifyOfPropertyChange(() => Station2Visable);
            }
        }

        private bool _isContainerEnabled = true;
        public bool IsContainerEnabled
        {
            get => _isContainerEnabled;
            set
            {
                _isContainerEnabled = value;
                NotifyOfPropertyChange(() => IsContainerEnabled);
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="windowManager"></param>
        [ImportingConstructor]
        public RobotTeachNewViewModel()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();

            CurWeldingPen = En_StationNo.StationNo1;

            foreach (var item in Enum.GetNames(typeof(En_SpeedType)))
            {
                SpeedTypes.Add((En_SpeedType)Enum.Parse(typeof(En_SpeedType), item));
            }

            foreach (var item in Enum.GetNames(typeof(En_StationNo)))
            {
                if (item == En_StationNo.StationNo3.ToString())
                {
                    continue;
                }
                WeldingPens.Add((En_StationNo)Enum.Parse(typeof(En_StationNo), item));
            }

            _eventAggregator.Subscribe(this);
        }

        public void Handle(RobotRealStateInfo message)
        {
            Position_Axias.Position_X1 = message.X1axis;
            Position_Axias.Position_Y1 = message.Y1axis;
            Position_Axias.Position_X2 = message.X2axis;
            Position_Axias.Position_Y2 = message.Y2axis;
            Position_Axias.Position_Z2 = message.Z2axis;
            Position_Axias.Position_R1 = message.R1axis;
            Position_Axias.Position_R2 = message.R2axis;
            Position_Axias.Position_R3 = message.R3axis;
            Position_Axias.Position_R4 = message.R4axis;
        }

        //public void Handle(List<IComponent> message)
        //{
        //if (null == _robot9075s || _robot9075s.Count() == 0)
        //{
        //    _robot9075s = message.FindAll(t => t is Motion9075_Component).OfType<Motion9075_Component>().ToArray();
        //}
        //}

        /// <summary>
        /// ROBOT轴移动/停止
        /// </summary>
        /// <param name="dirmsg"></param>
        /// <param name="isMove"></param>
        public void RobotMove(string dirmsg, bool isMove)
        {
            try
            {
                _mGoogol_Component.CleanAlarm();
                if (_mGoogol_Component.IsReset())
                {
                    return;
                }
                //点动模式
                if (IsDotMove)
                {
                    DotMoveFunc(dirmsg, isMove);
                }
                else//Jog模式
                {
                    JogMoveFunc(dirmsg, isMove);
                }
                Console.WriteLine($"RobotMove {dirmsg} {isMove}");
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.Error, ex.ToString() + ex.StackTrace);
            }
        }
        //防止输出移出控件，停止移动
        public void StopMove(string dirmsg)
        {
            switch (dirmsg)
            {
                case "Forward":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, false);
                    }
                    else
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, false);
                    }
                    break;
                case "Backward":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, false);

                    }
                    else
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, false);
                    }
                    break;
                case "Left":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                    }
                    else
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                    }
                    break;
                case "Right":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                    }
                    else
                    {
                        _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                    }
                    break;
                case "Down":
                    _mGoogol_Component.JogAxis(En_AxisNum.Z2, 1, false);
                    break;
                case "Up":
                    _mGoogol_Component.JogAxis(En_AxisNum.Z2, 1, false);
                    break;
                case "R1Postive":
                    _mGoogol_Component.JogAxis(En_AxisNum.R1, 1, false);
                    break;
                case "R1Negtive":
                    _mGoogol_Component.JogAxis(En_AxisNum.R1, 1, false);
                    break;
                case "R2Postive":
                    _mGoogol_Component.JogAxis(En_AxisNum.R2, 1, false);
                    break;
                case "R2Negtive":
                    _mGoogol_Component.JogAxis(En_AxisNum.R2, 1, false);
                    break;
                //case "R3Postive":
                //    _mGoogol_Component.JogAxis(En_AxisNum.R3, 1, false);
                //    break;
                //case "R3Negtive":
                //    _mGoogol_Component.JogAxis(En_AxisNum.R3, 1, false);
                //    break;
                //case "R4Postive":
                //    _mGoogol_Component.JogAxis(En_AxisNum.R4, 1, false);
                //    break;
                //case "R4Negtive":
                //    _mGoogol_Component.JogAxis(En_AxisNum.R4, 1, false);
                //    break;
            }
        }

        private void JogMoveFunc(string dirmsg, bool isMove)
        {
            switch (dirmsg)
            {
                case "Forward":

                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        _mGoogol_Component.SetJogSpeed(En_AxisNum.Y1, CurSpeedType, true);
                        _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, isMove);
                    }
                    else
                    {

                        _mGoogol_Component.SetJogSpeed(En_AxisNum.Y2, CurSpeedType, false);
                        _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, isMove);
                    }

                    break;
                case "Backward":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {

                        _mGoogol_Component.SetJogSpeed(En_AxisNum.Y1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                        _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, isMove);

                    }
                    else
                    {

                        _mGoogol_Component.SetJogSpeed(En_AxisNum.Y2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                        _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, isMove);
                    }

                    break;
                case "Left":

                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {

                        _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                        _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                    }
                    else
                    {
                        _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                        _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                    }

                    break;
                case "Right":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                        _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                    }
                    else
                    {

                        _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                        _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                    }
                    break;
                case "Down":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.Z2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                    _mGoogol_Component.JogAxis(En_AxisNum.Z2, 1, isMove);
                    break;
                case "Up":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.Z2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                    _mGoogol_Component.JogAxis(En_AxisNum.Z2, 1, isMove);
                    break;
                case "R1Pos":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                    _mGoogol_Component.JogAxis(En_AxisNum.R1, 1, isMove);
                    break;
                case "R1Neg":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                    _mGoogol_Component.JogAxis(En_AxisNum.R1, 1, isMove);
                    break;
                case "R2Pos":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                    _mGoogol_Component.JogAxis(En_AxisNum.R2, 1, isMove);
                    break;
                case "R2Neg":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                    _mGoogol_Component.JogAxis(En_AxisNum.R2, 1, isMove);
                    break;
                //case "R3Pos":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R3, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R3, 1, isMove);
                //    break;
                //case "R3Neg":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R3, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R3, 1, isMove);
                //    break;
                //case "R4Pos":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R4, CurSpeedType /*SpeedTypes[(byte)CurWeldingPen]*/, true);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R4, 1, isMove);
                //    break;
                //case "R4Neg":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R4, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R4, 1, isMove);
                //    break;
            }
        }
        private void JogMoveFuncs(string dirmsg, bool isMove)
        {
            switch (dirmsg)
            {
                case "Forward":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        //对Y1做防撞处理
                        //_mGoogol.SetJogSpeed(En_AxisNum.Y1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                        //_mGoogol.JogAxis(En_AxisNum.Y1, 1, isMove);
                        _mGoogol_Component.GetCurPos();

                        float xLimitVal = 0;
                        if ((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] < 0)
                        {
                            //相机在吸嘴左边
                            xLimitVal = 180;
                        }
                        else
                        { //相机在吸嘴右边
                            xLimitVal = 145;
                        }

                        if (Math.Abs((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) < xLimitVal)
                        {//触发防撞，且相机在吸嘴左侧                                                     
                            if ((390 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                            {
                                //2个Y轴不超过390，也就是处于离合状态
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.Y1, CurSpeedType, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, isMove);
                            }
                            else
                            {
                                //2个Y处于交叉状态，需要停止                              
                                _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, false);
                                if (xLimitVal == 145)
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个Y轴处于交叉状态，检测到撞击，相机在吸嘴右边，停止Y1运动", En_Logout_Type.Alarm, true);
                                }
                                else
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个Y轴处于交叉状态，检测到撞击，相机在吸嘴左边，停止Y1运动", En_Logout_Type.Alarm, true);
                                }

                            }
                        }
                        else //左右安全，走最大距离，综合不能超过472 = 471 + 1
                        {
                            if ((481 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                            {
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.Y1, CurSpeedType, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, isMove);
                            }
                            else
                            {
                                //NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.Info, $"Stop", QA_Infrastructure.En_Logout_Type.SpotCheck, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, false);
                                if (xLimitVal == 145)
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个Y轴处于离合状态检测到碰撞，相机在吸嘴右边，停止Y1运动！", En_Logout_Type.Alarm, true);
                                }
                                else
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个Y轴处于离合状态检测到碰撞，相机在吸嘴左边，停止Y1运动！", En_Logout_Type.Alarm, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        _mGoogol_Component.SetJogSpeed(En_AxisNum.Y2, CurSpeedType, false);
                        _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, isMove);
                    }

                    break;
                case "Backward":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        _mGoogol_Component.SetJogSpeed(En_AxisNum.Y1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                        _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, isMove);

                    }
                    else
                    {
                        _mGoogol_Component.GetCurPos();
                        float xLimitVal = 0;

                        if ((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] < 0)
                        {
                            //相机在吸嘴左边
                            xLimitVal = 180;
                        }
                        else
                        { //相机在吸嘴右边
                            xLimitVal = 145;
                        }
                        if (Math.Abs((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) < xLimitVal)
                        {//触发防撞                          
                            if ((250 + 140 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                            {
                                //NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.Info, $"Val1:{(350 - _mGoogol.CurPos[(byte)En_AxisNum.Y1]).ToString("f2")}---Val2:{_mGoogol.CurPos[(byte)En_AxisNum.Y2].ToString("f2")}",QA_Infrastructure.En_Logout_Type.SpotCheck,true);
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.Y2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, isMove);
                            }
                            else
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, false);
                                if (xLimitVal == 145)
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个轴系处于交叉状态，相机在吸嘴右边，检测到碰撞，停止Y2运动！", En_Logout_Type.Alarm, true);
                                }
                                else
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个轴系处于交叉状态，相机在吸嘴左边，检测到碰撞，停止Y2运动！", En_Logout_Type.Alarm, true);
                                }
                            }
                        }
                        else
                        {
                            //最大相对行程为471，但是坐标判断会之后，所以需要加上一点，防止过冲
                            if ((481 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                            {
                                //NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.Info, $"Val1:{(350 - _mGoogol.CurPos[(byte)En_AxisNum.Y1]).ToString("f2")}---Val2:{_mGoogol.CurPos[(byte)En_AxisNum.Y2].ToString("f2")}",QA_Infrastructure.En_Logout_Type.SpotCheck,true);
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.Y2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, isMove);
                            }
                            else
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, false);
                                if (xLimitVal == 145)
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个轴系处于离合状态，相机在吸嘴右边，检测到碰撞，停止Y2运动！", En_Logout_Type.Alarm, true);
                                }
                                else
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个轴系处于离合状态，相机在吸嘴左边，检测到碰撞，停止Y2运动！", En_Logout_Type.Alarm, true);
                                }

                            }
                        }
                    }

                    break;
                case "Left":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        //_mGoogol.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);                       
                        //_mGoogol.JogAxis(En_AxisNum.X1, 1, isMove);
                        _mGoogol_Component.GetCurPos();
                        //250 + 140
                        if ((_axisY1Y2SafeMaxDis - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] < _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) &&
                             (481 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                        {
                            if ((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] < 0)
                            {//相机在左边
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                            }
                            else
                            {//相机在右边
                                if (Math.Abs((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) > 145)
                                {
                                    _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                    _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                                }
                                else
                                {
                                    _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个轴系处于临近状态，相机在吸嘴右侧，X1正向运动检测到碰撞", En_Logout_Type.Alarm, true);
                                }
                            }
                        }
                        else
                        {
                            //Y1 Y2处于 前后离合状态，安全状态
                            if ((_mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] + _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) > 481)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个Y轴的距离太近，需要回退一点点！", En_Logout_Type.Alarm, true);
                                MessageBox.Warning("Jog->2个Y轴的距离太近，需要回退一点点！");
                            }
                            else
                            {
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                            }
                        }
                    }
                    else
                    {
                        //_mGoogol.SetJogSpeed(En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);                      
                        //_mGoogol.JogAxis(En_AxisNum.X2, 1, isMove);
                        //注意X2往+方向运动，需要做防撞处理
                        _mGoogol_Component.GetCurPos();
                        if ((_axisY1Y2SafeMaxDis - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] < _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) &&
                            (481 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                        {
                            if ((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] > 0)//相机在右边
                            {
                                //相机在吸嘴右边，Y2往左运动没有障碍
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType, false);
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                            }
                            else
                            {
                                ////相机在吸嘴左边，Y2往左运动有障碍，需要判断
                                if (Math.Abs((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) > 180)
                                {
                                    _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType, false);
                                    _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                                }
                                else
                                {
                                    _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个轴系处于临近状态，相机在吸嘴左边，X2负方向运动检测到碰撞", En_Logout_Type.Alarm, true);
                                }

                            }
                        }
                        else
                        {
                            if ((_mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] + _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) > 481)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个Y轴的距离太近，需要回退一点点！", En_Logout_Type.Alarm, true);
                                MessageBox.Warning("Jog->2个Y轴的距离太近，需要回退一点点！");

                            }
                            else
                            {
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                            }
                        }
                    }

                    break;
                case "Right":
                    if (_curWeldingPen == En_StationNo.StationNo1)
                    {
                        //_mGoogol.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                        //_mGoogol.JogAxis(En_AxisNum.X1, 1, isMove);
                        _mGoogol_Component.GetCurPos();
                        //运动左右，先判断前后安全距离，如果返现距离太近，需要提示先回一点，所以472 = 471 + 1 最多允许1mm过冲
                        if ((_axisY1Y2SafeMaxDis - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] < _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) &&
                             (481 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                        {
                            if ((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] > 0)
                            {
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                            }
                            else
                            {
                                if (Math.Abs((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) > 180)
                                {
                                    _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                                    _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                                }
                                else
                                    _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                            }
                        }
                        else
                        {
                            if ((_mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] + _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) > 481)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                                MessageBox.Show("2个Y轴的距离太近，需要回退一点点！");
                            }
                            else
                            {
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, isMove);
                            }
                        }
                    }
                    else
                    {
                        //注意X2往+方向运动，需要做防撞处理
                        _mGoogol_Component.GetCurPos();

                        if ((_axisY1Y2SafeMaxDis - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] < _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) &&
                            (481 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                        {
                            if ((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] > 0)
                            {//相机在吸嘴右边 如果轴范围减小，130要变大
                                if (Math.Abs((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) > 145)//13改 145
                                {
                                    _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                    _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                                }
                                else
                                {
                                    _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个轴系处于临近状态，相机在吸嘴右侧，X2正向运动检测到碰撞", En_Logout_Type.Alarm, true);
                                    //MessageBox.Warning("Jog->2个Y轴的距离太近，需要回退一点点！");
                                }
                            }
                            else
                            {
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                                //_mGoogol.JogAxis(En_AxisNum.X2, 1, false);
                            }
                        }
                        else
                        {
                            if ((_mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] + _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) > 481)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                                //MessageBox.Show("2个Y轴的距离太近，需要回退一点点！");
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Jog->2个Y轴的距离太近，需要回退一点点！", En_Logout_Type.Alarm, true);
                                MessageBox.Warning("Jog->2个Y轴的距离太近，需要回退一点点！");
                            }
                            else
                            {
                                _mGoogol_Component.SetJogSpeed(En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, isMove);
                            }
                        }
                    }

                    break;
                case "Down":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.Z2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                    _mGoogol_Component.JogAxis(En_AxisNum.Z2, 1, isMove);
                    break;
                case "Up":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.Z2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                    _mGoogol_Component.JogAxis(En_AxisNum.Z2, 1, isMove);
                    break;
                case "R1Pos":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                    _mGoogol_Component.JogAxis(En_AxisNum.R1, 1, isMove);
                    break;
                case "R1Neg":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                    _mGoogol_Component.JogAxis(En_AxisNum.R1, 1, isMove);
                    break;
                case "R2Pos":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                    _mGoogol_Component.JogAxis(En_AxisNum.R2, 1, isMove);
                    break;
                case "R2Neg":
                    _mGoogol_Component.SetJogSpeed(En_AxisNum.R2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                    _mGoogol_Component.JogAxis(En_AxisNum.R2, 1, isMove);
                    break;
                //case "R3Pos":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R3, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, true);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R3, 1, isMove);
                //    break;
                //case "R3Neg":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R3, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R3, 1, isMove);
                //    break;
                //case "R4Pos":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R4, CurSpeedType /*SpeedTypes[(byte)CurWeldingPen]*/, true);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R4, 1, isMove);
                //    break;
                //case "R4Neg":
                //    _mGoogol_Component.SetJogSpeed(En_AxisNum.R4, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/, false);
                //    _mGoogol_Component.JogAxis(En_AxisNum.R4, 1, isMove);
                //    break;
            }
        }

        private void DotMoveFunc(string dirmsg, bool isMove)
        {
            switch (dirmsg)
            {
                case "Forward":
                    if (isMove)
                    {
                        if (!_mGoogol_Component.MoveStroke(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.Y1 : En_AxisNum.Y2,
                             CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth : -1 * DotStepLenth))
                        {
                            return;
                        }
                        _mGoogol_Component.SetSpeed(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.Y1 : En_AxisNum.Y2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(
                            CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.Y1 : En_AxisNum.Y2,
                            CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth : -1 * DotStepLenth, true, false);

                    }
                    break;
                case "Backward":
                    if (isMove)
                    {
                        if (!_mGoogol_Component.MoveStroke(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.Y1 : En_AxisNum.Y2,
                             CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth * -1 : DotStepLenth))
                        {
                            return;
                        }
                        _mGoogol_Component.SetSpeed(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.Y1 : En_AxisNum.Y2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(
                            CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.Y1 : En_AxisNum.Y2,
                            CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth * -1 : DotStepLenth, true, false);

                    }
                    break;
                case "Left":
                    if (isMove)
                    {
                        if (!_mGoogol_Component.MoveStroke(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.X1 : En_AxisNum.X2,
                           CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth : DotStepLenth * -1))
                        {
                            return;
                        }
                        _mGoogol_Component.SetSpeed(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.X1 : En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(
                            CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.X1 : En_AxisNum.X2,
                            CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth : DotStepLenth * -1, true, false);

                    }
                    break;
                case "Right":
                    if (isMove)
                    {
                        if (!_mGoogol_Component.MoveStroke(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.X1 : En_AxisNum.X2,
                           CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth * -1 : DotStepLenth))
                        {
                            return;
                        }
                        _mGoogol_Component.SetSpeed(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.X1 : En_AxisNum.X2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(CurWeldingPen == En_StationNo.StationNo1 ? En_AxisNum.X1 : En_AxisNum.X2,
                            CurWeldingPen == En_StationNo.StationNo1 ? DotStepLenth * -1 : DotStepLenth, true, false);


                    }
                    break;
                case "Down":
                    if (isMove)
                    {
                        if (!_mGoogol_Component.MoveStroke(En_AxisNum.Z2,
                          DotStepLenth))
                        {
                            return;
                        }
                        _mGoogol_Component.SetSpeed(En_AxisNum.Z2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.Z2, DotStepLenth, true, true);

                    }
                    break;
                case "Up":
                    if (isMove)
                    {
                        _mGoogol_Component.SetSpeed(En_AxisNum.Z2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.Z2, DotStepLenth * -1, true, true);
                    }
                    break;
                case "R1Pos":
                    if (isMove)
                    {
                        _mGoogol_Component.SetSpeed(En_AxisNum.R1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R1, DotStepLenth, true, true);
                        //_mGoogol.MoveAbsoluteToPointSingleAxis(En_AxisNum.R1, DotStepLenth*-1, true, true);
                    }
                    break;
                case "R1Neg":
                    if (isMove)
                    {
                        _mGoogol_Component.SetSpeed(En_AxisNum.R1, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R1, DotStepLenth * -1, true, true);
                        //_mGoogol.MoveAbsoluteToPointSingleAxis(En_AxisNum.R1, DotStepLenth,true,true);
                    }
                    break;
                case "R2Pos":
                    if (isMove)
                    {
                        _mGoogol_Component.SetSpeed(En_AxisNum.R2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R2, DotStepLenth, true, true);
                    }
                    break;
                case "R2Neg":
                    if (isMove)
                    {
                        _mGoogol_Component.SetSpeed(En_AxisNum.R2, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R2, DotStepLenth * -1, true, true);
                    }
                    break;
                //case "R3Pos":
                //    if (isMove)
                //    {
                //        _mGoogol_Component.SetSpeed(En_AxisNum.R3, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                //        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R3, DotStepLenth, true, true);
                //    }
                //    break;
                //case "R3Neg":
                //    if (isMove)
                //    {
                //        _mGoogol_Component.SetSpeed(En_AxisNum.R3, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                //        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R3, DotStepLenth * -1, true, true);
                //    }
                //    break;
                //case "R4Pos":
                //    if (isMove)
                //    {
                //        _mGoogol_Component.SetSpeed(En_AxisNum.R4, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                //        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R4, DotStepLenth, true, true);
                //    }
                //    break;
                //case "R4Neg":
                //    if (isMove)
                //    {
                //        _mGoogol_Component.SetSpeed(En_AxisNum.R4, CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);
                //        _mGoogol_Component.MoveRelativeSingleAxis(En_AxisNum.R4, DotStepLenth * -1, true, true);
                //    }
                //    break;
            }
        }

        public void SpeedSelectionChanged()
        {
            try
            {

                _mGoogol_Component.SetSpeedAll(CurSpeedType/*SpeedTypes[(byte)CurWeldingPen]*/);

            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.Error, ex.ToString() + ex.StackTrace);
            }
        }

        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var delayTime = 200;
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        Position_Axias.Position_X1 = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                        Position_Axias.Position_Y1 = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
                        Position_Axias.Position_X2 = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                        Position_Axias.Position_Y2 = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                        Position_Axias.Position_Z2 = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                        Position_Axias.Position_R1 = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                        Position_Axias.Position_R2 = _mGoogol_Component.CurPos[(byte)En_AxisNum.R2];
                        //Position_Axias.Position_R3 = _mGoogol_Component.CurPos[(byte)En_AxisNum.R3];
                        //Position_Axias.Position_R4 = _mGoogol_Component.CurPos[(byte)En_AxisNum.R4];

                    }
                    catch (Exception e)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
                    }
                    await Task.Delay(delayTime, _cancellationToken);
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

        public bool StartCollisionCheck()
        {
            try
            {
                bool ret = false;
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;

                //bool lastStatus_Y1_NoCross = false;//Station1相机位置与Station2 轴系不接触
                //bool nowStatus_Y1_NoCross = false;

                bool lastStatus_Y2_NoCross = false;//Station1相机位置与Station2 轴系不接触
                bool nowStatus_Y2_NoCross = false;

                bool xLimit = false;

                bool lastStatus_Y2_Cross = false;//Station1相机位置与Station2 轴系 交叉接触
                bool nowStatus_Y2_Cross = false;

                bool lastStatus_X2_Pos = false;//Station1 X1位置与Station2 X2 之间保持125距离，正方向运动，超过会撞
                bool nowStatus_X2_Pos = false;

                bool lastStatus_X2_Neg = false;//Station1 X1位置与Station2 X2 之间保持180距离，负方向运动，超过会撞
                bool nowStatus_X2_Neg = false;

                bool lastStatus_X1_Pos = false;//Station1 X1位置与Station2 X2 之间保持125距离，正方向运动，超过会撞
                bool nowStatus_X1_Pos = false;

                bool lastStatus_X1_Neg = false;//Station1 X1位置与Station2 X2 之间保持180距离，负方向运动，超过会撞
                bool nowStatus_X1_Neg = false;

                Task.Factory.StartNew(async () =>
                {

                    while (true)
                    {
                        var delayTime = 10;
                        await Task.Delay(delayTime, _cancellationToken);
                        try
                        {
                            if (_cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                            _mGoogol_Component.GetCurPos();

                            float xLimitVal = 0;
                            if ((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] < 0)
                            {
                                //相机在吸嘴左边
                                xLimitVal = 180;
                            }
                            else
                            { //相机在吸嘴右边
                                xLimitVal = 145;
                            }
                            // //这个地方是否判断相机在吸嘴左右测，然后再判断限制范围
                            if (Math.Abs((_axisX1MaxStroke - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) < xLimitVal)
                            {
                                xLimit = true;
                            }
                            else
                                xLimit = false;

                            //360 = 250 + 140 
                            //2个Y处于交叉状态
                            if ((390 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] < _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) &&
                                (482 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                            {
                                if (Math.Abs((355 - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2]) < xLimitVal)
                                {
                                    if ((355 - _mGoogol_Component.CurPos[(byte)En_AxisNum.X1]) - _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] > 0)
                                    {
                                        //相机在吸嘴右边
                                        lastStatus_X2_Pos = true;
                                        lastStatus_X1_Pos = true;

                                        lastStatus_X2_Neg = false;
                                        lastStatus_X1_Neg = false;
                                    }
                                    else
                                    {
                                        lastStatus_X2_Neg = true;
                                        lastStatus_X1_Neg = true;
                                        //相机在吸嘴右边
                                        lastStatus_X2_Pos = false;
                                        lastStatus_X1_Pos = false;
                                    }
                                }
                                else
                                {
                                    lastStatus_X2_Neg = false;
                                    lastStatus_X1_Neg = false;

                                    lastStatus_X2_Pos = false;
                                    lastStatus_X1_Pos = false;
                                }

                                //if (Math.Abs((_axisX1MaxStroke - _mGoogol.CurPos[(byte)En_AxisNum.X1]) - _mGoogol.CurPos[(byte)En_AxisNum.X2]) < 180)
                                //{
                                //    ////相机在吸嘴左边
                                //    if ((_axisX1MaxStroke - _mGoogol.CurPos[(byte)En_AxisNum.X1]) - _mGoogol.CurPos[(byte)En_AxisNum.X2] < 0)
                                //    {
                                //        lastStatus_X2_Neg = true;
                                //        lastStatus_X1_Neg = true;
                                //    }
                                //    else
                                //    {
                                //        lastStatus_X2_Neg = false;
                                //        lastStatus_X1_Neg = false;
                                //    }

                                //}
                                //else
                                //{
                                //    lastStatus_X2_Neg = false;
                                //    lastStatus_X1_Neg = false;
                                //}
                            }
                            else
                            {
                                //2个轴理论处于非交叉状态
                                //如果坐标过冲，超过475怎么办？？？
                                if ((_mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] + _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) > 484)
                                {
                                    _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, false);
                                    _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, false);
                                }
                                lastStatus_X2_Pos = false;
                                lastStatus_X2_Neg = false;
                                lastStatus_X1_Neg = false;
                                lastStatus_X1_Pos = false;
                            }

                            if (nowStatus_X2_Pos == false && lastStatus_X2_Pos == true)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Run->Stop_X2_Pos", En_Logout_Type.Other, true);
                                //_mGoogol.JogAxis(En_AxisNum.X1, 1, false);
                            }
                            nowStatus_X2_Pos = lastStatus_X2_Pos;

                            if (nowStatus_X2_Neg == false && lastStatus_X2_Neg == true)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X2, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "Run->Stop_X2_Neg", En_Logout_Type.Other, true);
                                //_mGoogol.JogAxis(En_AxisNum.X1, 1, false);
                            }
                            nowStatus_X2_Neg = lastStatus_X2_Neg;

                            if (nowStatus_X1_Pos == false && lastStatus_X1_Pos == true)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, "Run->Stop_X1_Pos", En_Logout_Type.Other, true);
                            }
                            nowStatus_X1_Pos = lastStatus_X1_Pos;

                            if (nowStatus_X1_Neg == false && lastStatus_X1_Neg == true)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.X1, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, "Run->Stop_X1_Neg", En_Logout_Type.Other, true);
                            }
                            nowStatus_X1_Neg = lastStatus_X1_Neg;


                            if ((390 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] < _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]))
                            //||  _mGoogol.CurPos[(byte)En_AxisNum.Y2] >= 475)
                            {
                                lastStatus_Y2_NoCross = true;
                            }
                            else
                            {
                                lastStatus_Y2_NoCross = false;
                            }

                            if ((481 - _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] < _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2]) /*||*/
                             /*_mGoogol.CurPos[(byte)En_AxisNum.Y2] >= 471*/)
                            {
                                lastStatus_Y2_Cross = true;
                            }
                            else
                            {
                                lastStatus_Y2_Cross = false;
                            }

                            if (nowStatus_Y2_NoCross == false && lastStatus_Y2_NoCross == true && xLimit == true)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, "Run->Stop_Y2_1", En_Logout_Type.Other, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, false);
                            }
                            if (nowStatus_Y2_Cross == false && lastStatus_Y2_Cross == true && xLimit == false)
                            {
                                _mGoogol_Component.JogAxis(En_AxisNum.Y2, 1, false);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, "Run->Stop_Y2_2", En_Logout_Type.Other, true);
                                _mGoogol_Component.JogAxis(En_AxisNum.Y1, 1, false);
                            }

                            nowStatus_Y2_Cross = lastStatus_Y2_Cross;
                            nowStatus_Y2_NoCross = lastStatus_Y2_NoCross;
                        }
                        catch (Exception e)
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
                        }

                    }
                }, _cancellationToken);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message + ex.StackTrace, En_Logout_Type.Exception);
            }
            return true;

        }

        protected override void OnActivate()
        {
            Start();
            StartCollisionCheck();
            base.OnActivate();
        }
        protected override void OnDeactivate(bool close)
        {
            Stop();
            base.OnDeactivate(close);

        }
        /// <summary>
        /// 
        /// </summary>
        public async void RobotReset()
        {
            try
            {
                IsContainerEnabled = false;

                await Task.Run(() =>
                {
                    _mGoogol_Component.StopAllAxisMove();
                    _mGoogol_Component.CleanAlarm();

                    if (!_mGoogol_Component.WaitAxisMoveEnd())
                    {
                        MessageBox.Show("检测到轴系未停止");
                        return;
                    }
                    _eventAggregator.Publish(new PlcTrigInfo() { IsReset = true }, action => { Task.Run(action); });
                    _plc_Component.TrigPlcButton(EN_Plc_TrigButton.Reset);
                });
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.Error, ex.ToString() + ex.StackTrace);
            }
            finally
            {
                IsContainerEnabled = true;
            }
        }

        public void RobotRotateNo1()
        {

        }

        public void RobotRotateNo2()
        {

        }

        public void RobotRotateNo3()
        {

        }

        public void RobotRotateNo4()
        {

        }
    }
}
