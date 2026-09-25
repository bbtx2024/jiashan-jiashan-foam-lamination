using System;
using System.ComponentModel;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.Scanner
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("扫码枪设置")]
    public class ScannerParam : IParam
    {
        [Category("0.启用"), DisplayName("模块启用")]
        public override bool BUse { get; set; } = true;

        [Category("1.连接参数")]
        [DisplayName("IP")]
        public string IP { get; set; } = "192.168.1.100";

        [Category("1.连接参数")]
        [DisplayName("端口")]
        public int Port { get; set; } = 9102;

        private int _sendTimeout = 3000;
        [Category("1.连接参数")]
        [DisplayName("写入超时")]
        [Description("ms")]
        public int SendTimeout
        {
            get => _sendTimeout;
            set
            {
                if (value < 100) value = 100;
                if (value > 10000) value = 10000;
                _sendTimeout = value;
            }
        }

        private int _receiveTimeout = 3000;
        [Category("1.连接参数")]
        [DisplayName("读取超时")]
        [Description("ms")]
        public int ReceiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                if (value < 100) value = 100;
                if (value > 10000) value = 10000;
                _receiveTimeout = value;
            }
        }

        [Category("2.扫码设定")]
        [DisplayName("触发扫码指令")]
        public string StartCommand { get; set; } = "+";

        [Category("2.扫码设定")]
        [DisplayName("终止扫码指令")]
        public string StopCommand { get; set; } = "-";

        [Category("2.扫码设定")]
        [DisplayName("启用结束标识符")]
        public bool Bechocmd { get; set; } = true;

        [Category("2.扫码设定")]
        [DisplayName("未扫到码返回值")]
        public string ErrorStr { get; set; } = "ERROR";

        public int _minlen = 1;
        [Category("2.扫码设定")]
        [DisplayName("最小长度")]
        public int Minlen
        {
            get { return _minlen; }
            set
            {
                if (value < 1) value = 1;
                if (value > 100) value = 100;
                _minlen = value;
                _maxlen = Math.Max(_minlen, _maxlen);
            }
        }

        public int _maxlen = 40;
        [Category("2.扫码设定")]
        [DisplayName("最大长度")]
        public int Maxlen
        {
            get { return _maxlen; }
            set
            {
                if (value < 1) value = 1;
                if (value > 100) value = 100;
                _maxlen = value;
                _minlen = Math.Min(_minlen, _maxlen);
            }
        }

        [Category("3.Tray")]
        [DisplayName("启用获取Tray信息")]
        public bool BUseTray { get; set; } = true;

        [Category("3.Tray")]
        [DisplayName("TrayIP")]
        public string TrayIP { get; set; } = "10.55.72.16";
    }
}
