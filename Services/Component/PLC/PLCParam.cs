/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-14
 * 说明：（PLC参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using QA.Business.Component.PLC.Attributes;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.PLC
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("PLC设置")]
    public class PLCParam : IParam
    {
        [Category("0.启用"), DisplayName("模块启用")]
        public override bool BUse { get => true; }

        [Category("1.启用")]
        [DisplayName("启用ModbusTcp")]
        [ReadOnly(true)]
        public bool BUseModbusTcp { get; set; } = true;

        [TypeConverter(typeof(SerialPortsConverter))]
        [XmlIgnore]
        [Category("2.Modbus485通讯参数")]
        [DisplayName("串口号")]
        public string ComPort { get; set; }

        [XmlIgnore]
        [Category("2.Modbus485通讯参数")]
        [DisplayName("站号")]
        [ReadOnly(true)]
        public byte StationID { get; set; } = 1;

        [Category("3.ModbusTcp通讯")]
        [DisplayName("IP")]
        //[ReadOnly(true)]
        public string IP { get; set; } = "172.16.11.100";

        [XmlIgnore]
        [Category("3.ModbusTcp通讯")]
        [DisplayName("端口")]
        //[ReadOnly(true)]
        public int Port { get; set; } = 502;

        [XmlIgnore]
        [Category("3.ModbusTcp通讯")]
        [DisplayName("缓冲区大小")]
        [Description("kb")]
        [ReadOnly(true)]
        public int BufferSize { get; set; } = 1024;

        private int _timeout = 3000;
        [XmlIgnore]
        [Category("3.ModbusTcp通讯")]
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

        #region 读通信地址
        private ushort _fromPLC_StartAddr = 2000;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-起始地址")]
        [ReadOnly(true)]
        public ushort FromPLC_StartAddr
        {
            get => _fromPLC_StartAddr;
            set => _fromPLC_StartAddr = value;
        }

        private ushort _fromPlc_AddrNum = 50;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-地址个数")]
        [ReadOnly(true)]
        public ushort FromPLC_AddrNum
        {
            get => _fromPlc_AddrNum;
            set
            {
                if (value < 1) value = 1;
                if (value > 100) value = 100;
                if (_fromPlc_AddrNum > 0)
                {
                    _fromPlc_AddrNum = value;
                }
            }
        }

        private ushort _fromPlcMachineStatus = 2000;

        [Category("4.读通讯地址")]
        [DisplayName("读PLC-机台状态地址")]
        [Description("机台启动中1、急停中2、复位中3、暂停中4")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_MachineStatus
        {
            get => _fromPlcMachineStatus;
            set
            {
                if (_fromPlcMachineStatus >= _fromPLC_StartAddr)
                {
                    _fromPlcMachineStatus = value;
                }
            }
        }

        private ushort _fromPlcRunStatus = 2001;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-控制上位机状态地址")]
        [Description("PLC控制上位机 bit0-上电 bit1—启动 bit2—复位 bit3—暂停 bit4—急停")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_RunStatus
        {
            get => _fromPlcRunStatus;
            set
            {
                if (_fromPlcRunStatus >= _fromPLC_StartAddr)
                {
                    _fromPlcRunStatus = value;
                }
            }
        }
        private ushort _fromPLC_SimulateRunSignal = 5000;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-空跑模式标记")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_SimulateRunSignal
        {
            get => _fromPLC_SimulateRunSignal;
            set
            {
                _fromPLC_SimulateRunSignal = value;
            }
        }
        private ushort _fromPLC_SimulateRunSignal2 = 5001;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-空载具强制加工标记")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_SimulateRunSignal2
        {
            get => _fromPLC_SimulateRunSignal2;
            set
            {
                _fromPLC_SimulateRunSignal2 = value;
            }
        }

        private ushort _fromPLC_NotNeedProcessModeAddr = 5002;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-流线模式标记")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_NotNeedProcessModeAddr
        {
            get => _fromPLC_NotNeedProcessModeAddr;
            set
            {
                if (_fromPLC_NotNeedProcessModeAddr >= _fromPLC_StartAddr)
                {
                    _fromPLC_NotNeedProcessModeAddr = value;
                }
            }
        }
        private ushort _fromPLC_ReadySignal = 5010;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-物料Ready触发加工标记")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_ReadySignal
        {
            get => _fromPLC_ReadySignal;
            set
            {
                if (_fromPLC_ReadySignal >= _fromPLC_StartAddr)
                {
                    _fromPLC_ReadySignal = value;
                }
            }
        }

        private ushort _fromPLC_FeederCDDCylinderReachOut = 5012;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-飞达相机气缸-飞达位置")]
        [Description("1为飞达1位置，2为飞达2位置")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_FeederCDDCylinderReachOut
        {
            get => _fromPLC_FeederCDDCylinderReachOut;
            set
            {
                if (_fromPLC_FeederCDDCylinderReachOut >= _fromPLC_StartAddr)
                {
                    _fromPLC_FeederCDDCylinderReachOut = value;
                }
            }
        }
        private ushort _fromPLC_FeederTypeStatus = 5013;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-飞达1物料检测结果")]
        [Description("0无料，1有料")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_FeederTypeStatus
        {
            get => _fromPLC_FeederTypeStatus;
            set
            {
                _fromPLC_FeederTypeStatus = value;
            }
        }
        private ushort _fromPLC_FeederInPlaceStatus = 5014;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-飞达1在位检测结果")]
        [Description("0不在位，1在位")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_FeederInPlaceStatus
        {
            get => _fromPLC_FeederInPlaceStatus;
            set
            {
                _fromPLC_FeederInPlaceStatus = value;
            }
        }
        private ushort _fromPLC_Feeder2TypeStatus = 5015;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-飞达2物料检测结果")]
        [Description("0无料，1有料")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_Feeder2TypeStatus
        {
            get => _fromPLC_Feeder2TypeStatus;
            set
            {
                _fromPLC_Feeder2TypeStatus = value;
            }
        }
        private ushort _fromPLC_Feeder2InPlaceStatus = 5016;
        [Category("4.读通讯地址")]
        [DisplayName("读PLC-飞达1在位检测结果")]
        [Description("0不在位，1在位")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_Feeder2InPlaceStatus
        {
            get => _fromPLC_Feeder2InPlaceStatus;
            set
            {
                _fromPLC_Feeder2InPlaceStatus = value;
            }
        }
        private ushort _fromPLC_FeederGetTape = 5513;
        [Category("4.读通讯地址")]
        [DisplayName("写PLC-飞达1送料信号")]
        [Description("送料由0变1表示触发送料")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_FeederGetTape
        {
            get => _fromPLC_FeederGetTape;
            set
            {
                if (_fromPLC_FeederGetTape >= _fromPLC_StartAddr)
                {
                    _fromPLC_FeederGetTape = value;
                }
            }
        }
        private ushort _fromPLC_Feeder2GetTape = 5515;
        [Category("4.读通讯地址")]
        [DisplayName("写PLC-飞达送料信号")]
        [Description("送料由0变1表示触发送料")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort FromPLC_Feeder2GetTape
        {
            get => _fromPLC_Feeder2GetTape;
            set
            {
                if (_fromPLC_Feeder2GetTape >= _fromPLC_StartAddr)
                {
                    _fromPLC_Feeder2GetTape = value;
                }
            }
        }

        //private ushort _fromPLC_SafeDoorAlarmAddr = 5004;
        //[Category("4.读通讯地址")]
        //[DisplayName("读PLC-安全门报警地址")]
        //[Description("0:正常 1:报警")]
        //[TriggerHandler(true)]
        //[ReadOnly(true)]
        //public ushort FromPLC_SafeDoorAlarmAddr
        //{
        //    get => _fromPLC_SafeDoorAlarmAddr;
        //    set
        //    {
        //        if (_fromPLC_SafeDoorAlarmAddr >= _fromPLC_StartAddr)
        //        {
        //            _fromPLC_SafeDoorAlarmAddr = value;
        //        }
        //    }
        //}

        //private ushort _fromPLC_AlarmAddr = 5005;
        //[Category("4.读通讯地址")]
        //[DisplayName("读PLC-报警地址")]
        //[Description("bit0: 待料处卡料, bit1:焊接处卡料, bit4：前阻挡气缸未复位；bit5:前阻挡气缸未到位；bit6:底上抬气缸未复位;bit7: " +
        //            "底上抬气缸未到位;bit8:后阻挡气缸未复位;bit9:后阻挡气缸未到位;bit10:气压报警;bit15:上位机报警")]
        //[TriggerHandler(true)]
        //[ReadOnly(true)]
        //public ushort FromPLC_AlarmAddr
        //{
        //    get => _fromPLC_AlarmAddr;
        //    set
        //    {
        //        if (_fromPLC_AlarmAddr >= _fromPLC_StartAddr)
        //        {
        //            _fromPLC_AlarmAddr = value;
        //        }
        //    }
        //}
        #endregion

        #region 写通信地址
        private ushort _toPLC_StartAddr = 3000;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-起始地址")]
        [ReadOnly(true)]
        public ushort ToPLC_StartAddr
        {
            get => _toPLC_StartAddr;
            set => _toPLC_StartAddr = value;
        }

        [Category("4.写通讯地址")]
        [DisplayName("写PLC-地址个数")]
        [ReadOnly(true)]
        public ushort ToPLC_AddrNum { get; set; } = 30;

        private ushort _toPLC_RunStausAddr = 3000;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-工位运行状态地址")]
        [Description("1.运行 2.急停 3.复位 4.暂停 5.复位完成 6.工作完成 9. 空闲中")]
        [ReadOnly(true)]
        public ushort ToPLC_RunStausAddr
        {
            get => _toPLC_RunStausAddr;
            set
            {
                if (_toPLC_RunStausAddr >= _toPLC_StartAddr)
                {
                    _toPLC_RunStausAddr = value;
                }
            }
        }
        private ushort _toPLC_PlcButton = 3001;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-控制PLC按钮")]
        [Description("1:控制PLC启动 2:控制PLC暂停 3:控制PLC复位")]
        [FloatAddress(true)]
        [ReadOnly(true)]
        public ushort ToPLC_PlcButton
        {
            get => _toPLC_PlcButton;
            set
            {
                if (_toPLC_PlcButton >= _toPLC_StartAddr)
                {
                    _toPLC_PlcButton = value;
                }
            }
        }
        private ushort _toPLC_HeartBeat = 3002;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-心跳信号")]
        [Description("1:心跳正常")]
        [FloatAddress(true)]
        [ReadOnly(true)]
        public ushort ToPLC_HeartBeat
        {
            get => _toPLC_HeartBeat;
            set
            {
                if (_toPLC_HeartBeat >= _toPLC_StartAddr)
                {
                    _toPLC_HeartBeat = value;
                }
            }
        }
        //private ushort _toPLC_AlarmAddr = 3001;
        //[Category("4.写通讯地址")]
        //[DisplayName("写PLC-报警地址")]
        //[Description("0:正常 1:闪烁 2:闪烁加蜂鸣器")]
        //[ReadOnly(true)]
        //public ushort ToPLC_AlarmAddr
        //{
        //    get => _toPLC_AlarmAddr;
        //    set
        //    {
        //        if (_toPLC_AlarmAddr >= _toPLC_StartAddr)
        //        {
        //            _toPLC_AlarmAddr = value;
        //        }
        //    }
        //}
        //private ushort _toPLC_SuctionFilmAddr = 5501;
        //[Category("4.写通讯地址")]
        //[DisplayName("写PLC-吸废膜")]
        //[Description("1:开 0:关")]
        //[FloatAddress(true)]
        //[ReadOnly(true)]
        //public ushort ToPLC_SuctionFilmAddr
        //{
        //    get => _toPLC_SuctionFilmAddr;
        //    set
        //    {
        //        _toPLC_SuctionFilmAddr = value;
        //    }
        //}


        private ushort _toPLC_SuctionFilmAddr = 5501;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-吸废膜")]
        [Description("1:开 0:关")]
        [FloatAddress(true)]
        [ReadOnly(true)]
        public ushort ToPLC_SuctionFilmAddr
        {
            get => _toPLC_SuctionFilmAddr;
            set
            {
                _toPLC_SuctionFilmAddr = value;
            }
        }

        private ushort _toPLC_WorkResultAddr = 5510;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-工作结果")]
        [Description("1:OK 2:NG")]
        [FloatAddress(true)]
        [ReadOnly(true)]
        public ushort ToPLC_WorkResultAddr
        {
            get => _toPLC_WorkResultAddr;
            set
            {
                _toPLC_WorkResultAddr = value;
            }
        }


        private ushort _toPLC_NeedDwellAddr = 5511;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-是否需要保压")]
        [Description("x=cav-1 bitx=0:OK bitx=1:NG")]
        [FloatAddress(true)]
        [ReadOnly(true)]
        public ushort ToPLC_NeedDwellAddr
        {
            get => _toPLC_NeedDwellAddr;
            set
            {
                _toPLC_NeedDwellAddr = value;
            }
        }
        private ushort _toPLC_FeederCDDCylinderReachOut = 5512;
        [Category("4.写通讯地址")]
        [DisplayName("写PLC-飞达相机气缸-飞达位置")]
        [Description("1飞达1位置，2飞达2位置")]
        [TriggerHandler(true)]
        [ReadOnly(true)]
        public ushort ToPLC_FeederCDDCylinderReachOut
        {
            get => _toPLC_FeederCDDCylinderReachOut;
            set
            {
                _toPLC_FeederCDDCylinderReachOut = value;
            }
        }

        //private ushort _toPLC_SwReady = 5125;
        //[Category("4.写通讯地址")]
        //[DisplayName("写PLC-上位机准备完成")]
        //[Description("0:未准备好 1:准备好")]
        //[FloatAddress(true)]
        //[ReadOnly(true)]
        //public ushort ToPLC_SwReady
        //{
        //    get => _toPLC_SwReady;
        //    set
        //    {
        //        if (_toPLC_SwReady >= _toPLC_StartAddr)
        //        {
        //            _toPLC_SwReady = value;
        //        }
        //    }
        //}


        #endregion
    }
}
