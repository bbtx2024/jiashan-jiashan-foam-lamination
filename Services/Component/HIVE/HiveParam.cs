using QA.Business.Interfaces;
using QA_Infrastructure;
using System;
using System.ComponentModel;

namespace QA.Business.Component.HIVE
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    [Description("Hive系统设置")]
    public class HiveParam : IParam
    {
        [Category("0.启用"), DisplayName("模块启用")]
        public override bool BUse { get; set; } = true;

        [Category("1.Hive参数"), DisplayName("IP地址")]
        public string Hive_IP { get; set; } = "10.0.0.2";

        [Category("1.Hive参数"), DisplayName("端口号")]
        public string Hive_Port { get; set; } = "5008";

        [Category("2.其他"), DisplayName("Site")]
        public string Site { get; set; } = "ITJS";

        [Category("2.其他"), DisplayName("Station_type")]
        public string Station_type { get; set; } = "LA5";

        [Category("2.其他"), DisplayName("SF_line_ID")]
        public string SF_line_ID { get; set; } = "gnd";

        [Category("2.其他"), DisplayName("Vendor")]
        public string Vendor { get; set; } = "Quick";

        [Category("2.其他"), DisplayName("软件更新时间")]
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;

        [Category("2.其他"), DisplayName("软件版本维持红色天数")]
        public int UpdateRedDays { get; set; } = 7;

        //添加Hive点位信息""
        [Category("3.HIVE上传参数列表"), DisplayName("Safty position_X1")]
        public string Safty_position_X1 { get; set; } = "320";

        [Category("3.HIVE上传参数列表"), DisplayName("Safty position_Y1")]
        public string Safty_position_Y1 { get; set; } = "260";

        [Category("3.HIVE上传参数列表"), DisplayName("Safty position_X2")]
        public string Safty_position_X2 { get; set; } = "330";

        [Category("3.HIVE上传参数列表"), DisplayName("Safty position_Y2")]
        public string Safty_position_Y2 { get; set; } = "460";

        [Category("3.HIVE上传参数列表"), DisplayName("Safty position_Z2")]
        public string Safty_position_Z2 { get; set; } = "40";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_X1")]
        public string Pick_position_X1 { get; set; } = "200.783";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_Y1")]
        public string Pick_position_Y1 { get; set; } = "28.226";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_Z1")]
        public string Pick_position_Z1 { get; set; } = "22.764";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_R1")]
        public string Pick_position_R1 { get; set; } = "7.35";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_X2")]
        public string Pick_position_X2 { get; set; } = "200.63";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_Y2")]
        public string Pick_position_Y2 { get; set; } = "28.27";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_Z2")]
        public string Pick_position_Z2 { get; set; } = "23.084";

        [Category("3.HIVE上传参数列表"), DisplayName("Pick position_R2")]
        public string Pick_position_R2 { get; set; } = "7.35";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X1")]
        public string Vision_position_X1 { get; set; } = "247.673";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y1")]
        public string Vision_position_Y1 { get; set; } = "238.43";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X4")]
        public string Vision_position_X4 { get; set; } = "247.673";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y4")]
        public string Vision_position_Y4 { get; set; } = "193.067";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X7")]
        public string Vision_position_X7 { get; set; } = "247.673";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y7")]
        public string Vision_position_Y7 { get; set; } = "148.155";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X10")]
        public string Vision_position_X10 { get; set; } = "247.673";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y10")]
        public string Vision_position_Y10 { get; set; } = "102.672";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X2")]
        public string Vision_position_X2 { get; set; } = "157.701";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y2")]
        public string Vision_position_Y2 { get; set; } = "237.721";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X5")]
        public string Vision_position_X5 { get; set; } = "157.701";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y5")]
        public string Vision_position_Y5 { get; set; } = "192.914";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X8")]
        public string Vision_position_X8 { get; set; } = "157.701";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y8")]
        public string Vision_position_Y8 { get; set; } = "148.038";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X11")]
        public string Vision_position_X11 { get; set; } = "157.701";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y11")]
        public string Vision_position_Y11 { get; set; } = "102.898";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X3")]
        public string Vision_position_X3 { get; set; } = "67.746";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y3")]
        public string Vision_position_Y3 { get; set; } = "238.221";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X6")]
        public string Vision_position_X6 { get; set; } = "67.746";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y6")]
        public string Vision_position_Y6 { get; set; } = "193.323";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X9")]
        public string Vision_position_X9 { get; set; } = "67.746";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y9")]
        public string Vision_position_Y9 { get; set; } = "148.31";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X12")]
        public string Vision_position_X12 { get; set; } = "67.746";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y12")]
        public string Vision_position_Y12 { get; set; } = "103.302";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X13")]
        public string Vision_position_X13 { get; set; } = "208.454";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y13")]
        public string Vision_position_Y13 { get; set; } = "128.118";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Z13")]
        public string Vision_position_Z13 { get; set; } = "13.013";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_R13")]
        public string Vision_position_R13 { get; set; } = "7.351";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_X14")]
        public string Vision_position_X14 { get; set; } = "164.095";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Y14")]
        public string Vision_position_Y14 { get; set; } = "127.558";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_Z14")]
        public string Vision_position_Z14 { get; set; } = "13.013";

        [Category("3.HIVE上传参数列表"), DisplayName("Vision position_R14")]
        public string Vision_position_R14 { get; set; } = "8.453";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X1")]
        public string Assy_position_X1 { get; set; } = "112.576";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y1")]
        public string Assy_position_Y1 { get; set; } = "318.636";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z1")]
        public string Assy_position_Z1 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X4")]
        public string Assy_position_X4 { get; set; } = "68.681";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y4")]
        public string Assy_position_Y4 { get; set; } = "363.644";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z4")]
        public string Assy_position_Z4 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X7")]
        public string Assy_position_X7 { get; set; } = "112.514";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y7")]
        public string Assy_position_Y7 { get; set; } = "408.663";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z7")]
        public string Assy_position_Z7 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X10")]
        public string Assy_position_X10 { get; set; } = "68.521";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y10")]
        public string Assy_position_Y10 { get; set; } = "453.894";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z10")]
        public string Assy_position_Z10 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X2")]
        public string Assy_position_X2 { get; set; } = "202.206";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y2")]
        public string Assy_position_Y2 { get; set; } = "318.934";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z2")]
        public string Assy_position_Z2 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X5")]
        public string Assy_position_X5 { get; set; } = "158.995";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y5")]
        public string Assy_position_Y5 { get; set; } = "363.967";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z5")]
        public string Assy_position_Z5 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X8")]
        public string Assy_position_X8 { get; set; } = "202.206";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y8")]
        public string Assy_position_Y8 { get; set; } = "409.23";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z8")]
        public string Assy_position_Z8 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X11")]
        public string Assy_position_X11 { get; set; } = "159.195";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y11")]
        public string Assy_position_Y11 { get; set; } = "453.894";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z11")]
        public string Assy_position_Z11 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X3")]
        public string Assy_position_X3 { get; set; } = "292.307";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y3")]
        public string Assy_position_Y3 { get; set; } = "318.814";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z3")]
        public string Assy_position_Z3 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X6")]
        public string Assy_position_X6 { get; set; } = "248.661";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y6")]
        public string Assy_position_Y6 { get; set; } = "363.677";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z6")]
        public string Assy_position_Z6 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X9")]
        public string Assy_position_X9 { get; set; } = "292.058";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y9")]
        public string Assy_position_Y9 { get; set; } = "408.841";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z9")]
        public string Assy_position_Z9 { get; set; } = "26.393";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_X12")]
        public string Assy_position_X12 { get; set; } = "248.966";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Y12")]
        public string Assy_position_Y12 { get; set; } = "453.49";

        [Category("3.HIVE上传参数列表"), DisplayName("Assy position_Z12")]
        public string Assy_position_Z12 { get; set; } = "26.393";



        [Category("3.HIVE上传参数列表"), DisplayName("Tossing poaition_X")]
        public string Tossing_poaition_X { get; set; } = "36.937";

        [Category("3.HIVE上传参数列表"), DisplayName("Tossing poaition_Y")]
        public string Tossing_poaition_Y { get; set; } = "144.358";

        [Category("3.HIVE上传参数列表"), DisplayName("Tossing poaition_Z")]
        public string Tossing_poaition_Z { get; set; } = "18.981";
    }
}
