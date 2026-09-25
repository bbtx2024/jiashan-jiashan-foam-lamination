/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-28
 * 说明：（9075主板参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using QA.Business.Interfaces;
using QA_Infrastructure;
using QA_Infrastructure.Attributes;

namespace QA.Business.Component.Motion.Robot9075
{
    public enum EN_9075OutPort
    {
        Ext1,
        Ext2,
        Ext3,
        Ext4,
        Ext5,
        Ext6,
        Ext7,
        Ext8,
        Ext9,
        Ext10,
        Ext11,
        Ext12,
        Ext13,
        Ext14,
        Ext15,
        Ext16,
        Mout1,
        Mout2,
        Mout3,
        Mout4,
    }
    public enum EN_9075Input
    {
        EI1,
        EI2,
        EI3,
        EI4,
        EI5,
        EI6,
        EI7,
        EI8,
        EI9,
        EI10,
        EI11,
        EI12,
        EI13,
        EI14,
        EI15,
        EI16,
        MIn1,
        MIn2,
        MIn3,
        MIn4,
    }
    public enum RobotAxisNum
    {
        Robot_Axis_X,
        Robot_Axis_Y,
        Robot_Axis_Z,
        Robot_Axis_R,
    }

    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("9075板卡设置")]
    public class Motion9075Param : IParam
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
        public int Baudrate { get; private set; } = 230400;
        [Category("1.连接设置"), DisplayName("板卡地址")]
        public byte Addr { get; private set; } = 0x01;

        [Category("2.启用")]
        [DisplayName("是否启用主板IO焊台报警")]
        public bool BuseTempAlarmIO { get; set; } = true;

        [Category("3.运动起点设置"), DisplayName("左上起点或右上起点,默认右上")]
        public bool MoveStartPnt { get; set; } = true;

        [Category("2.光源常亮设置"), DisplayName("启用")]
        public bool BAlwaysLight { get; set; } = false;

        [Browsable(false)]
        public int Timeout { get; set; } = 60;

        [Category("0.G命令运动超时"), DisplayName("运动超时")]
        public int GOrderTimeout
        {
            get { return Timeout; }
            set
            {
                if (value <= 1) { value = 1; }
                if (value >= 60) { value = 60; }
                Timeout = value;
            }
        }

        [Category("4.待机位置"), DisplayName("待机点"), Description("[0]代表X轴，[1]代表Y轴，[2]代表Z轴，[3]代表R轴")]
        public float[] ReadyPos { get; set; } = new float[] { 0, 0, 0, 0 };

        [Browsable(false)]
        public float[] WorkSpeedXLimit { get; set; } = new float[3] { 5, 200, 2000 };

        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("5.工作速度"), DisplayName("X工作速度"), Description("起始、速度、加速度")]
        public float[] WorkSpeedX
        {
            get { return WorkSpeedXLimit; }
            set
            {
                if (value[0] < 1) value[0] = 1;
                if (value[0] > 30) value[0] = 30;

                if (value[1] < 2) value[1] = 2;
                if (value[1] > 500) value[1] = 500;

                if (value[2] < 3) value[2] = 3;
                if (value[2] > 4000) value[2] = 4000;

                WorkSpeedXLimit = value;
            }
        }
        [Browsable(false)]
        public float[] WorkSpeedYLimit { get; set; } = new float[3] { 5, 200, 2000 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("5.工作速度"), DisplayName("Y工作速度"), Description("起始、速度、加速度")]
        public float[] WorkSpeedY
        {
            get { return WorkSpeedYLimit; }
            set
            {
                if (value[0] < 1) value[0] = 1;
                if (value[0] > 30) value[0] = 30;

                if (value[1] < 2) value[1] = 2;
                if (value[1] > 500) value[1] = 500;

                if (value[2] < 3) value[2] = 3;
                if (value[2] > 4000) value[2] = 4000;

                WorkSpeedYLimit = value;
            }
        }
        [Browsable(false)]
        public float[] WorkSpeedZLimit { get; set; } = new float[3] { 5, 200, 2000 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("5.工作速度"), DisplayName("Z工作速度"), Description("起始、速度、加速度")]
        public float[] WorkSpeedZ
        {
            get { return WorkSpeedZLimit; }
            set
            {
                if (value[0] < 1) value[0] = 1;
                if (value[0] > 30) value[0] = 30;

                if (value[1] < 2) value[1] = 2;
                if (value[1] > 500) value[1] = 500;

                if (value[2] < 3) value[2] = 3;
                if (value[2] > 4000) value[2] = 4000;

                WorkSpeedZLimit = value;
            }
        }
        [Browsable(false)]
        public float[] WorkSpeedRLimit { get; set; } = new float[3] { 5, 200, 2000 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("5.工作速度"), DisplayName("R工作速度"), Description("起始、速度、加速度")]
        public float[] WorkSpeedR
        {
            get { return WorkSpeedRLimit; }
            set
            {
                if (value[0] < 1) value[0] = 1;
                if (value[0] > 30) value[0] = 30;

                if (value[1] < 2) value[1] = 2;
                if (value[1] > 500) value[1] = 500;

                if (value[2] < 3) value[2] = 3;
                if (value[2] > 4000) value[2] = 4000;

                WorkSpeedRLimit = value;
            }
        }

        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("6.示教低速"), DisplayName("X示教低速"), Description("起始、速度、加速度")]
        public float[] LowSpeedX { get; private set; } = new float[3] { 1, 10, 50 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("6.示教低速"), DisplayName("Y示教低速"), Description("起始、速度、加速度")]
        public float[] LowSpeedY { get; private set; } = new float[3] { 1, 10, 50 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("6.示教低速"), DisplayName("Z示教低速"), Description("起始、速度、加速度")]
        public float[] LowSpeedZ { get; private set; } = new float[3] { 1, 10, 50 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("6.示教低速"), DisplayName("R示教低速"), Description("起始、速度、加速度")]
        public float[] LowSpeedR { get; private set; } = new float[3] { 1, 10, 50 };

        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("7.示教中速"), DisplayName("X示教中速"), Description("起始、速度、加速度")]
        public float[] MidSpeedX { get; private set; } = new float[3] { 5, 50, 500 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("7.示教中速"), DisplayName("Y示教中速"), Description("起始、速度、加速度")]
        public float[] MidSpeedY { get; private set; } = new float[3] { 5, 50, 500 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("7.示教中速"), DisplayName("Z示教中速"), Description("起始、速度、加速度")]
        public float[] MidSpeedZ { get; private set; } = new float[3] { 5, 50, 500 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("7.示教中速"), DisplayName("R示教中速"), Description("起始、速度、加速度")]
        public float[] MidSpeedR { get; private set; } = new float[3] { 5, 50, 500 };

        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("8.示教高速"), DisplayName("X示教高速"), Description("起始、速度、加速度")]
        public float[] HighSpeedX { get; private set; } = new float[3] { 10, 100, 1000 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("8.示教高速"), DisplayName("Y示教高速"), Description("起始、速度、加速度")]
        public float[] HighSpeedY { get; private set; } = new float[3] { 10, 100, 1000 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("8.示教高速"), DisplayName("Z示教高速"), Description("起始、速度、加速度")]
        public float[] HighSpeedZ { get; private set; } = new float[3] { 10, 100, 1000 };
        [EditorAttribute(typeof(HidePropertyViewArrayEdit), typeof(System.Drawing.Design.UITypeEditor))]
        [Category("8.示教高速"), DisplayName("R示教高速"), Description("起始、速度、加速度")]
        public float[] HighSpeedR { get; private set; } = new float[3] { 10, 100, 1000 };

        [Category("9.端口配置"), DisplayName("相机光源")]
        public EN_9075OutPort Light
        {
            get; private set;
        } = EN_9075OutPort.Ext3;

        [Category("9.端口配置"), DisplayName("出锡开关")]
        public EN_9075OutPort TinoutControl
        {
            get; private set;
        } = EN_9075OutPort.Mout1;

        [Category("9.端口配置"), DisplayName("吸烟开关")]
        public EN_9075OutPort SucSmoke
        {
            get; private set;
        } = EN_9075OutPort.Mout2;

        [Category("9.端口配置"), DisplayName("清洗吹气开关")]
        public EN_9075OutPort CleanBlow
        {
            get; private set;
        } = EN_9075OutPort.Mout3;

        [Category("9.端口配置"), DisplayName("滚轮清洗开关")]
        public EN_9075OutPort WheelClean
        {
            get; private set;
        } = EN_9075OutPort.Mout4;

        [Category("3.方向设置"), DisplayName("X反向")]
        public bool XInverse
        {
            get; set;
        } = false;
        [Category("3.方向设置"), DisplayName("Y反向")]
        public bool YInverse
        {
            get; set;
        } = false;

        public List<float[]> GetSpeeds(EN_SpeedType speed)
        {
            List<float[]> ret = new List<float[]>();
            switch (speed)
            {
                case EN_SpeedType.Low:
                    ret.Add(LowSpeedX);
                    ret.Add(LowSpeedY);
                    ret.Add(LowSpeedZ);
                    ret.Add(LowSpeedR);
                    break;
                case EN_SpeedType.Mid:
                    ret.Add(MidSpeedX);
                    ret.Add(MidSpeedY);
                    ret.Add(MidSpeedZ);
                    ret.Add(MidSpeedR);
                    break;
                case EN_SpeedType.High:
                    ret.Add(HighSpeedX);
                    ret.Add(HighSpeedY);
                    ret.Add(HighSpeedZ);
                    ret.Add(HighSpeedR);
                    break;
                case EN_SpeedType.Work:
                    ret.Add(WorkSpeedX);
                    ret.Add(WorkSpeedY);
                    ret.Add(WorkSpeedZ);
                    ret.Add(WorkSpeedR);
                    break;
            }
            return ret;
        }
        public float[] GetSpeed(EN_AxisNum axis, EN_SpeedType speed)
        {
            var speeds = GetSpeeds(speed);
            float[] ret = new float[3];
            switch (axis)
            {
                case EN_AxisNum.X:
                    ret = speeds[0];
                    break;
                case EN_AxisNum.Y:
                    ret = speeds[1];
                    break;
                case EN_AxisNum.Z:
                    ret = speeds[2];
                    break;
                case EN_AxisNum.R:
                    ret = speeds[3];
                    break;
            }
            return ret;
        }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <param name="code">错误码</param>
        /// <returns></returns>
        //public string GetErrInfo(int code)
        //{
        //    try
        //    {
        //        if (ErrDic.ContainsKey(code))
        //            return ErrDic[code];
        //    }
        //    catch (Exception ex)
        //    {
        //        // throw ex;
        //        NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
        //    }
        //    return "未知错误";
        //}

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().Name);
            sb.Append(" Port :");
            sb.Append(PortName);
            sb.Append(" Addr: ");
            sb.Append(Addr.ToString("X2"));
            return sb.ToString();
        }
    }
}
