/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-10
 * 说明：（PDCA参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.PDCA
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("PDCA设置")]
    public class PDCAParam : IParam
    {
        [Category("1.PDCA通讯")]
        [DisplayName("IP")]
        public string IP { get; set; } = "127.0.0.1";

        [XmlIgnore]
        [Category("1.PDCA通讯")]
        [DisplayName("端口")]
        public int Port { get; set; } = 502;

        [XmlIgnore]
        [Category("1.PDCA通讯")]
        [DisplayName("缓冲区大小")]
        [Description("kb")]
        public int BufferSize { get; set; } = 1024;

        [XmlIgnore]
        [Category("1.PDCA通讯")]
        [DisplayName("超时")]
        [Description("ms")]
        public int Timeout { get; set; } = 3000;

        [Browsable(false)]
        private int _cavityNumLimit = 1;

        [XmlIgnore]
        [Category("2.治具信息")]
        [DisplayName("穴位数")]
        public int CavityNum
        {
            get { return _cavityNumLimit; }
            set
            {
                if (value < 1) value = 1;
                if (value > 100) value = 100;
                _cavityNumLimit = value;
            }
        }

        [Category("2.PDCA模拟")]
        [DisplayName("模拟PDCA数据")]
        public bool IsPdcaSimulated { get; set; } = false;

        [XmlIgnore]
        [Category("2.PDCA模拟")]
        [DisplayName("产品码模拟")]
        public string ProductSnSimulate { get; set; } = "Default";

        [XmlIgnore]
        [Category("2.PDCA模拟")]
        [DisplayName("载具码模拟")]
        public string CarrierSnSimulate { get; set; } = "Default";

        [Category("3.PDCA模式")]
        [DisplayName("PDCA模式")]
        public En_PdcaMode PdcaMode { get; set; } = En_PdcaMode.PROD;

        [Category("1.PDCA上传前检查设备参数")]
        [DisplayName("PDCA上传前检查设备参数")]
        public bool IsUseCheckDeviceParamBeforeUpLoad { get; set; } = false;

        [XmlIgnore]
        [Category("2.PDCA 参数")]
        [DisplayName("PDCA软件版本")]
        public string PdcaSoftWareVersion { get; set; } = "QkJS_v1.0";

        [XmlIgnore]
        [Category("2.PDCA 参数")]
        [DisplayName("PDCA虚拟SN的工厂代号")]
        public string PdcaDumySnPlantFactory { get; set; } = "C47";

        [XmlIgnore]
        [Category("2.PDCA 参数")]
        [DisplayName("PDCA虚拟SN工程师代号")]
        public string PdcaDumySnEngineeringCode { get; set; } = "JY88";

        [XmlIgnore]
        [Category("2.PDCA 参数")]
        [DisplayName("PDCA虚拟SN修订号")]
        public string PdcaDumySnRevision { get; set; } = "1";

        [XmlIgnore]
        [Category("2.PDCA 参数")]
        [DisplayName("本机IP地址")]
        public string PdcaLocalIP { get; set; } = "169.254.1.100";

        [XmlIgnore]
        [Category("2.PDCA 参数")]
        [DisplayName("本机开机用户名")]
        public string PdcaUserName { get; set; } = "KK";

        [XmlIgnore]
        [Category("2.PDCA 参数")]
        [DisplayName("本机开机密码")]
        public string PdcaPassWord { get; set; } = "QUICK8888";


    }
}
