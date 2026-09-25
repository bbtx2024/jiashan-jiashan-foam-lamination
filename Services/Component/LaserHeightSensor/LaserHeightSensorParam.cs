/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-12
 * 说明：（激光测高仪参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.LaserHeightSensor
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("激光测高设置")]
    public class LaserHeightSensorParam : IParam
    {
        [XmlIgnore]
        [Category("1.COM通讯参数")]
        [DisplayName("端口号")]
        public string COMPort { get; set; } = "COM2";

        [XmlIgnore]
        [Category("1.COM通讯参数")]
        [DisplayName("波特率")]
        public int Baudrate { get; set; } = 115200;

        [XmlIgnore]
        [Category("1.通讯参数")]
        [DisplayName("写入超时")]
        [Description("ms")]
        public int SendTimeout { get; set; } = 3000;

        [XmlIgnore]
        [Category("1.通讯参数")]
        [DisplayName("读取超时")]
        [Description("ms")]
        public int ReceiveTimeout { get; set; } = 3000;

        [XmlIgnore]
        [Category("2.参数设置")]
        [DisplayName("激光测高延时")]
        [Description("ms")]
        public int MeasureHeightDelay { get; set; } = 100;

        [Category("2.参数设置"), DisplayName("焊接点1基准高度")]
        public float BaseHeight { get; set; } = 0;

        [Category("2.参数设置"), DisplayName("焊接点2基准高度")]
        public float BaseHeight2 { get; set; } = 0;

        [Category("2.参数设置"), DisplayName("允许误差")]
        public float OffsetHeight { get; set; } = 0;

        [Category("2.参数设置"), DisplayName("测高范围>=")]
        public float HeightMoreDate { get; set; } = 0;

        [Category("2.参数设置"), DisplayName("测高范围<=")]
        public float HeightLessDate { get; set; } = 0;
    }
}
