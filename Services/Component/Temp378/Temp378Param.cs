using System;
using System.ComponentModel;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.Temp378
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("378温控设置")]
    public class Temp378Param : IParam
    {
        private string portName = string.Empty;
        [TypeConverter(typeof(SerialPortsConverter))]
        [Browsable(true), ReadOnly(false)]
        [Category("1.连接设置"), DisplayName("串口号")]
        public string PortName
        {
            get { return portName; }
            set
            {
                portName = value;
            }
        }

        [Browsable(true), ReadOnly(false)]
        [Category("1.连接设置"), DisplayName("波特率")]
        public int Baudrate { get; private set; } = 115200;

        [Browsable(false)]
        private ushort _setTempLimit { get; set; } = 50;
        [Category("2.设置")]
        [DisplayName("设置温度")]
        public ushort SetTemp
        {
            get => _setTempLimit;
            set
            {
                if (value < 50) value = 50;
                if (value >= 450) value = 450;
                _setTempLimit = value;
            }
        }

        [Browsable(false)]
        public ushort TempOffsetLimit { get; set; } = 10;
        [Category("2.设置")]
        [DisplayName("允许温度偏差")]
        public ushort TempOffset
        {
            get
            {
                return TempOffsetLimit;
            }
            set
            {
                if (value < 1) value = 1;
                if (value >= 15) value = 15;
                TempTimeoutLimit = value;
            }
        }

        [Browsable(false)]
        public ushort TempTimeoutLimit { get; set; } = 10;
        [Category("2.设置")]
        [DisplayName("升温超时S")]
        public ushort TempTimeout
        {
            get
            {
                return TempTimeoutLimit;
            }
            set
            {
                if (value < 1) value = 1;
                if (value >= 10) value = 10;
                TempTimeoutLimit = value;
            }
        }
    }
}
