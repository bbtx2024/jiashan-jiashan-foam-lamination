/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：luanliang
 * 创建日期：2022-04-28
 * 说明：（螺丝电批参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.ScrewDriver
{
    public enum ScrewMode
    {
        TorqueMode,
        PositionMode,
    }

    public enum ScrewAddr
    {
        SetSpeedAddr = 2100,
        SetAddSpeedAddr,
        SetSubSpeedAddr,
        SetCvrveAddr,
        ModeAddr,
        SetHomeOffsetAddr,
        SetNormalSpeedAddr,
        SetSpotDistanceAddr,
        SetTorqueAddr,
        SetTorqueHighSpeedAddr,
        SetTorqueMiddleSpeedAddr,
        SetTorqueLowerSpeedAddr,
        SetTorqueAddSpeedAddr,
        SetTorqueCompareValue1Addr,
        SetTorqueCompareValue2Addr,
        SetDwellTimeAddr,
        SetReverseAngleAddr,
    }
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("螺丝电批设置")]
    public class ScrewDriverParam : IParam
    {
        [Category("0.启用")]
        [DisplayName("是否启用ModbusTcp")]
        [ReadOnly(true)]
        public bool BUseModbusTcp { get; set; } = true;

        [TypeConverter(typeof(SerialPortsConverter))]
        [XmlIgnore]
        [Category("1.Modbus485通讯参数")]
        [DisplayName("串口号")]
        public string ComPort { get; set; }

        [XmlIgnore]
        [Category("1.Modbus485通讯参数")]
        [DisplayName("站号")]
        [ReadOnly(true)]
        public byte StationId { get; set; } = 1;

        [Category("2.ModbusTcp通讯")]
        [DisplayName("IP")]
        //[ReadOnly(true)]
        public string Ip { get; set; } = "127.0.0.1";

        [XmlIgnore]
        [Category("2.ModbusTcp通讯")]
        [DisplayName("端口")]
        //[ReadOnly(true)]
        public int Port { get; set; } = 502;

        [XmlIgnore]
        [Category("2.ModbusTcp通讯")]
        [DisplayName("缓冲区大小")]
        [Description("kb")]
        [ReadOnly(true)]
        public int BufferSize { get; set; } = 1024;

        private int _timeout = 3000;
        [XmlIgnore]
        [Category("2.ModbusTcp通讯")]
        [DisplayName("超时")]
        [Description("ms")]
        [ReadOnly(true)]
        public int Timeout
        {
            get => _timeout;
            set
            {
                if (value <= 3000)
                {
                    _timeout = value;
                }
            }
        }

        private int _setSpeed = 10;
        [XmlIgnore]
        [Category("3.参数设置")]
        [DisplayName("速度设置")]
        [Description("degree/s")]
        [ReadOnly(true)]
        public int SetSpeed
        {
            get => _setSpeed;
            set
            {
                if (value <= 100)
                {
                    _setSpeed = value;
                }
            }
        }

        private int _setAddSpeed = 10;
        [XmlIgnore]
        [Category("3.参数设置")]
        [DisplayName("加速度设置")]
        [Description("degree/s²")]
        [ReadOnly(true)]
        public int SetAddSpeed
        {
            get => _setAddSpeed;
            set
            {
                if (value <= 100)
                {
                    _setAddSpeed = value;
                }
            }
        }

        private int _setSubSpeed = 10;
        [XmlIgnore]
        [Category("3.参数设置")]
        [DisplayName("减速度设置")]
        [Description("degree/s²")]
        [ReadOnly(true)]
        public int SetSubSpeed
        {
            get => _setSubSpeed;
            set
            {
                if (value <= 100)
                {
                    _setSubSpeed = value;
                }
            }
        }

        [Category("3.参数设置")]
        [DisplayName("是否设置曲线")]
        //[ReadOnly(true)]
        public bool IsSetCvrve { get; set; } = false;

        [Category("3.参数设置")]
        [DisplayName("设置模式")]
        public ScrewMode Mode { get; set; } = ScrewMode.TorqueMode;

        private float _setHomeOffset = 10;
        [Category("3.参数设置")]
        [DisplayName("设置回零偏移")]
        public float SetHomeOffset
        {
            get => _setHomeOffset;
            set
            {
                if (value <= 100)
                {
                    _setHomeOffset = value;
                }
            }
        }

        private float _setNormalSpeed = 10;
        [Category("3.参数设置")]
        [DisplayName("设置手动速度")]
        public float SetNormalSpeed
        {
            get => _setNormalSpeed;
            set
            {
                if (value <= 100)
                {
                    _setNormalSpeed = value;
                }
            }
        }

        private float _setSpotDistance = 10;
        [Category("3.参数设置")]
        [DisplayName("设置点动距离")]
        public float SetSpotDistance
        {
            get => _setSpotDistance;
            set
            {
                if (value <= 100)
                {
                    _setSpotDistance = value;
                }
            }
        }

        private float _setTorque = 10;
        [Category("3.参数设置")]
        [DisplayName("设置扭矩")]
        public float SetTorque
        {
            get => _setTorque;
            set
            {
                if (value <= 100)
                {
                    _setTorque = value;
                }
            }
        }

        private float _setTorqueHighSpeed = 10;
        [Category("3.参数设置")]
        [DisplayName("设置扭矩高速")]
        public float SetTorqueHighSpeed
        {
            get => _setTorqueHighSpeed;
            set
            {
                if (value <= 100)
                {
                    _setTorqueHighSpeed = value;
                }
            }
        }
        private float _setTorqueMiddleSpeed = 10;
        [Category("3.参数设置")]
        [DisplayName("设置扭矩中速")]
        public float SetTorqueMiddleSpeed
        {
            get => _setTorqueMiddleSpeed;
            set
            {
                if (value <= 100)
                {
                    _setTorqueMiddleSpeed = value;
                }
            }
        }

        private float _setTorqueLowerSpeed = 10;
        [Category("3.参数设置")]
        [DisplayName("设置扭矩低速")]
        public float SetTorqueLowerSpeed
        {
            get => _setTorqueLowerSpeed;
            set
            {
                if (value <= 100)
                {
                    _setTorqueLowerSpeed = value;
                }
            }
        }

        private float _setTorqueAddSpeed = 10;
        [Category("3.参数设置")]
        [DisplayName("设置扭矩加速度")]
        public float SetTorqueAddSpeed
        {
            get => _setTorqueAddSpeed;
            set
            {
                if (value <= 100)
                {
                    _setTorqueAddSpeed = value;
                }
            }
        }

        private float _setTorqueCompareValue1 = 10;
        [Category("3.参数设置")]
        [DisplayName("设置扭力比较1")]
        public float SetTorqueCompareValue1
        {
            get => _setTorqueCompareValue1;
            set
            {
                if (value <= 100)
                {
                    _setTorqueCompareValue1 = value;
                }
            }
        }

        private float _setTorqueCompareValue2 = 10;
        [Category("3.参数设置")]
        [DisplayName("设置扭力比较2")]
        public float SetTorqueCompareValue2
        {
            get => _setTorqueCompareValue2;
            set
            {
                if (value <= 100)
                {
                    _setTorqueCompareValue2 = value;
                }
            }
        }

        private float _setDwellTime = 10;
        [Category("3.参数设置")]
        [DisplayName("设置保压时间")]
        public float SetDwellTime
        {
            get => _setDwellTime;
            set
            {
                if (value <= 100)
                {
                    _setDwellTime = value;
                }
            }
        }

        private float _setReverseAngle = 10;
        [Category("3.参数设置")]
        [DisplayName("设置保压时间")]
        public float SetReverseAngle
        {
            get => _setReverseAngle;
            set
            {
                if (value <= 100)
                {
                    _setReverseAngle = value;
                }
            }
        }



        #region 写通信地址       

        //private ushort _toPLC_WorkResultAddrRight = 2019;
        //[Category("4.写通讯地址")]
        //[DisplayName("写PLC-右工作结果")]
        //[Description("1:OK 2:NG")]
        //[FloatAddress(true)]
        //[ReadOnly(true)]
        //public ushort ToPLC_WorkResultAddrRight
        //{
        //    get => _toPLC_WorkResultAddrRight;
        //    set
        //    {
        //        if (_toPLC_WorkResultAddrRight >= _toPLC_StartAddr)
        //        {
        //            _toPLC_WorkResultAddrRight = value;
        //        }
        //    }
        //}

        #endregion

        #region 读通信地址
        //private ushort _fromPLC_StartAddr = 2050;
        //[Category("4.读通讯地址")]
        //[DisplayName("读PLC-起始地址")]
        //[ReadOnly(true)]
        //public ushort FromPLC_StartAddr
        //{
        //    get => _fromPLC_StartAddr;
        //    set => _fromPLC_StartAddr = value;
        //}

        //private ushort _fromPlcRunStatus = 2051;
        //[Category("4.读通讯地址")]
        //[Description("PLC控制上位机 bit0-上电 bit1—启动 bit2—复位 bit3—暂停 bit4—急停")]
        //[TriggerHandler(true)]
        //[ReadOnly(true)]
        //public ushort FromPLC_RunStatus
        //{
        //    get => _fromPlcRunStatus;
        //    set
        //    {
        //        if (_fromPlcRunStatus >= _fromPLC_StartAddr)
        //        {
        //            _fromPlcRunStatus = value;
        //        }
        //    }
        //}       
        #endregion
    }
}
