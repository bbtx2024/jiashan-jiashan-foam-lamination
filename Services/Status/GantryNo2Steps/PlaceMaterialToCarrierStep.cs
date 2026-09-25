using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
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
using QA.Business.Procedure;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Business.Steps
{
    //吸嘴上面产品贴合到载具上的产品
    public class PlaceTargetInfo
    {
        public LaserSprayVisionPoint VisionPoint;
        public LaserSpraySolderPoint SolderPoint;
        public int CavityNum;
        public int NozzleNo;
        public float[] BasePos;//SolderPoint中的数据，X,Y,Z,R
        public float[] Pos;//视觉软件计算的数据，X,Y,R
        public bool CanPlace;
    }
    public class PlaceMaterialToCarrierStep : IStepStation2
    {
        #region Field
        private StepStatus _stepStatus;
        private RecipeManager _recipeManager;
        private MotionGoogol_Component _mGoogol_Component;
        private Camera_Component _camera_Component;
        private PLC_Component _plc_Component;
        private CacheParamManager _cacheParamManager;
        private GlobalVariable _globalVariable;
        private MES_Component _mes_Component;
        private Hive_Component _hive_Component;
        private IEventAggregator _eventAggregator;
        private CameraParam _cameraParam = null;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.PlaceMaterialToCarrierStep;
        private HomeUiParam_Enable Enable { get => _stepStatus.CacheParamManager.HomeUiParam.Enable; }
        #endregion

        #region Constructor
        public PlaceMaterialToCarrierStep()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _recipeManager = IoC.Get<RecipeManager>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _mes_Component = (MES_Component)IoC.Get<IMES>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _cameraParam = IoC.Get<CameraParam>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
            if (Enable.PickOpportunity == EN_PickOpportunity.AfterStart)
            {
                //提前送料
                FeederCDD(false);
            }
            _stepStatus.isEndCarrierFinish = false;
            _stepStatus.ManualResetEvt_CtrlPlace.WaitOne();

            if (_mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
            }

            if (!_mGoogol_Component.GetCurPos(En_StationNo.StationNo1) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
            {
                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->获取当前坐标失败(Lỗi lấy tọa độ hiện tại)", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
            }

            //拍照是否完成
            //解锁运动轴
            if (_stepStatus.GetMoveMutex() == false && _plc_Component.IsCarrierReady())
            {
                bool isFirstPlace = true;
                CarrierStatus carrierStatus = _stepStatus.GetCurCarrier();
                if (carrierStatus == null)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->未能获取载具信息(Không lấy được thông tin tàu sân bay)", En_Logout_Type.Alarm, true);
                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                }

                //指示贴合信息，最终对应关系都写入placeTargetInfo
                List<PlaceTargetInfo> placeTargetInfo = new List<PlaceTargetInfo>();
                //_stepStatus.CurNeedProcess 存储上视觉ok的点位
                //_stepStatus.DownCamOk[nozzleIdx] 表示下视觉是否ok
                if (Enable.PickOpportunity == EN_PickOpportunity.AfterUpCam || _stepStatus.GetEnableNozzleCount() < 2)
                {
                    //如果是随机贴
                    //指示当前在处理哪个吸嘴
                    int currProcessNozzleIdx = 0;
                    //下视觉个数一定等于吸嘴启用个数，上视觉个数不定，所以应该用上视觉循环
                    //_stepStatus.CurNeedProcess里面的point一定是上视觉ok的
                    foreach (var visionPoint in _stepStatus.CurNeedProcess)
                    {
                        //超过4说明所有吸嘴都已匹配过
                        if (currProcessNozzleIdx >= 2)
                        {
                            break;
                        }
                        //找到未匹配的第一个可用吸嘴
                        while (currProcessNozzleIdx < 2)
                        {
                            //如果下视觉ok（下视觉ok说明吸嘴已启用）
                            if (_stepStatus.DownCamOk[currProcessNozzleIdx])
                            {
                                //匹配成功，已经找到可用的吸嘴，索引为nozzleIdx
                                //获取贴合点
                                var solderPoint = visionPoint.GetSolderPointByNozzleNo(currProcessNozzleIdx + 1);
                                if (solderPoint == null)
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->制程中{visionPoint.CavityNum}穴没有吸嘴{currProcessNozzleIdx + 1}的贴合点(Thiếu tọa độ khớp miệng hút trong quá trình sản xuất)", En_Logout_Type.Alarm, true);
                                    return (false, EN_RunRet.TaskErr, EN_RunStep.Err);
                                }
                                bool canPlace;
                                float[] pos = new float[3];
                                if (_plc_Component.IsSimulateRun())
                                {
                                    //空跑模式直接使用制程中默认点位，但是默认点位只有xy；使用下视觉R作为点位的R
                                    float posR = _stepStatus.DownCameraPos(currProcessNozzleIdx + 1)[3];
                                    pos = new float[3] { solderPoint.SolderPointX, solderPoint.SolderPointY, posR };
                                    canPlace = true;
                                }
                                else
                                {
                                    if (!_camera_Component.GetFitPos(currProcessNozzleIdx + 1, visionPoint.CavityNum, out pos) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                                    {
                                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->计算最终贴合坐标失败，穴位号：{visionPoint.CavityNum}(Tính toán tọa độ phù hợp cuối cùng thất bại)", En_Logout_Type.Alarm, true);
                                        canPlace = false;
                                    }
                                    else
                                    {
                                        //添加xy补偿值
                                        pos[0] += visionPoint.VisionPointXOffset + solderPoint.SolderPointXOffset;
                                        pos[1] += visionPoint.VisionPointYOffset + solderPoint.SolderPointYOffset;
                                        NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"Place->计算最终贴合坐标成功XYR:[{pos[0].ToString("f3")},{pos[1].ToString("f3")},{pos[2].ToString("f3")}]，吸嘴编号：{(currProcessNozzleIdx + 1)},穴位号：{visionPoint.CavityNum}", En_Logout_Type.Run, true);
                                        canPlace = true;
                                        //将下视觉图像保存到ok里面
                                        carrierStatus.tapeBaseDistance[visionPoint.CavityNum - 1] = _stepStatus.tapeBaseDistance[currProcessNozzleIdx];
                                    }
                                }
                                placeTargetInfo.Add(new PlaceTargetInfo()
                                {
                                    VisionPoint = visionPoint,
                                    SolderPoint = solderPoint,
                                    CavityNum = visionPoint.CavityNum,
                                    NozzleNo = currProcessNozzleIdx + 1,
                                    BasePos = solderPoint.GetPos(),
                                    Pos = new float[3] { pos[0], pos[1], pos[2] },
                                    CanPlace = canPlace,
                                });
                                //匹配成功之后
                                currProcessNozzleIdx++;
                                break;
                            }
                            //匹配失败
                            currProcessNozzleIdx++;
                        }
                    }
                }
                else
                {
                    //如果是固定贴
                    //固定贴不看下视觉，只看吸嘴是否启用。如果出现某个启用的吸嘴下视觉不是ok，应该重新取料，不能进入这个step
                    //防呆，确保四个吸嘴都下视觉ok（上面已经加了 _stepStatus.GetEnableNozzleCount() != 4 的判断，所以此处必定为四个吸嘴都要取料）
                    for (int i = 0; i < 2; i++)
                    {
                        HashSet<int> throwTapeNozzleNoSet0 = new HashSet<int>();
                        if (_stepStatus.UseNozzle(i + 1) && !_stepStatus.DownCamOk[i])
                        {
                            throwTapeNozzleNoSet0.Add(i + 1);
                        }
                        if (throwTapeNozzleNoSet0.Count > 0)
                        {
                            if (!_stepStatus.ThrowTapes(throwTapeNozzleNoSet0, true, false, EN_TossingCode.MaterialDeflect, carrierStatus, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->抛掉下视觉NG物料失败(Thả vật liệu NG thị giác xuống thất bại)", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.TaskErr, EN_RunStep.Err);
                            }
                            return (false, EN_RunRet.TaskErr, EN_RunStep.PickMaterialFromFeederStep);
                        }
                    }
                    foreach (var visionPoint in _stepStatus.CurNeedProcess)
                    {
                        int currProcessNozzleIdx;
                        if ((visionPoint.CavityNum >= 1 && visionPoint.CavityNum <= 3) || (visionPoint.CavityNum >= 7 && visionPoint.CavityNum <= 9))
                        {
                            currProcessNozzleIdx = 0;
                        }
                        else
                        {
                            currProcessNozzleIdx = 1;
                        }
                        //获取贴合点
                        var solderPoint = visionPoint.GetSolderPointByNozzleNo(currProcessNozzleIdx + 1);
                        if (solderPoint == null)
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->制程中{visionPoint.CavityNum}穴没有吸嘴{currProcessNozzleIdx + 1}的贴合点(Thiếu tọa độ khớp miệng hút trong quá trình sản xuất)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.TaskErr, EN_RunStep.Err);
                        }
                        bool canPlace;
                        float[] pos = new float[3];
                        if (_plc_Component.IsSimulateRun())
                        {
                            //空跑模式直接使用制程中默认点位，但是默认点位只有xy；使用下视觉R作为点位的R
                            float posR = _stepStatus.DownCameraPos(currProcessNozzleIdx + 1)[3];
                            pos = new float[3] { solderPoint.SolderPointX, solderPoint.SolderPointY, posR };
                            canPlace = true;
                        }
                        else
                        {
                            if (!_camera_Component.GetFitPos(currProcessNozzleIdx + 1, visionPoint.CavityNum, out pos) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->计算最终贴合坐标失败，穴位号：{visionPoint.CavityNum}(Tính toán tọa độ phù hợp cuối cùng thất bại)", En_Logout_Type.Alarm, true);
                                canPlace = false;
                            }
                            else
                            {
                                //添加xy补偿值
                                pos[0] += visionPoint.VisionPointXOffset + solderPoint.SolderPointXOffset;
                                pos[1] += visionPoint.VisionPointYOffset + solderPoint.SolderPointYOffset;
                                NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"Place->计算最终贴合坐标成功XYR:[{pos[0].ToString("f3")},{pos[1].ToString("f3")},{pos[2].ToString("f3")}]，吸嘴编号：{(currProcessNozzleIdx + 1)},穴位号：{visionPoint.CavityNum}", En_Logout_Type.Run, true);
                                canPlace = true;
                                //将下视觉图像保存到ok里面
                                carrierStatus.tapeBaseDistance[visionPoint.CavityNum - 1] = _stepStatus.tapeBaseDistance[currProcessNozzleIdx];
                            }

                        }
                        placeTargetInfo.Add(new PlaceTargetInfo()
                        {
                            VisionPoint = visionPoint,
                            SolderPoint = solderPoint,
                            CavityNum = visionPoint.CavityNum,
                            NozzleNo = currProcessNozzleIdx + 1,
                            BasePos = solderPoint.GetPos(),
                            Pos = new float[3] { pos[0], pos[1], pos[2] },
                            CanPlace = canPlace,
                        });
                    }
                }
                for (int i = 0; i < placeTargetInfo.Count; i++)
                {
                    //检测是否可以贴合
                    if (placeTargetInfo[i].CanPlace)
                    {
                        int cavityNo = placeTargetInfo[i].CavityNum;
                        int nozzleNo = placeTargetInfo[i].NozzleNo;
                        int index = _stepStatus.GetIndexFromCurProcedure(placeTargetInfo[i].CavityNum);
                        var recipe = _recipeManager.GetRecipe(placeTargetInfo[i].SolderPoint.SolderRecipeName);
                        if (recipe == null || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->获取贴合配方失败：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[X2,Y2]:{placeTargetInfo[i].Pos[0].ToString("f2")},{placeTargetInfo[i].Pos[1].ToString("f2")}(Nhận công thức phù hợp thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }

                        #region 相机轴系左右避让
                        var targetX = placeTargetInfo[i].Pos[0];
                        if (!_mGoogol_Component.SafeAvoid())
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->触发防撞系统，运动失败(Kích hoạt hệ thống chống va chạm, chuyển động thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        #endregion

                        if (Math.Abs(placeTargetInfo[i].Pos[0] - placeTargetInfo[i].BasePos[0]) >= 10 ||
                            Math.Abs(placeTargetInfo[i].Pos[1] - placeTargetInfo[i].BasePos[1]) >= 10)
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->基准坐标与实际坐标超过10mm，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，ActAxis[X2,Y2]:{placeTargetInfo[i].Pos[0].ToString("f2")},{placeTargetInfo[i].Pos[1].ToString("f2")}，BaseAxis[X2,Y2]:{placeTargetInfo[i].BasePos[0].ToString("f2")},{placeTargetInfo[i].BasePos[1].ToString("f2")}(Tọa độ cơ sở và tọa độ thực tế hơn 10 mm)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //获取当前R轴坐标
                        if (!_mGoogol_Component.GetCurPos(En_StationNo.StationNo2) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->获取2站当前位置失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[X2,Y2]:{placeTargetInfo[i].Pos[0].ToString("f2")},{placeTargetInfo[i].Pos[1].ToString("f2")}(Nhận vị trí hiện tại của 2 trạm không thành công)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        float posR = _mGoogol_Component.CurPos[nozzleNo + 4];
                        //float posZ = _mGoogol_Component.CurPos[(int)En_AxisNum.Z2];
                        //如果当前R和目标R相差过大，修改旋转角度
                        while (Math.Abs(posR - placeTargetInfo[i].Pos[2]) > 180)
                        {
                            if (posR > placeTargetInfo[i].Pos[2])
                            {
                                placeTargetInfo[i].Pos[2] += 360;
                            }
                            else
                            {
                                placeTargetInfo[i].Pos[2] -= 360;
                            }
                        }
                        //根据一次贴合高度计算目标Z，由于实际高度关系Z不会撞
                        float posZ = placeTargetInfo[i].BasePos[2] - recipe.Place1stHeight;
                        //运动XYZR
                        if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R((En_AxisNum)(nozzleNo + 4), new float[4] { placeTargetInfo[i].Pos[0], placeTargetInfo[i].Pos[1], posZ, placeTargetInfo[i].Pos[2] }, true, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->运动到贴合点XYR位置失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[X2,Y2]:{placeTargetInfo[i].Pos[0].ToString("f2")},{placeTargetInfo[i].Pos[1].ToString("f2")}(Chuyển động đến vị trí phù hợp XYR thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        if (placeTargetInfo.Count == 2)//同时贴两个料时，把编号存在FeederCddSn中
                        {
                            _camera_Component.FeederCddSn[nozzleNo - 1] = placeTargetInfo[0].CavityNum.ToString()+","+ placeTargetInfo[1].CavityNum.ToString();
                        }
                        else//单料
                        {
                            _camera_Component.FeederCddSn[nozzleNo - 1] = placeTargetInfo[i].CavityNum.ToString();
                        }
                        //移动图片
                        _camera_Component.CavGroup(carrierStatus.carrierSN, "cav_" + placeTargetInfo[i].CavityNum, _camera_Component.FeederCddSn[nozzleNo - 1], (nozzleNo - 1).ToString());
                        _camera_Component.FeederCddSn[nozzleNo - 1] = "";
                        _camera_Component.DownCddSn[nozzleNo - 1] = "";


                        /****增加Feeder拍照功能***/
                        if (!Enable.IsFeederCheck && isFirstPlace && Enable.PickOpportunity == EN_PickOpportunity.AfterStart)
                        {
                            isFirstPlace = false;
                            FeederCDD();
                        }
                        //对应吸嘴气缸下降
                        if (!_mGoogol_Component.SetCylinderUpDown(nozzleNo, false, false) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->打开气缸失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[X2,Y2]:{placeTargetInfo[i].Pos[0].ToString("f2")},{placeTargetInfo[i].Pos[1].ToString("f2")}(Mở xi lanh thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //等待气缸下降一段时间再找压力
                        await Task.Delay(100);
                        //Z轴降速
                        if (!_mGoogol_Component.SetSpeed(En_AxisNum.Z2, recipe.Place1stHeightSetSpeed) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->设置贴合一次高度速度：{NLogTrace.GetFloatArrayString(recipe.Place1stHeightSetSpeed)} 失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[R]:{placeTargetInfo[i].Pos[2].ToString("f2")}(Thiết lập tốc độ độ cao phù hợp một lần thất bại)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //运动到贴合位置，分找压力和不找压力两种情况
                        //realPress表示吸嘴稳定时候的压力值，用于在后面判断压力是否达标
                        float realPress = 0;
                        short pressGet;
                        if (recipe.PosMode && !_plc_Component.IsSimulateRun())
                        {
                            if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, placeTargetInfo[i].BasePos[2], false, true, false))
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->开始运动到贴合点失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Chuyển động đến điểm phù hợp thất bại)", En_Logout_Type.Alarm, true);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                            //找压力模式，需要实时读取压力
                            DateTime startTime = DateTime.Now;
                            //目标压力
                            float targetPress = recipe.Press;
                            //压力提前量，因为发送轴停止指令需要一段时间
                            float beforePress = recipe.BeforePress;
                            //只要到这个压力就立刻发送轴停止指令
                            float stopMovePress = targetPress - beforePress;
                            while (true)
                            {
                                if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                //获取当前压力
                                pressGet = _mGoogol_Component.AInput[nozzleNo - 1];
                                realPress = _cacheParamManager.PressParam.SglParam[nozzleNo - 1].GetCalibedPress(pressGet);
                                //压力达标则停止轴运动，注意发送停止指令后轴还会往下走一段（通信需要时间）
                                if (realPress >= stopMovePress)
                                {
                                    _mGoogol_Component.StopAxisMove(En_AxisNum.Z2);
                                    break;
                                }
                                //如果规定时间内未找到压力，跳出
                                if ((DateTime.Now - startTime).TotalMilliseconds >= recipe.FindPressTimeout)
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"Place->找压力到达超时时间 {recipe.FindPressTimeout} ms", En_Logout_Type.Alarm, true);
                                    break;
                                }
                                //如果移动到目标位置还未找到压力，跳出
                                //为true表示没有轴在运动
                                if (_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"Place->{placeTargetInfo[i].CavityNum}未能找到指定压力", En_Logout_Type.Alarm, true);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (_plc_Component.IsSimulateRun())
                            {
                                //直接下降到固定高度
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, placeTargetInfo[i].BasePos[2] - 10, true, true, false))
                                {
                                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->运动到贴合点失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Chuyển động đến điểm phù hợp thất bại)", En_Logout_Type.Alarm, true);
                                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                                }
                            }
                            else
                            {
                                //直接下降到固定高度
                                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, placeTargetInfo[i].BasePos[2], true, true, false))
                                {
                                    if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                                    if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->运动到贴合点失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Chuyển động đến điểm phù hợp thất bại)", En_Logout_Type.Alarm, true);
                                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                                }
                            }
                        }
                        //贴合保压
                        await Task.Delay(recipe.Place1stDelay);
                        //等待一段时间后读到的压力才是稳定的压力，下面再判断压力够不够
                        if (!_plc_Component.IsSimulateRun())
                        {
                            pressGet = _mGoogol_Component.AInput[nozzleNo - 1];
                            realPress = _cacheParamManager.PressParam.SglParam[nozzleNo - 1].GetCalibedPress(pressGet);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Place->{placeTargetInfo[i].CavityNum}穴压力 {realPress.ToString("F2")} N", En_Logout_Type.Run, true);
                            //保存压力，单位kg
                            carrierStatus.Tape_PastePress[placeTargetInfo[i].CavityNum - 1] = realPress / 10.0f;
                        }
                        //关闭真空吸 -> 打开真空破 -> 等待 -> 气缸上抬 -> 关闭真空破
                        while (!_stepStatus.SetCloseSucOpenBreakAndCylinderUp(nozzleNo, recipe.BreakDelay) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->关闭真空吸，打开真空破失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[X2,Y2]:{placeTargetInfo[i].Pos[0].ToString("f2")},{placeTargetInfo[i].Pos[1].ToString("f2")}(Phá vỡ chân không thất bại)", En_Logout_Type.Alarm, true);
                            var alarm = nozzleNo == 1 ? _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, "设置吸嘴气缸1下降失败(Thiết lập xi lanh rơi thất bại)", 64119) :
                                _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, "设置吸嘴气缸2下降失败(Thiết lập xi lanh rơi thất bại)", 64121);

                            if (MessageBox.Show("设置气缸上抬失败，是否重新上抬气缸？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                continue;
                            }
                            else
                            {
                                _baseBiz.RemoveRunAlarm(alarm);
                                return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                            }
                        }
                        //将吸嘴号保存到nozzleID中，以便于后续写测量信息给后站
                        carrierStatus.Tape_Nozzle[placeTargetInfo[i].CavityNum - 1] = placeTargetInfo[i].NozzleNo;
                        /*****************************************************************************/
                        //获取气缸上到位信号
                        if (_mGoogol_Component.IsCylinderUpDownReady(nozzleNo, true) == false || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"贴合获取{nozzleNo}气缸上到位失败(Thất bại ở vị trí xi lanh)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //压力超限报警判断，放在气缸缩回后面以确保不会因为报警导致压着物料
                        if (!_plc_Component.IsSimulateRun())
                        {
                            if (realPress < recipe.MinPress || realPress > recipe.MaxPress)
                            {
                                var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, "吸嘴" + nozzleNo + "贴" + cavityNo + "穴压力超限(Áp lực vượt quá)", 112);
                                if (MessageBox.Show("吸嘴" + nozzleNo + "贴" + cavityNo + "穴压力超限，为" + realPress.ToString("f2") + "N\n确认继续吗？(Áp suất vượt quá, xác nhận tiếp tục?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                                {
                                    _baseBiz.RemoveRunAlarm(alarm);
                                }
                                else
                                {
                                    _baseBiz.RemoveRunAlarm(alarm);
                                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                                }
                            }
                        }
                        //检测该吸嘴真空值，确保未带起物料
                        //这里不能加延时，影响ct，且这个判断用处不大
                        if (_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck && !_plc_Component.IsSimulateRun())
                        {
                            if (_mGoogol_Component.GetMaterialReady(nozzleNo))
                            {
                                var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, "吸嘴" + nozzleNo + "贴" + cavityNo + "穴后仍吸料(Miệng hút vẫn còn nguyên vật liệu)", 112);
                                if (MessageBox.Show("吸嘴" + nozzleNo + "贴" + cavityNo + "穴后仍吸料\n确认继续吗？(Vật liệu vẫn còn sau khi miệng hút phù hợp, xác nhận tiếp tục?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                                {
                                    _baseBiz.RemoveRunAlarm(alarm);
                                }
                                else
                                {
                                    _baseBiz.RemoveRunAlarm(alarm);
                                    return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                                }
                            }
                        }
                        //回归工作速度
                        if (!_mGoogol_Component.SetSpeed(En_AxisNum.Z2, En_SpeedType.Work) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                        {
                            if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                            if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->运动到贴合点Z到位后，设置上抬工作速度失败，穴位号：{placeTargetInfo[i].CavityNum},吸嘴编号：{nozzleNo}，Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Thiết lập tốc độ làm việc nâng không thành công)", En_Logout_Type.Alarm, true);
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        //已经贴合完成
                        carrierStatus.cavStateStr[placeTargetInfo[i].CavityNum - 1] = "贴合OK";
                        //只要不是空跑，就增加吸嘴寿命
                        if (!_plc_Component.IsSimulateRun())
                        {
                            _plc_Component.AddAlive(nozzleNo);
                        }
                        //增加uph
                        if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                        {
                            _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddUph(true);
                            _stepStatus.OutputPerHour.AddOne(true);
                        }
                        else
                        {
                            _stepStatus.CacheParamManager.HomeUiParam.Statistic.AddUph(false);
                        }
                        //生成csv文件
                        WriteDataToCsv(carrierStatus, placeTargetInfo[i]);
                        //刷新穴位状态
                        PublishCavityStateMsg(placeTargetInfo[i].CavityNum, EN_TrayStatus.OK);
                        //存一下tray，防止中途复位导致tray没有保存
                        if (!_stepStatus.BackupAndWriteTray(carrierStatus))
                        {
                            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                        }
                        if (placeTargetInfo[i].CavityNum == 10)
                        {
                            _stepStatus.IFitCav10 = true;
                        }
                    }
                    _stepStatus.CurNeedProcess.Remove(placeTargetInfo[i].VisionPoint);
                }

                //抛掉NG的物料，因为在autostart检测过ng仓Y小于安全距离，所以可以先释放轴锁再抛料
                HashSet<int> throwTapeNozzleNoSet = new HashSet<int>();
                if (!_stepStatus.CacheParamManager.HomeUiParam.Enable.UseVacSucCheck)
                {
                    //禁用真空吸检测，必须全部抛掉，因为禁用真空吸检测会全部取料
                    throwTapeNozzleNoSet = new HashSet<int> { 1, 2 };
                }
                else
                {
                    //暂存要抛料的吸嘴
                    for (int nozzleIdx = 0; nozzleIdx < 2; nozzleIdx++)
                    {
                        //抛料条件：吸嘴启用、取料ok、下视觉ng
                        if (_stepStatus.UseNozzle(nozzleIdx + 1) && _stepStatus.PickTapeOk[nozzleIdx] && !_stepStatus.DownCamOk[nozzleIdx])
                        {
                            throwTapeNozzleNoSet.Add(nozzleIdx + 1);
                        }
                    }
                }

                if (throwTapeNozzleNoSet.ToList().Count > 0)
                {
                    if (!_stepStatus.ThrowTapes(throwTapeNozzleNoSet, true && Enable.PickOpportunity == EN_PickOpportunity.AfterStart, false, EN_TossingCode.MaterialDeflect, carrierStatus, true) || _mGoogol_Component.Exit() || _stepStatus.NextStep2 != RunStep)
                    {
                        if (_stepStatus.NextStep2 != RunStep) return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
                        if (_mGoogol_Component.Exit()) return (false, EN_RunRet.MotionErr, EN_RunStep.Stop);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Place->抛料失败(Ném thất bại)", En_Logout_Type.Alarm, true);
                        return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
                    }
                }

                #region 释放轴锁，危险操作！！！！勿随意更改代码位置/内容
                if (!_stepStatus.IsAllUpCamFinished())
                {
                    //上视觉未完毕，应继续上视觉
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Place->暂停贴合线程，继续上视觉处理", En_Logout_Type.Run, true);
                    _stepStatus.SetMoveMutex(true);
                    _stepStatus.AutoResetEvt_CtrlVisual.Set();
                    _stepStatus.ManualResetEvt_CtrlPlace.Reset();//暂停该线程
                }
                else
                {
                    //上视觉完毕，无需上视觉。此时可能载具剩余穴位都贴完，也有可能没贴完（例如只有两个ok的tape但是需要贴三个穴位）
                    if (_stepStatus.CurNeedProcess.Count == 0)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Place->暂停贴合线程，放行载具", En_Logout_Type.Run, true);
                        _stepStatus.isEndCarrierFinish = true;
                        _stepStatus.isCavityNull = 0;
                        //载具剩余穴位都贴完，放行
                        if (!_stepStatus.CarrierFinish(carrierStatus))
                        {
                            return (false, EN_RunRet.DefaultErr, EN_RunStep.Err);
                        }
                        _stepStatus.SetMoveMutex(true);
                        _stepStatus.ManualResetEvt_CtrlPlace.Reset();//暂停该线程
                    }
                }
                #endregion
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.AutoProcess);
        }

        /// <summary>
        /// 将某个指定穴位的数据记录到表格中
        /// </summary>
        private void WriteDataToCsv(CarrierStatus carrierStatus, PlaceTargetInfo placeTargetInfo)
        {
            string dir0 = $@"D:\QKProject\Data\Product";
            string result = (placeTargetInfo.CanPlace) ? "OK" : "NG";

            if (!Directory.Exists(dir0))
            {
                Directory.CreateDirectory(dir0);
            }
            string file = $@"{dir0}\{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
            bool existCsv = File.Exists(file);
            try
            {
                using (StreamWriter sw = new StreamWriter(file, true))
                {
                    if (!existCsv)
                    {
                        sw.WriteLine($"贴合时间,载具码,穴位号,吸嘴号,贴合结果,贴合坐标X,贴合坐标Y,贴合坐标R");
                    }
                    sw.WriteLine(
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "," +
                        carrierStatus.carrierSN + "," +
                        placeTargetInfo.CavityNum + "," +
                        placeTargetInfo.NozzleNo + "," +
                        result + "," +
                        placeTargetInfo.Pos[0].ToString("F3") + "," +
                        placeTargetInfo.Pos[1].ToString("F3") + "," +
                        placeTargetInfo.Pos[2].ToString("F2")
                        );
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private void PublishCavityStateMsg(int cavity, EN_TrayStatus status)
        {
            _eventAggregator.Publish(new CarrierInfoPanelMessage() { Cavity = cavity, Status = status, }, action => { Task.Run(action); });
        }
        /// <summary>
        /// 相机拍照
        /// </summary>
        /// <returns></returns>
        private async void FeederCDD(bool isCDD = true)
        {
            await Task.Run(() =>
            {
                try
                {
                    //等待物料到位
                    if (!_stepStatus.TrigFeederConveyTape(true))
                    {
                        return;
                    }
                    if (!isCDD)
                    {
                        return;
                    }
                    if (!_camera_Component.FeederCdd())
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Feeder提前拍照-程序错误", En_Logout_Type.Run, true);
                }
            });
        }

    }
}
