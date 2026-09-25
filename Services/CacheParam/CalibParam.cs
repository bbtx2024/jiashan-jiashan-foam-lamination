/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-28
 * 说明：（标定数据存储）
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
    [Description("标定参数设置")]
    public class CalibParam
    {
        [DisplayName("左工位喷咀相对相机距离_X")]
        public float StationsCaliLeft_X { get; set; } = 0;

        [DisplayName("左工位喷咀相对相机距离_Y")]
        public float StationsCaliLeft_Y { get; set; } = 0;

        [DisplayName("右工位喷咀相对相机距离_X")]
        public float StationsCaliRight_X { get; set; } = 0;

        [DisplayName("右工位喷咀相对相机距离_Y")]
        public float StationsCaliRight_Y { get; set; } = 0;

        [DisplayName("激光测高相对相机距离_X")]
        public float LaserHighToCamer_X { get; set; } = 0;

        [DisplayName("激光测高相对相机距离_Y")]
        public float LaserHighToCamer_Y { get; set; } = 0;

        [DisplayName("激光测高相对喷咀的距离")]
        public float LaserHighToNozzleValue { get; set; } = 0;

        [DisplayName("激光测高标定时喷咀下压的距离")]
        public float NozzleToGT2Value { get; set; } = 0;

        [DisplayName("低压校准写入值")]
        public float LowerCalibPressValue { get; set; } = 0;

        [DisplayName("低压传感器值")]
        public float LowerCalibPressSensorValue { get; set; } = 0;

        [DisplayName("高压校准写入值")]
        public float HigherCalibPressValue { get; set; } = 0;

        [DisplayName("高压传感器值")]
        public float HigherCalibPressSensorValue { get; set; } = 0;

    }
}
