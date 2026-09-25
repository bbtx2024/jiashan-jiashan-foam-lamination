/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-11
 * 说明：（GT2测高仪参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using System.IO.Ports;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.GT2
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("GT2设置")]
    public class GT2Param : IParam
    {
        [TypeConverter(typeof(SerialPortsConverter))]
        [Browsable(true)]
        [Category("1.连接设置"), DisplayName("串口号")]
        public string PortName { get; set; }

        [Browsable(true), ReadOnly(false)]
        [Category("1.连接设置"), DisplayName("波特率")]
        public int Baudrate { get; set; } = 115200;

        [Browsable(true), ReadOnly(false)]
        [Category("1.连接设置"), DisplayName("数据位")]
        public int DataBit { get; set; } = 8;

        [Browsable(true), ReadOnly(false)]
        [Category("1.连接设置"), DisplayName("停止位")]
        public StopBits StopBits { get; set; } = StopBits.One;

        [Browsable(true), ReadOnly(false)]
        [Category("1.连接设置"), DisplayName("校验位")]
        public Parity Parity { get; set; } = Parity.None;

        [Browsable(true), ReadOnly(false)]
        [Category("1.连接设置"), DisplayName("超时时长")]
        public int TimeOut { get; set; } = 500;

        [Browsable(true), ReadOnly(false)]
        [Category("2.读取设置"), DisplayName("起始位")]
        public int StartAddress { get; set; } = 1;

        [Browsable(true), ReadOnly(false)]
        [Category("2.读取设置"), DisplayName("长度")]
        public int ReadLength { get; set; } = 12;

    }
}
