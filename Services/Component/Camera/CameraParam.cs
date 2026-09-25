/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-24
 * 说明：（相机参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.Camera
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    [Description("相机设置")]
    public class CameraParam : IParam
    {
        [Category("0.启用"), DisplayName("模块启用")]
        public override bool BUse { get => true; }
        [Category("0.启用"), DisplayName("存全图启用")]
        public bool SaveAllPhotos { get; set; }

        [Category("0.启用"), DisplayName("下视觉人工判断启用")]
        public bool OP_DownCdd { get; set; }

        //public override bool BUse { get; set; } = false;

        [Category("0.启用")]
        [DisplayName("使用新视觉指令")]
        [Browsable(false)]
        public bool UseNewOrder { get; set; }
        [Category("0.保压X轴偏移基准值")]
        [DisplayName("保压X轴偏移基准值")]
        [Browsable(false)]
        public float PressurizePosX { get; set; } = 10;

        [XmlIgnore]
        [Category("1.通讯参数")]
        [DisplayName("IP")]
        public string IP { get; set; } = "127.0.0.1";

        [XmlIgnore]
        [Category("1.通讯参数")]
        [DisplayName("端口")]
        public int Port { get; set; } = 8150;

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

        [Category("2.视觉相关设置")]
        [DisplayName("上相机拍照前等待时间")]
        public int UpCamWaitTime { get; set; } = 100;

        [Category("2.视觉相关设置")]
        [DisplayName("下相机拍照前等待时间")]
        public int DownCamWaitTime { get; set; } = 100;

        private string visionPath = @"D:\Cognex\SPP\SmallPartsPlacement.exe";
        [Category("2.视觉相关设置")]
        [DisplayName("SPP视觉exe文件路径")]
        public string VisionPath
        {
            get
            {
                return visionPath;
            }
            set
            {
                if (!visionPath.EndsWith(".exe"))
                {
                    visionPath = "";
                }
                else
                {
                    visionPath = value;
                }
            }
        }

        private string imgPath = @"D:\Cognex\Images";
        [Category("2.视觉相关设置")]
        [DisplayName("图片存储路径")]
        public string ImgPath
        {
            get
            {
                return imgPath;
            }
            set
            {
                imgPath = value.TrimEnd('\\');
            }
        }

        [Category("3.FTP图片上传")]
        [DisplayName("启用图片打包上传至服务器")]
        public bool FTP_AutoUploadImg { get; set; } = false;

        [Category("3.FTP图片上传")]
        [DisplayName("服务器IP")]
        public string FTP_ServerIP { get; set; } = "10.55.60.30";

        [Category("3.FTP图片上传")]
        [DisplayName("服务器端口")]
        public int FTP_ServerPort { get; set; } = 21;

        [Category("3.FTP图片上传")]
        [DisplayName("登录用户名")]
        public string FTP_UserName { get; set; } = "LA3_Line1";

        [Category("3.FTP图片上传")]
        [DisplayName("登陆密码")]
        public string FTP_Password { get; set; } = "sunny123";

        [Category("3.FTP图片上传")]
        [DisplayName("线别")]
        public string FTP_Line { get; set; } = "LA3_Line1";

        [Category("3.FTP图片上传")]
        [DisplayName("工站")]
        public string FTP_Station { get; set; } = "Step ANT-贴合";
        [Category("4.视觉NG自动重拍次数(0次为不重拍)")]
        [DisplayName("Feeder相机")]
        public int FeederNGCDDnum { get; set; } = 0;
        [Category("4.视觉NG自动重拍次数(0次为不重拍)")]
        [DisplayName("保压相机")]
        [Browsable(false)]
        public int PreNGCDDnum { get; set; } = 0;
        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("是否启用")]
        public bool IsDownMarkBus { get; set; } = false;

        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("角度基准值-A")]
        public float DownMarkAngle { get; set; } = 0;
        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("角度偏移量-A(±)")]
        public float DownMarkAnglePos { get; set; } = 0;

        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("吸嘴1基准值-X")]
        public float DownMark_X1 { get; set; } = 0;
        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("吸嘴1基准值-Y")]
        public float DownMark_Y1 { get; set; } = 0;

        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("吸嘴2基准值-X")]
        public float DownMark_X2 { get; set; } = 0;
        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("吸嘴2基准值-Y")]
        public float DownMark_Y2 { get; set; } = 0;

        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("吸嘴X偏移量-X(±)")]
        public float DownMarkpos_X { get; set; } = 0;
        [Category("6.视觉防呆-下相机定位")]
        [DisplayName("吸嘴Y偏移量-Y(±)")]
        public float DownMarkpos_Y { get; set; } = 0;



        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("是否启用")]
        public bool IsFeedRepairBus { get; set; } = false;

        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("1号吸嘴左取料X补偿值-X")]
        public float FeedRepair_LX1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("1号吸嘴左取料Y补偿值-Y")]
        public float FeedRepair_LY1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("1号吸嘴左取料R补偿值-R")]
        public float FeedRepair_LA1 { get; set; } = 0;

        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("1号吸嘴右取料X补偿值-X")]
        public float FeedRepair_RX1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("1号吸嘴右取料Y补偿值-Y")]
        public float FeedRepair_RY1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("1号吸嘴右取料R补偿值-R")]
        public float FeedRepair_RA1 { get; set; } = 0;


        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("2号吸嘴左取料X补偿值-X")]
        public float FeedRepair_LX2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("2号吸嘴左取料Y补偿值-Y")]
        public float FeedRepair_LY2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("2号吸嘴左取料R补偿值-R")]
        public float FeedRepair_LA2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("2号吸嘴右取料X补偿值-X")]
        public float FeedRepair_RX2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("2号吸嘴右取料Y补偿值-Y")]
        public float FeedRepair_RY2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed相机定位")]
        [DisplayName("2号吸嘴右取料R补偿值-R")]
        public float FeedRepair_RA2 { get; set; } = 0;



        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("1号吸嘴左取料X补偿值-X")]
        public float Feed2Repair_LX1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("1号吸嘴左取料Y补偿值-Y")]
        public float Feed2Repair_LY1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("1号吸嘴左取料R补偿值-R")]
        public float Feed2Repair_LA1 { get; set; } = 0;

        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("1号吸嘴右取料X补偿值-X")]
        public float Feed2Repair_RX1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("1号吸嘴右取料Y补偿值-Y")]
        public float Feed2Repair_RY1 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("1号吸嘴右取料R补偿值-R")]
        public float Feed2Repair_RA1 { get; set; } = 0;


        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("2号吸嘴左取料X补偿值-X")]
        public float Feed2Repair_LX2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("2号吸嘴左取料Y补偿值-Y")]
        public float Feed2Repair_LY2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("2号吸嘴左取料R补偿值-R")]
        public float Feed2Repair_LA2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("2号吸嘴右取料X补偿值-X")]
        public float Feed2Repair_RX2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("2号吸嘴右取料Y补偿值-Y")]
        public float Feed2Repair_RY2 { get; set; } = 0;
        [Category("7.视觉补偿-Feed2相机定位")]
        [DisplayName("2号吸嘴右取料R补偿值-R")]
        public float Feed2Repair_RA2 { get; set; } = 0;
    }
}
