using System;
using System.ComponentModel;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.MES
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("MES设置")]
    public class MESParam : IParam
    {
        [Category("0.启用"), DisplayName("模块启用")]
        public override bool BUse { get; set; } = true;

        [Category("0.启用")]
        [DisplayName("启动时是否MES检查软件版本")]
        //public bool UploadAlarmWhenStart { get; set; } = false;
        public bool IsCheckVersion { get; set; } = true;

        [Category("1.Tape相关")]
        [DisplayName("TapeSN位数")]
        public EN_TapeLenKind TapeLenKind { get; set; } = EN_TapeLenKind.Length27;
        [Browsable(false)]
        public int TapeLength => (int)TapeLenKind;

        [Category("1.Tape相关")]
        [DisplayName("所有TapeIPN")]
        public string TapeIPNs { get; set; } = "946-28646-06001,946-28646-06002";

        [Category("1.Tape相关")]
        [DisplayName("飞达1TapeSN")]
        public string Feeder1TapeSN { get; set; } = "FWV3132946-28646-06002078T6";

        [Category("1.Tape相关")]
        [DisplayName("飞达2TapeSN")]
        public string Feeder2TapeSN { get; set; } = "FWV3132946-28646-06002078T6";
        [Category("1.Tape相关")]
        [DisplayName("Tape单卷最大物料数")]
        public int TapeAlarmMaxUseCount { get; set; } = 2000;

        [Category("1.Tape相关")]
        [DisplayName("Tape剩余预警物料数")]
        public int TapeAlarmTipLeftCount { get; set; } = 200;

        [Category("1.Tape相关")]
        [DisplayName("Tape物料名称")]
        public string TapeName { get; set; } = "B2B Tape";

        [Category("2.MES参数设定")]
        [DisplayName("Line")]
        public string Line { get; set; } = "BU22-J6311";//NPI: BU22-J6102

        [Category("2.MES参数设定")]
        [DisplayName("Station")]
        public string Station { get; set; } = "Flex GND Tape";

        [Category("2.MES参数设定")]
        [DisplayName("Fixid")]
        public string Fixid { get; set; } = "A1-1F-L01L-Step Flex Bend and Assy";//NPI: 001

        [Category("2.MES参数设定")]
        [DisplayName("MesIP")]
        public string MesIP { get; set; } = "";

        [Category("2.MES参数设定")]
        [DisplayName("操作人员编号")]
        public string Opuserid { get; set; } = "Quick";

        [Category("4.上传载具异常信息")]
        [DisplayName("启用上传BadCarrier")]
        public bool UseCheckCarrier { get; set; } = false;
    }
}
