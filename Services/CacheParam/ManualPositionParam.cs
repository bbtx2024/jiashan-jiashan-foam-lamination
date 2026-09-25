/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-23
 * 说明：（定位位置数据）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using QA_Infrastructure;
namespace QA.Business.CacheParam
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    [Description("点检位置设置")]
    public class ManualPositionParam
    {

        #region  FA11-004

        #region 标定
        [DisplayName("吸嘴1Feeder1相机标定坐标")]
        public float[] AxisNozzle1CalibFeeder1Pos
        {
            get; set;
        } = new float[4];
        [DisplayName("吸嘴1Feeder2相机标定坐标")]
        public float[] AxisNozzle1CalibFeeder2Pos
        {
            get; set;
        } = new float[4];
        [DisplayName("吸嘴2Feeder1相机标定坐标")]
        public float[] AxisNozzle2CalibFeeder1Pos
        {
            get; set;
        } = new float[4];
        [DisplayName("吸嘴2Feeder2相机标定坐标")]
        public float[] AxisNozzle2CalibFeeder2Pos
        {
            get; set;
        } = new float[4];



        [DisplayName("载具Fov标定1号坐标")]
        public float[] AxisCarrierJointCalibPosNo1
        {
            get; set;
        } = new float[2];
        [DisplayName("载具Fov标定2号坐标")]
        public float[] AxisCarrierJointCalibPosNo2
        {
            get; set;
        } = new float[2];
        [DisplayName("1号嘴吸取标定片1号坐标")]
        public float[] AxisNozzleNo1SucPiecePosNo1
        {
            get; set;
        } = new float[4];
        [DisplayName("1号嘴吸取标定片2号坐标")]
        public float[] AxisNozzleNo1SucPiecePosNo2
        {
            get; set;
        } = new float[4];


        [DisplayName("1#吸嘴联合标定坐标")]
        public float[] AxisCalibJoint_NozzleNo1Pos
        {
            get; set;
        } = new float[4];
        [DisplayName("2#吸嘴联合标定坐标")]
        public float[] AxisCalibJoint_NozzleNo2Pos
        {
            get; set;
        } = new float[4];
        [DisplayName("1#吸嘴训练坐标")]
        public float[] TTNNozzleNo1Pos
        {
            get; set;
        } = new float[4];
        [DisplayName("2#吸嘴训练坐标")]
        public float[] TTNNozzleNo2Pos
        {
            get; set;
        } = new float[4];
        #endregion


        #region 生产
        [DisplayName("Feeder 吸嘴1左取料位")]
        public float[] AxisFeederPickNozzle1LeftPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder 吸嘴1右取料位")]
        public float[] AxisFeederPickNozzle1RightPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder 吸嘴2左取料位")]
        public float[] AxisFeederPickNozzle2LeftPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder 吸嘴2右取料位")]
        public float[] AxisFeederPickNozzle2RightPos
        {
            get; set;
        } = new float[4];


        [DisplayName("Feeder2 吸嘴1左取料位")]
        public float[] AxisFeeder2PickNozzle1LeftPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder2 吸嘴1右取料位")]
        public float[] AxisFeeder2PickNozzle1RightPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder2 吸嘴2左取料位")]
        public float[] AxisFeeder2PickNozzle2LeftPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder2 吸嘴2右取料位")]
        public float[] AxisFeeder2PickNozzle2RightPos
        {
            get; set;
        } = new float[4];



        [DisplayName("下视觉拍照喷嘴坐标")]
        public float[] AxisDownCamera_NoPos
        {
            get; set;
        } = new float[5];
        #endregion

        #endregion



        [DisplayName("9点标定移动距离")]
        public float MoveDisNineCalib { get; set; } = 5;

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_1 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_2 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_3 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_4 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_5 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_6 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_7 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_8 { get; set; } = new float[4];

        [DisplayName("1号上相机9点标定起始点坐标")]
        public float[] UpCameraNo1NineCalibPos_9 { get; set; } = new float[4];

        

        [DisplayName("Feeder 吸嘴3左取料位")]
        public float[] AxisFeederPickNozzle3LeftPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder 吸嘴3右取料位")]
        public float[] AxisFeederPickNozzle3RightPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder 吸嘴4左取料位")]
        public float[] AxisFeederPickNozzle4LeftPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder 吸嘴4右取料位")]
        public float[] AxisFeederPickNozzle4RightPos
        {
            get; set;
        } = new float[4];

        [DisplayName("Feeder 吸嘴13取料位")]
        public float[] AxisFeederPickNozzle13Pos
        {
            get; set;
        } = new float[5];

        [DisplayName("Feeder 吸嘴24取料位")]
        public float[] AxisFeederPickNozzle24Pos
        {
            get; set;
        } = new float[5];

       
        [DisplayName("1号嘴吸取标定片3号坐标")]
        public float[] AxisNozzleNo1SucPiecePosNo3
        {
            get; set;
        } = new float[4];
        [DisplayName("1号嘴吸取标定片4号坐标")]
        public float[] AxisNozzleNo1SucPiecePosNo4
        {
            get; set;
        } = new float[4];

        [DisplayName("载具Fov标定3号坐标")]
        public float[] AxisCarrierJointCalibPosNo3
        {
            get; set;
        } = new float[2];
        [DisplayName("载具Fov标定4号坐标")]
        public float[] AxisCarrierJointCalibPosNo4
        {
            get; set;
        } = new float[2];

        [DisplayName("1号吸嘴Ng料仓坐标")]
        public float[] AxisNgSiloPos
        {
            get; set;
        } = new float[3];
        [DisplayName("2号吸嘴Ng料仓坐标")]
        public float[] AxisNgSiloPos2
        {
            get; set;
        } = new float[3];
        [DisplayName("Feeder1拍照坐标")]
        public float[] AxisFeeder1Pos
        {
            get; set;
        } = new float[3];
        [DisplayName("Feeder2拍照坐标")]
        public float[] AxisFeeder2Pos 
        {
            get; set;
        } = new float[3];

        [DisplayName("扫载具码坐标")]
        public float[] AxisScannerCarrierBarcodePos
        {
            get; set;
        } = new float[2];

        [DisplayName("测试次数")]
        public int TestCount
        {
            get; set;
        }

        [DisplayName("轴精度停止时间")]
        public float AxisWaitTime
        {
            get; set;
        }

        [DisplayName("上相机静态测试坐标")]
        public float[] UpCamStaticTestPos
        {
            get; set;
        } = new float[4];

        [DisplayName("下相机静态测试坐标")]
        public float[] DownCamStaticTestPos
        {
            get; set;
        } = new float[4];

        [DisplayName("上相机动态测试起点")]
        public float[] UpCamDynamicTestStartPos
        {
            get; set;
        } = new float[5];

        [DisplayName("上相机动态测试终点")]
        public float[] UpCamDynamicTestEndPos
        {
            get; set;
        } = new float[5];

        [DisplayName("下相机动态测试起点")]
        public float[] DownCamDynamicTestStartPos
        {
            get; set;
        } = new float[5];

        [DisplayName("下相机动态测试终点")]
        public float[] DownCamDynamicTestEndPos
        {
            get; set;
        } = new float[5];



        [DisplayName("相机轴-轴精度开始点位")]
        public float[] UpCamAxisTestStartPos
        {
            get; set;
        } = new float[5];

        [DisplayName("相机轴-轴精度结束点位")]
        public float[] UpCamAxisTestEndPos
        {
            get; set;
        } = new float[5];

        [DisplayName("吸嘴轴-轴精度结束点位")]
        public float[] DownCamAxisTestStartPos
        {
            get; set;
        } = new float[5];

        [DisplayName("吸嘴轴-轴精度结束点位")]
        public float[] DownCamAxisTestEndPos
        {
            get; set;
        } = new float[5];


        [DisplayName("应力测试-开始点位")]
        public float[] StressTestStartPos
        {
            get; set;
        } = new float[5];

        [DisplayName("应力测试-结束点位")]
        public float[] StressTestEndPos
        {
            get; set;
        } = new float[5];





        [DisplayName("1#吸嘴标定起始坐标")]
        public float[] AxisCalibNineAddTwo_NozzleNo1Pos
        {
            get; set;
        } = new float[4];

        [DisplayName("2#吸嘴标定起始坐标")]
        public float[] AxisCalibNineAddTwo_NozzleNo2Pos
        {
            get; set;
        } = new float[4];

        [DisplayName("3#吸嘴标定起始坐标")]
        public float[] AxisCalibNineAddTwo_NozzleNo3Pos
        {
            get; set;
        } = new float[4];

        [DisplayName("4#吸嘴标定起始坐标")]
        public float[] AxisCalibNineAddTwo_NozzleNo4Pos
        {
            get; set;
        } = new float[4];
        

        [DisplayName("下视觉拍照喷嘴物料1号坐标")]
        public float[] AxisDownCamera_No1Pos
        {
            get; set;
        } = new float[4];

        [DisplayName("下视觉拍照喷嘴物料2号坐标")]
        public float[] AxisDownCamera_No2Pos
        {
            get; set;
        } = new float[4];

        [DisplayName("下视觉拍照喷嘴物料3号坐标")]
        public float[] AxisDownCamera_No3Pos
        {
            get; set;
        } = new float[4];

        [DisplayName("下视觉拍照喷嘴物料4号坐标")]
        public float[] AxisDownCamera_No4Pos
        {
            get; set;
        } = new float[4];

        [DisplayName("压力标定坐标")]
        public float[] CalibPressSensorPos
        {
            get; set;
        } = new float[4];
    }
}
