using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.CacheParam;
using QA.Business.Component.Motion.Googol;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Steps;
using QA.SpotCheckPages.Models;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.ViewModels
{
    [Export("SpotCheckPressurePageViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckPressurePageViewModel : Screen, INotifyPropertyChanged, ISpotPageViewModel
    {
        //0V------>0
        //5V------>16383 (0x3FFF)
        //分辨率为：0.00030525

        #region Field
        private CacheParamManager _cacheParamManager;
        private RecipeManager _recipeManager;
        private MotionGoogol_Component _mGoogol_Component;
        private StepStatus _stepStatus;

        private Task _curTask = null;//用于判断当前是否正在执行耗时操作 

        public ManualPressModel Model { get; set; } = new ManualPressModel();
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "压力点检";

        private ushort _OrderID = 3;
        public ushort OrderID
        {
            get { return _OrderID; }
            set { _OrderID = value; }
        }

        private float _calibValue { get; set; }
        public float CalibValue
        {
            get => _calibValue;
            set
            {
                _calibValue = value;
                NotifyOfPropertyChange(() => CalibValue);
            }
        }

        private string _curPressPosVlaue { get; set; }
        public string CurPressPosVlaue
        {
            get => _curPressPosVlaue;
            set
            {
                _curPressPosVlaue = value;
                NotifyOfPropertyChange(() => CurPressPosVlaue);
            }
        }

        private string _stressTestStartPos { get; set; }
        public string StressTestStartPos
        {
            get => _stressTestStartPos;
            set
            {
                _stressTestStartPos = value;
                NotifyOfPropertyChange(() => StressTestStartPos);
            }
        }
        private string _stressTestEndPos { get; set; }
        public string StressTestEndPos
        {
            get => _stressTestEndPos;
            set
            {
                _stressTestEndPos = value;
                NotifyOfPropertyChange(() => StressTestEndPos);
            }
        }


        private string _setDestPressValue { get; set; }
        public string SetDestPressValue
        {
            get => _setDestPressValue;
            set
            {
                _setDestPressValue = value;
                NotifyOfPropertyChange(() => SetDestPressValue);
            }
        }

        private string _setDestPressMaxValue { get; set; }
        public string SetDestPressMaxValue
        {
            get => _setDestPressValue;
            set
            {
                _setDestPressMaxValue = value;
                NotifyOfPropertyChange(() => SetDestPressMaxValue);
            }
        }
        private string _setBufferPressDis { get; set; }
        public string SetBufferPressDis
        {
            get => _setBufferPressDis;
            set
            {
                _setBufferPressDis = value;
                NotifyOfPropertyChange(() => SetBufferPressDis);
            }
        }
        private string _setBufferPressSpeed { get; set; }
        public string SetBufferPressSpeed
        {
            get => _setBufferPressSpeed;
            set
            {
                _setBufferPressSpeed = value;
                NotifyOfPropertyChange(() => SetBufferPressSpeed);
            }
        }

        private string _setPosPressOffset { get; set; }
        public string SetPosPressOffset
        {
            get => _setPosPressOffset;
            set
            {
                _setPosPressOffset = value;
                NotifyOfPropertyChange(() => SetPosPressOffset);
            }
        }

        private string _setPressCompressTime { get; set; }
        public string SetPressCompressTime
        {
            get => _setPressCompressTime;
            set
            {
                _setPressCompressTime = value;
                NotifyOfPropertyChange(() => SetPressCompressTime);
            }
        }

        private int _stressPlaceDelay = 1500;
        public int StressPlaceDelay
        {
            get => _stressPlaceDelay;
            set
            {
                _stressPlaceDelay = value;
                NotifyOfPropertyChange(() => StressPlaceDelay);
            }
        }

        #endregion

        #region Constructor
        public SpotCheckPressurePageViewModel()
        {
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _recipeManager = IoC.Get<RecipeManager>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _stepStatus = IoC.Get<StepStatus>();

            Model.DoPressTargetPos = _cacheParamManager.PressParam.TargetHeight;
            Model.DoPressTargetPress = _cacheParamManager.PressParam.TargetPress;
            Model.DoPressBufferDis = _cacheParamManager.PressParam.BufferHeight;
            Model.DoPressSpeed = _cacheParamManager.PressParam.BufferSpeed;
            Model.DoPressPosRange = _cacheParamManager.PressParam.PositionRange;
            Model.DoPressLiftUp = _cacheParamManager.PressParam.LiftUp;
            Model.DoPressTimeout = _cacheParamManager.PressParam.PressTimeout;
            UpdateUI();
            CurPressPosVlaue = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.CalibPressSensorPos);
            StressTestStartPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.StressTestStartPos);
            StressTestEndPos = NLogTrace.GetFloatArrayString(_cacheParamManager.manualPositionParam.StressTestEndPos);

        }
        #endregion

        #region Method     

        //public void SelectItemChangedCommand(object p)
        //{
        //    ListView lv = p as ListView;
        //    Friend friend = lv.SelectedItem as Friend;
        //    Head = friend.Head;
        //    Nickname = friend.Nickname;
        //}

        //public void NumUDDestPressChanged(object p)
        //{
        //    NumericUpDown numUd = p as NumericUpDown;
        //    _cacheParamManager.manualPositionParam.SetDestPressValue = (float)numUd.Value;
        //    _cacheParamManager.SaveManualPositionParam();

        //}

        //public void NumUDDestMaxPressChanged(object p)
        //{
        //    NumericUpDown numUd = p as NumericUpDown;
        //    _cacheParamManager.manualPositionParam.SetDestPressMaxValue = (float)numUd.Value;
        //    _cacheParamManager.SaveManualPositionParam();
        //}

        //public void NumUDBufferPressDisChanged(object p)
        //{
        //    NumericUpDown numUd = p as NumericUpDown;
        //    _cacheParamManager.manualPositionParam.SetBufferPressDis = (float)numUd.Value;
        //    _cacheParamManager.SaveManualPositionParam();
        //}
        //public void NumUDBufferPressSpeedChanged(object p)
        //{
        //    NumericUpDown numUd = p as NumericUpDown;
        //    _cacheParamManager.manualPositionParam.SetBufferPressSpeed = (float)numUd.Value;
        //    _cacheParamManager.SaveManualPositionParam();
        //}
        //public void NumUDPressPosOffsetChanged(object p)
        //{
        //    NumericUpDown numUd = p as NumericUpDown;
        //    _cacheParamManager.manualPositionParam.SetPressPosOffset = (float)numUd.Value;
        //    _cacheParamManager.SaveManualPositionParam();
        //}
        //public void NumUDPressCompressTimeChanged(object p)
        //{
        //    NumericUpDown numUd = p as NumericUpDown;
        //    _cacheParamManager.manualPositionParam.SetPressCompressTime = (float)numUd.Value;
        //    _cacheParamManager.SaveManualPositionParam();
        //}

        //public void LowerPressCalib()
        //{
        //    //记录读取压力，和设置压力机压力
        //    _cacheParamManager.calibParam.LowerCalibPressValue = CalibValue;

        //    _cacheParamManager.SaveCalibParam();
        //}

        //public void HigherPressCalib()
        //{
        //    _cacheParamManager.calibParam.HigherCalibPressValue = CalibValue;

        //    _cacheParamManager.SaveCalibParam();
        //}

        public void UpdateUI()
        {
            int nozzleIdx = Model.SltNozzleIdx;
            short pressGet = _mGoogol_Component.AInput[nozzleIdx];
            Model.CurPress = _cacheParamManager.PressParam.SglParam[nozzleIdx].GetCalibedPress(pressGet).ToString("F2");
        }

        public void GetCurPress()
        {
            UpdateUI();
        }

        /// <summary>
        /// 执行低压校准
        /// </summary>
        public void DoLowPressCalib()
        {
            MessageBoxResult rs = MessageBox.Ask("确认是否执行低压校准", "ASK");
            if (rs == MessageBoxResult.Yes || rs == MessageBoxResult.OK)
            {
                int nozzleIdx = Model.SltNozzleIdx;
                short lowA = _mGoogol_Component.AInput[nozzleIdx];
                float lowD = Model.LowPressCalibValue;
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"执行低压校准 nozzleIdx:{nozzleIdx} lowA:{lowA} lowD:{lowD}", En_Logout_Type.SpotCheck);
                if (!_cacheParamManager.PressParam.SglParam[nozzleIdx].DoLowCalib(lowA, lowD))
                {
                    MessageBox.Error("低压校准失败", "ERROR");
                    return;
                }
                _cacheParamManager.SaveSpecifiedParam(typeof(PressParam));
            }
        }

        /// <summary>
        /// 执行高压校准
        /// </summary>
        public void DoHighPressCalib()
        {
            MessageBoxResult rs = MessageBox.Ask("确认是否执行高压校准", "ASK");
            if (rs == MessageBoxResult.Yes || rs == MessageBoxResult.OK)
            {
                int nozzleIdx = Model.SltNozzleIdx;
                short highA = _mGoogol_Component.AInput[nozzleIdx];
                float highD = Model.HighPressCalibValue;
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"执行高压校准 nozzleIdx:{nozzleIdx} highA:{highA} highD:{highD}", En_Logout_Type.SpotCheck);
                if (!_cacheParamManager.PressParam.SglParam[nozzleIdx].DoHighCalib(highA, highD))
                {
                    MessageBox.Error("高压校准失败", "ERROR");
                    return;
                }
                _cacheParamManager.SaveSpecifiedParam(typeof(PressParam));
            }
        }

        /// <summary>
        /// 执行一次空压
        /// </summary>
        public async void DoEmptyPress()
        {
            MessageBoxResult rs = MessageBox.Ask("确认是否执行空压", "ASK");
            if (rs == MessageBoxResult.Yes || rs == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Show("轴系在运动，等静止再操作！");
                        return;
                    }
                    int nozzleIdx = Model.SltNozzleIdx;
                    if (_curTask != null && !_curTask.IsCompleted)
                    {
                        MessageBox.Warning("当前还有未完成的任务，请稍后重试", "WARNING");
                        return;
                    }
                    _curTask = new TaskFactory().StartNew(() =>
                    {
                        En_AxisNum axis = Model.Heads[Model.SltNozzleIdx].Axis;

                        //NLogTrace.Info(LogType.Manual, $"执行一次空压 head:{headidx} axis:{axis}");
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"执行一次空压 nozzleIdx:{nozzleIdx} axis:{axis}");
                        _mGoogol_Component.SetMoveSpeed(axis, Model.DoPressSpeed);
                        _mGoogol_Component.MoveAbsoluteSingleAxis(axis, Model.DoPressTargetPos, false);

                        DateTime startTime = DateTime.Now;
                        bool isPressOk = false;
                        while (true)
                        {
                            if ((DateTime.Now - startTime).TotalMilliseconds >= Model.DoPressTimeout)
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"[Spot Press Check]执行一次空压 到达超时时间 {Model.DoPressTimeout}");
                                break;
                            }
                            if (!isPressOk && !_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"[Spot Press Check]执行一次空压 点位运动结束 {axis}");
                                return;
                            }
                            short pressA = _mGoogol_Component.AInput[nozzleIdx];
                            float pressD = _cacheParamManager.PressParam.SglParam[nozzleIdx].GetCalibedPress(pressA);
                            Console.WriteLine($"{pressA},   {pressD}");
                            if (pressD >= Model.DoPressTargetPress * Model.BeforeRate)   //提前量
                            {
                                if (!isPressOk)
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"[Spot Press Check]执行一次空压 到达目标压力 {pressD}");
                                    isPressOk = true;
                                    _mGoogol_Component.StopAxisMove(axis);
                                }
                            }
                            Thread.Sleep(10);
                        }

                        //if (!_mGoogol_Component.GetCurPos() || _mGoogol_Component.Exit())
                        //{
                        //    if (_mGoogol_Component.Exit()) return;
                        //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取2站当前位置失败", En_Logout_Type.Alarm, true);
                        //    return;
                        //}
                        //float curPos = _mGoogol_Component.CurPos[axis];

                        float curPos = _mGoogol_Component.GetAxisCurPos(axis);
                        float liftUpPos = curPos - Model.DoPressLiftUp > 0 ? curPos - Model.DoPressLiftUp : 0;
                        if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid))
                            return;
                        if (!_mGoogol_Component.MoveRelativeSingleAxis(axis, liftUpPos, true, true))
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"[Spot Press Check]执行一次空压 上抬错误");
                            return;
                        }
                    });
                });
            }
        }

        public void SetPressPos()
        {
            MessageBoxResult mbr = MessageBox.Show("确定设置压力标定坐标吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (mbr == MessageBoxResult.OK)
            {
                _cacheParamManager.manualPositionParam.CalibPressSensorPos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                _cacheParamManager.manualPositionParam.CalibPressSensorPos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                _cacheParamManager.manualPositionParam.CalibPressSensorPos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                _cacheParamManager.manualPositionParam.CalibPressSensorPos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                _cacheParamManager.SaveManualPositionParam();

                var pos = _cacheParamManager.manualPositionParam.CalibPressSensorPos;
                CurPressPosVlaue = NLogTrace.GetFloatArrayString(pos);
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"SetPressPos() 坐标为：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
                return;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"SetPressPos() 弹窗取消操作", En_Logout_Type.SpotCheck);
        }

        public async void MoveSpacePressPosXY()
        {
            MessageBoxResult mbr = MessageBox.Show("确定移动压力标定坐标XY吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (mbr == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Show("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid)) return;
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2(_cacheParamManager.manualPositionParam.CalibPressSensorPos, true, true)) return;

                    var pos = _cacheParamManager.manualPositionParam.CalibPressSensorPos;
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"MoveSpacePressPosXY() 坐标为：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
                    MessageBox.Show("移动压力标定坐标XY成功！");
                    return;
                });
            }
            else
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"MoveSpacePressPosXY() 弹窗取消操作", En_Logout_Type.SpotCheck);
        }

        public async void MoveSpacePressPosZ()
        {
            MessageBoxResult mbr = MessageBox.Show("确定移动压力标定坐标Z吗?", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (mbr == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Show("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid)) return;
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2(_cacheParamManager.manualPositionParam.CalibPressSensorPos, true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, _cacheParamManager.manualPositionParam.CalibPressSensorPos[2], true, true)) return;

                    var pos = _cacheParamManager.manualPositionParam.CalibPressSensorPos;
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"MoveSpacePressPosXY() 坐标为：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
                    MessageBox.Show("移动压力标定坐标Z 成功！");
                    return;
                });
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"MoveSpacePressPosXY() 弹窗取消操作", En_Logout_Type.SpotCheck);
        }

        public void SaveEmptyPressParam()
        {
            MessageBoxResult rs = MessageBox.Ask("确认保存空压参数", "ASK");
            if (rs == MessageBoxResult.Yes || rs == MessageBoxResult.OK)
            {
                _cacheParamManager.PressParam.TargetHeight = Model.DoPressTargetPos;
                _cacheParamManager.PressParam.TargetPress = Model.DoPressTargetPress;
                _cacheParamManager.PressParam.BufferHeight = Model.DoPressBufferDis;
                _cacheParamManager.PressParam.BufferSpeed = Model.DoPressSpeed;
                _cacheParamManager.PressParam.PositionRange = Model.DoPressPosRange;
                _cacheParamManager.PressParam.LiftUp = Model.DoPressLiftUp;
                _cacheParamManager.PressParam.PressTimeout = Model.DoPressTimeout;
                _cacheParamManager.SaveSpecifiedParam(typeof(PressParam));
            }
        }

        /// <summary>
        /// 停止空压
        /// </summary>
        public void StopEmptyPress()
        {
            int nozzleIdx = Model.SltNozzleIdx;
            En_AxisNum axis = Model.Heads[Model.SltNozzleIdx].Axis;
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"停止空压 nozzleIdx:{nozzleIdx} axis:{axis}");

            _mGoogol_Component.StopAxisMove(axis);
        }

        public async void TestStress()
        {
            if (MessageBox.Show("确定开始应力测试吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            await Task.Run(() =>
            {
                //false说明有轴在运动
                if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                {
                    MessageBox.Warning("轴系在运动，等静止再操作！");
                    return;
                }
                if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid)) return;
                //获取当前位置
                var posXYZ = _mGoogol_Component.GetCurPos();
                if (!_mGoogol_Component.GetCurPos())
                {
                    MessageBox.Warning("获取工站2当前点位信息失败");
                    return;
                }
                float[] posStart = GetPressPos(true);
                float[] posEnd = GetPressPos(false);
                if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true)) return;
                if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { posStart[0], posStart[1] }, true, true)) return;
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posStart[2], true, true)) return;
                int waitTime = 1000;
                Thread.Sleep(waitTime);
                // 开始测试
                for (int i = 0; i < 3; i++)
                {
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(new float[] { posEnd[0], posEnd[1], posEnd[2] }, true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(new float[] { posStart[0], posStart[1], posStart[2] }, true, true)) return;
                    Thread.Sleep(waitTime);
                }
                ////所有气缸收回，z2回0，y2回安全Y
                //if (!_mGoogol_Component.SetAllCylindersUpDown(true)) return;
                //if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true)) return;
                //var safeY = (_mGoogol_Component.Param as MotionGoogolParam).SafeY2;
                //if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Y2, safeY, true)) return;
                ////x1y1回0,0
                //if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[] { 0, 0 }, true)) return;
                ////使用当前制程的1 10 3 12穴，注意避让
                //var task = _stepStatus.CurrentProcedure;
                //int[] cavityArr = { 1, 10, 12, 3 };
                //bool breakThread = false;
                //foreach (var cavity in cavityArr)
                //{
                //    for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                //    {
                //        var point = task.GetVisionPointByCavityNum(cavity).GetSolderPointByNozzleNo(nozzleNo);
                //        var recipe = _recipeManager.GetRecipe(point.SolderRecipeName);
                //        var posXYZ = point.GetPos();
                //        if (!_mGoogol_Component.SafeAvoid()) return;
                //        float posR = _mGoogol_Component.CurPos[nozzleNo + 4];
                //    //压三次
                //    DoPress:
                //        for (int i = 0; i < 3; i++)
                //        {
                //            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(new[] { posXYZ[0], posXYZ[1], posXYZ[2] - recipe.Place1stHeight }, true)) return;
                //            if (!_mGoogol_Component.SetCylinderUpDown(point.NozzleNo, false)) return;
                //            if (!_mGoogol_Component.SetSpeed(En_AxisNum.Z2, recipe.Place1stHeightSetSpeed)) return;
                //            float realPress = 0;
                //            if (recipe.PosMode)
                //            {
                //                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posXYZ[2], false)) return;
                //                //找压力模式，需要实时读取压力
                //                DateTime startTime = DateTime.Now;
                //                //目标压力
                //                float targetPress = recipe.Press;
                //                //压力提前量，因为发送轴停止指令需要一段时间
                //                float beforePress = recipe.BeforePress;
                //                //只要到这个压力就立刻发送轴停止指令
                //                float stopMovePress = targetPress - beforePress;
                //                short pressGet;
                //                while (true)
                //                {
                //                    if (_mGoogol_Component.Exit()) return;
                //                    //获取当前压力
                //                    pressGet = _mGoogol_Component.AInput[nozzleNo - 1];
                //                    realPress = _cacheParamManager.PressParam.SglParam[nozzleNo - 1].GetCalibedPress(pressGet);
                //                    //压力达标则停止轴运动，注意发送停止指令后轴还会往下走一段（通信需要时间）
                //                    if (realPress >= stopMovePress)
                //                    {
                //                        _mGoogol_Component.StopAxisMove(En_AxisNum.Z2);
                //                        break;
                //                    }
                //                    //如果规定时间内未找到压力，跳出
                //                    if ((DateTime.Now - startTime).TotalMilliseconds >= recipe.FindPressTimeout)
                //                    {
                //                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"找压力到达超时时间 {recipe.FindPressTimeout} ms", En_Logout_Type.Alarm, true);
                //                        break;
                //                    }
                //                    //如果移动到目标位置还未找到压力，跳出
                //                    //_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled)为true表示没有轴在运动
                //                    if (_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                //                    {
                //                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"未能找到指定压力", En_Logout_Type.Alarm, true);
                //                        break;
                //                    }
                //                }
                //                //贴合保压
                //                Thread.Sleep(StressPlaceDelay);
                //                //等待一段时间后读到的压力才是稳定的压力，下面再判断压力够不够
                //                pressGet = _mGoogol_Component.AInput[nozzleNo - 1];
                //                realPress = _cacheParamManager.PressParam.SglParam[nozzleNo - 1].GetCalibedPress(pressGet);
                //            }
                //            else
                //            {
                //                //直接下降到固定高度
                //                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, posXYZ[2], true)) return;
                //                //贴合保压
                //                Thread.Sleep(StressPlaceDelay);
                //            }
                //            if (!_mGoogol_Component.SetSpeed(En_AxisNum.Z2, En_SpeedType.Mid)) return;
                //            //关吸开吹
                //            if (!_mGoogol_Component.SetVacuum(nozzleNo, false)) return;
                //            //等待吹一段时间
                //            Thread.Sleep(recipe.BreakDelay);
                //            //上抬5mm
                //            if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(new[] { posXYZ[0], posXYZ[1], posXYZ[2] - recipe.Place1stHeight-5 }, true)) return;
                //            //关闭吹
                //            if (!_mGoogol_Component.SetVacuum(nozzleNo, false, false)) return;
                //        }
                //        if (!_mGoogol_Component.SetAllCylindersUpDown(true)) return;
                //        var result = MessageBox.Show("是否继续？\n是表示进行下一个\n否表示重压\n取消表示结束", "提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                //        if (result == MessageBoxResult.No)
                //        {
                //            goto DoPress;
                //        }
                //        else if (result == MessageBoxResult.Cancel)
                //        {
                //            breakThread = true;
                //            break;
                //        }
                //    }
                //    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, false)) return;
                //    if (breakThread)
                //    {
                //        break;
                //    }
                //}
                //if (!_mGoogol_Component.SetAllCylindersUpDown(true)) return;
                //if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true)) return;
                //if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Y2, safeY, true)) return;
                MessageBox.Success("应力测试完毕！");
            });
        }

        private float[] GetPressPos(bool isStart)
        {
            if (isStart) return _cacheParamManager.manualPositionParam.StressTestStartPos;
            return _cacheParamManager.manualPositionParam.StressTestEndPos;
        }

        private void SetPressTestPos(bool isStart)
        {
            if (MessageBox.Show("确定设置坐标吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                float[] pos = GetPressPos(isStart);
                pos[0] = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                pos[1] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
                pos[2] = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                pos[3] = _mGoogol_Component.CurPos[(byte)En_AxisNum.R1];
                SetPressPosStr(isStart, pos);
                _cacheParamManager.SaveAllParam();
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"设定坐标为：{NLogTrace.GetFloatArrayString(pos)}", En_Logout_Type.SpotCheck);
            }
        }

        private async void MovePressTestPos(bool isStart = true)
        {
            if (MessageBox.Show("确定空移吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    float[] pos = GetPressPos(isStart);
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid)) return;
                    _mGoogol_Component.SetAllCylindersUpDown(true);
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2(new float[2] { pos[0], pos[1] }, true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, true)) return;
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.R1, pos[3], true, true)) return;
                    MessageBox.Success("移动完毕！");
                });
            }
        }

        private void SetPressPosStr(bool isStart, float[] pos)
        {
            if (isStart)
            {
                StressTestStartPos = NLogTrace.GetFloatArrayString(pos);
            }
            else
            {
                StressTestEndPos = NLogTrace.GetFloatArrayString(pos);
            }
        }

        public void SetPressTestStartPos()
        {
            SetPressTestPos(true);
        }
        public void SetPressTestEndPos()
        {
            SetPressTestPos(false);
        }
        public void MovePressTestStartPos()
        {
            MovePressTestPos(true);
        }
        public void MovePressTestEndPos()
        {
            MovePressTestPos(false);
        }

        #endregion
    }
}
