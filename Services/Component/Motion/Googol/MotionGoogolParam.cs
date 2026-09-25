/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-24
 * 说明：（固高运动控制参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reflection;
using QA.Business.Helper;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA_Infrastructure;
using QA_Infrastructure.Attributes;

namespace QA.Business.Component.Motion.Googol
{
    public enum EN_GoogolExtendOutput
    {
        Out0_0,
        Out0_1,
        Out0_2,
        Out0_3,
        Out0_4,
        Out0_5,
        Out0_6,
        Out0_7,
        Out0_8,
        Out0_9,
        Out0_10,
        Out0_11,
        Out0_12,
        Out0_13,
        Out0_14,
        Out0_15,
        Out1_0,
        Out1_1,
        Out1_2,
        Out1_3,
        Out1_4,
        Out1_5,
        Out1_6,
        Out1_7,
        Out1_8,
        Out1_9,
        Out1_10,
        Out1_11,
        Out1_12,
        Out1_13,
        Out1_14,
        Out1_15,
    }

    public enum EN_GoogolExtendInput
    {
        In0_0,
        In0_1,
        In0_2,
        In0_3,
        In0_4,
        In0_5,
        In0_6,
        In0_7,
        In0_8,
        In0_9,
        In0_10,
        In0_11,
        In0_12,
        In0_13,
        In0_14,
        In0_15,
        In1_0,
        In1_1,
        In1_2,
        In1_3,
        In1_4,
        In1_5,
        In1_6,
        In1_7,
        In1_8,
        In1_9,
        In1_10,
        In1_11,
        In1_12,
        In1_13,
        In1_14,
        In1_15,
    }

    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("固高板卡设置")]
    public class MotionGoogolParam : IParam
    {
        public override void SetBrowsableAndReadOnly(EN_UserType userType)
        {
            base.SetBrowsableAndReadOnly(userType);
            if (!IsCylinderDoubleSwitch)
            {
                PropAttrUtils.SetBrowsable(this, () => OutCylinderUpNo1, false);
                PropAttrUtils.SetBrowsable(this, () => OutCylinderUpNo2, false);
                PropAttrUtils.SetBrowsable(this, () => OutCylinderUpNo3, false);
                PropAttrUtils.SetBrowsable(this, () => OutCylinderUpNo4, false);
                PropAttrUtils.SetBrowsable(this, () => InCylinderDownReadyNo1, false);
                PropAttrUtils.SetBrowsable(this, () => InCylinderDownReadyNo2, false);
                PropAttrUtils.SetBrowsable(this, () => InCylinderDownReadyNo3, false);
                PropAttrUtils.SetBrowsable(this, () => InCylinderDownReadyNo4, false);
            }
        }

        [Category("0.启用"), DisplayName("模块启用")]
        public override bool BUse { get => true; }

        #region 脉冲当量

        public readonly float[] PluseEquivalents = new float[8]
        {
            1280, 1280,
            1280, 1280, 2560,
            138.89f, 138.89f, 138.89f, 
            //138.89f,
            //138.89f
        };

        //private float _PluseEquivalentX1 { get; set; } = 1280;
        [Category("1.脉冲当量"), DisplayName("X1脉冲当量"), Description("每个轴的脉冲当量")]
        public float PluseEquivalentX1
        {
            get { return PluseEquivalents[(int)En_AxisNum.X1]; }
            //private set
            //{
            //    if (value < 1) value = 1;
            //    else if (value > 3500) value = 3500;
            //    PluseEquivalents[(int)En_AxisNum.X1] = value;
            //}
        }

        //private float _PluseEquivalentY1 { get; set; } = 1280;
        [Category("1.脉冲当量"), DisplayName("Y1脉冲当量"), Description("每个轴的脉冲当量")]
        public float PluseEquivalentY1
        {
            get { return PluseEquivalents[(int)En_AxisNum.Y1]; }
            //private set
            //{
            //    if (value < 1) value = 1;
            //    else if (value > 3500) value = 3500;
            //    PluseEquivalents[(int)En_AxisNum.Y1] = value;
            //}
        }

        //private float _PluseEquivalentX2 { get; set; } = 1280;
        [Category("1.脉冲当量"), DisplayName("X2脉冲当量"), Description("每个轴的脉冲当量")]
        public float PluseEquivalentX2
        {
            get { return PluseEquivalents[(int)En_AxisNum.X2]; }
            //private set
            //{
            //    if (value < 1) value = 1;
            //    else if (value > 3500) value = 3500;
            //    PluseEquivalents[(int)En_AxisNum.X2] = value;
            //}
        }

        //private float _PluseEquivalentY2 { get; set; } = 1280;
        [Category("1.脉冲当量"), DisplayName("Y2脉冲当量"), Description("每个轴的脉冲当量")]
        public float PluseEquivalentY2
        {
            get { return PluseEquivalents[(int)En_AxisNum.Y2]; }
            //private set
            //{
            //    if (value < 1) value = 1;
            //    else if (value > 3500) value = 3500;
            //    PluseEquivalents[(int)En_AxisNum.Y2] = value;
            //}
        }

        //private float _PluseEquivalentZ2 { get; set; } = 2560;
        [Category("1.脉冲当量"), DisplayName("Z2脉冲当量"), Description("每个轴的脉冲当量")]
        public float PluseEquivalentZ2
        {
            get { return PluseEquivalents[(int)En_AxisNum.Z2]; }
            //set
            //{
            //    if (value < 1) value = 1;
            //    else if (value > 600) value = 600;
            //    PluseEquivalents[(int)En_AxisNum.Z2] = value;
            //}
        }

        //private float _PluseEquivalentR1 { get; set; } = 138.89f;
        [Category("1.脉冲当量"), DisplayName("R1脉冲当量"), Description("每个轴的脉冲当量")]
        public float PluseEquivalentR1
        {
            get { return PluseEquivalents[(int)En_AxisNum.R1]; }
            //set
            //{
            //    if (value < 1) value = 1;
            //    else if (value > 600) value = 600;
            //    PluseEquivalents[(int)En_AxisNum.R1] = value;
            //}
        }

        //private float _PluseEquivalentR2 { get; set; } = 138.89f;
        [Category("1.脉冲当量"), DisplayName("R2脉冲当量"), Description("每个轴的脉冲当量")]
        public float PluseEquivalentR2
        {
            get { return PluseEquivalents[(int)En_AxisNum.R2]; }
            //set
            //{
            //    if (value < 1) value = 1;
            //    else if (value > 600) value = 600;
            //    PluseEquivalents[(int)En_AxisNum.R2] = value;
            //}
        }

        //private float _PluseEquivalentR3 { get; set; } = 138.89f;
        //[Category("1.脉冲当量"), DisplayName("R3脉冲当量"), Description("每个轴的脉冲当量")]
        //public float PluseEquivalentR3
        //{
        //    get { return PluseEquivalents[(int)En_AxisNum.R3]; }
        //    //set
        //    //{
        //    //    if (value < 1) value = 1;
        //    //    else if (value > 600) value = 600;
        //    //    PluseEquivalents[(int)En_AxisNum.R3] = value;
        //    //}
        //}

        //private float _PluseEquivalentR4 { get; set; } = 138.89f;
        //[Category("1.脉冲当量"), DisplayName("R4脉冲当量"), Description("每个轴的脉冲当量")]
        //public float PluseEquivalentR4
        //{
        //    get { return PluseEquivalents[(int)En_AxisNum.R4]; }
        //    //set
        //    //{
        //    //    if (value < 1) value = 1;
        //    //    else if (value > 600) value = 600;
        //    //    PluseEquivalents[(int)En_AxisNum.R4] = value;
        //    //}
        //}

        #endregion

        #region 速度设置

        #region 工作速度

        private float[] _workSpeedX1 = new float[3] { 10, 200, 2000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("2.工作速度"), DisplayName("X1工作速度"), Description("起始速度、最大速度、加速度")]
        public float[] WorkSpeedX1
        {
            get => _workSpeedX1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _workSpeedX1 = value;
            }
        }

        private float[] _workSpeedY1 = new float[3] { 10, 200, 2000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("2.工作速度"), DisplayName("Y1工作速度"), Description("起始速度、最大速度、加速度")]
        public float[] WorkSpeedY1
        {
            get => _workSpeedY1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _workSpeedY1 = value;
            }
        }

        private float[] _workSpeedX2 = new float[3] { 10, 200, 2000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("2.工作速度"), DisplayName("X2工作速度"), Description("起始速度、最大速度、加速度")]
        public float[] WorkSpeedX2
        {
            get => _workSpeedX2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _workSpeedX2 = value;
            }
        }

        private float[] _workSpeedY2 = new float[3] { 10, 200, 2000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("2.工作速度"), DisplayName("Y2工作速度"), Description("起始速度、最大速度、加速度")]
        public float[] WorkSpeedY2
        {
            get => _workSpeedY2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _workSpeedY2 = value;
            }
        }

        private float[] _workSpeedZ2 = new float[3] { 10, 100, 1500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("2.工作速度"), DisplayName("Z2工作速度"), Description("起始速度、最大速度、加速度")]
        public float[] WorkSpeedZ2
        {
            get => _workSpeedZ2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _workSpeedZ2 = value;
            }
        }

        private float[] _workSpeedR1 = new float[3] { 10, 200, 2000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("2.工作速度"), DisplayName("R1工作速度"), Description("起始速度、最大速度、加速度")]
        public float[] WorkSpeedR1
        {
            get => _workSpeedR1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _workSpeedR1 = value;
            }
        }

        private float[] _workSpeedR2 = new float[3] { 10, 200, 2000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("2.工作速度"), DisplayName("R2工作速度"), Description("起始速度、最大速度、加速度")]
        public float[] WorkSpeedR2
        {
            get => _workSpeedR2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _workSpeedR2 = value;
            }
        }

        //private float[] _workSpeedR3 = new float[3] { 10, 200, 2000 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("2.工作速度"), DisplayName("R3工作速度"), Description("起始速度、最大速度、加速度")]
        //public float[] WorkSpeedR3
        //{
        //    get => _workSpeedR3;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _workSpeedR3 = value;
        //    }
        //}

        //private float[] _workSpeedR4 = new float[3] { 10, 200, 2000 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("2.工作速度"), DisplayName("R4工作速度"), Description("起始速度、最大速度、加速度")]
        //public float[] WorkSpeedR4
        //{
        //    get => _workSpeedR4;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _workSpeedR4 = value;
        //    }
        //}

        #endregion

        #region 复位速度

        private float[] _resetSpeedX1 = new float[3] { 20, 5, 20 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("3.复位速度"), DisplayName("X1复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        public float[] ResetSpeedX1
        {
            get => _resetSpeedX1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 100) value[0] = 100;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 30) value[1] = 30;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 1000) value[2] = 1000;
                _resetSpeedX1 = value;
            }
        }

        private float[] _resetSpeedY1 = new float[3] { 20, 5, 20 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("3.复位速度"), DisplayName("Y1复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        public float[] ResetSpeedY1
        {
            get => _resetSpeedY1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 100) value[0] = 100;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 30) value[1] = 30;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 1000) value[2] = 1000;
                _resetSpeedY1 = value;
            }
        }

        private float[] _resetSpeedX2 = new float[3] { 20, 5, 20 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("3.复位速度"), DisplayName("X2复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        public float[] ResetSpeedX2
        {
            get => _resetSpeedX2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 100) value[0] = 100;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 30) value[1] = 30;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 1000) value[2] = 1000;
                _resetSpeedX2 = value;
            }
        }

        private float[] _resetSpeedY2 = new float[3] { 20, 5, 20 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("3.复位速度"), DisplayName("Y2复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        public float[] ResetSpeedY2
        {
            get => _resetSpeedY2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 100) value[0] = 100;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 30) value[1] = 30;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 1000) value[2] = 1000;
                _resetSpeedY2 = value;
            }
        }

        private float[] _resetSpeedZ2 = new float[3] { 20, 5, 20 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("3.复位速度"), DisplayName("Z2复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        public float[] ResetSpeedZ2
        {
            get => _resetSpeedZ2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 100) value[0] = 100;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 30) value[1] = 30;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 1000) value[2] = 1000;
                _resetSpeedZ2 = value;
            }
        }

        private float[] _resetSpeedR1 = new float[3] { 30, 10, 100 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("3.复位速度"), DisplayName("R1复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        public float[] ResetSpeedR1
        {
            get => _resetSpeedR1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 100) value[0] = 100;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 30) value[1] = 30;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 1000) value[2] = 1000;
                _resetSpeedR1 = value;
            }
        }

        private float[] _resetSpeedR2 = new float[3] { 30, 10, 100 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("3.复位速度"), DisplayName("R2复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        public float[] ResetSpeedR2
        {
            get => _resetSpeedR2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 100) value[0] = 100;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 30) value[1] = 30;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 1000) value[2] = 1000;
                _resetSpeedR2 = value;
            }
        }

        //private float[] _resetSpeedR3 = new float[3] { 30, 10, 100 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("3.复位速度"), DisplayName("R3复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        //public float[] ResetSpeedR3
        //{
        //    get => _resetSpeedR3;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 100) value[0] = 100;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 30) value[1] = 30;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 1000) value[2] = 1000;
        //        _resetSpeedR3 = value;
        //    }
        //}

        //private float[] _resetSpeedR4 = new float[3] { 30, 10, 100 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("3.复位速度"), DisplayName("R4复位速度"), Description("搜索开关速度、搜索index标识速度、搜索加速度")]
        //public float[] ResetSpeedR4
        //{
        //    get => _resetSpeedR4;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 100) value[0] = 100;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 30) value[1] = 30;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 1000) value[2] = 1000;
        //        _resetSpeedR4 = value;
        //    }
        //}

        #endregion

        #region 示教低速

        private float[] _lowSpeedX1 = new float[3] { 0, 10, 50 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("4.示教低速"), DisplayName("X1示教低速"), Description("起始速度、最大速度、加速度")]
        public float[] LowSpeedX1
        {
            get => _lowSpeedX1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _lowSpeedX1 = value;
            }
        }

        private float[] _lowSpeedY1 = new float[3] { 0, 10, 50 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("4.示教低速"), DisplayName("Y1示教低速"), Description("起始速度、最大速度、加速度")]
        public float[] LowSpeedY1
        {
            get => _lowSpeedY1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _lowSpeedY1 = value;
            }
        }

        private float[] _lowSpeedX2 = new float[3] { 0, 10, 50 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("4.示教低速"), DisplayName("X2示教低速"), Description("起始速度、最大速度、加速度")]
        public float[] LowSpeedX2
        {
            get => _lowSpeedX2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _lowSpeedX2 = value;
            }
        }

        private float[] _lowSpeedY2 = new float[3] { 0, 10, 50 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("4.示教低速"), DisplayName("Y2示教低速"), Description("起始速度、最大速度、加速度")]
        public float[] LowSpeedY2
        {
            get => _lowSpeedY2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _lowSpeedY2 = value;
            }
        }

        private float[] _lowSpeedZ2 = new float[3] { 0, 10, 50 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("4.示教低速"), DisplayName("Z2示教低速"), Description("起始速度、最大速度、加速度")]
        public float[] LowSpeedZ2
        {
            get => _lowSpeedZ2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _lowSpeedZ2 = value;
            }
        }

        private float[] _lowSpeedR1 = new float[3] { 0, 30, 100 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("4.示教低速"), DisplayName("R1示教低速"), Description("起始速度、最大速度、加速度")]
        public float[] LowSpeedR1
        {
            get => _lowSpeedR1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _lowSpeedR1 = value;
            }
        }

        private float[] _lowSpeedR2 = new float[3] { 0, 30, 100 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("4.示教低速"), DisplayName("R2示教低速"), Description("起始速度、最大速度、加速度")]
        public float[] LowSpeedR2
        {
            get => _lowSpeedR2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _lowSpeedR2 = value;
            }
        }

        //private float[] _lowSpeedR3 = new float[3] { 0, 30, 100 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("4.示教低速"), DisplayName("R3示教低速"), Description("起始速度、最大速度、加速度")]
        //public float[] LowSpeedR3
        //{
        //    get => _lowSpeedR3;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _lowSpeedR3 = value;
        //    }
        //}

        //private float[] _lowSpeedR4 = new float[3] { 0, 30, 100 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("4.示教低速"), DisplayName("R4示教低速"), Description("起始速度、最大速度、加速度")]
        //public float[] LowSpeedR4
        //{
        //    get => _lowSpeedR4;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _lowSpeedR4 = value;
        //    }
        //}

        #endregion

        #region 示教中速

        private float[] _midSpeedX1 = new float[3] { 5, 50, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("5.示教中速"), DisplayName("X1示教中速"), Description("起始速度、最大速度、加速度")]
        public float[] MidSpeedX1
        {
            get => _midSpeedX1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _midSpeedX1 = value;
            }
        }

        private float[] _midSpeedY1 = new float[3] { 5, 50, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("5.示教中速"), DisplayName("Y1示教中速"), Description("起始速度、最大速度、加速度")]
        public float[] MidSpeedY1
        {
            get => _midSpeedY1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _midSpeedY1 = value;
            }
        }

        private float[] _midSpeedX2 = new float[3] { 5, 50, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("5.示教中速"), DisplayName("X2示教中速"), Description("起始速度、最大速度、加速度")]
        public float[] MidSpeedX2
        {
            get => _midSpeedX2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _midSpeedX2 = value;
            }
        }

        private float[] _midSpeedY2 = new float[3] { 5, 50, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("5.示教中速"), DisplayName("Y2示教中速"), Description("起始速度、最大速度、加速度")]
        public float[] MidSpeedY2
        {
            get => _midSpeedY2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _midSpeedY2 = value;
            }
        }

        private float[] _midSpeedZ2 = new float[3] { 5, 50, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("5.示教中速"), DisplayName("Z2示教中速"), Description("起始速度、最大速度、加速度")]
        public float[] MidSpeedZ2
        {
            get => _midSpeedZ2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _midSpeedZ2 = value;
            }
        }

        private float[] _midSpeedR1 = new float[3] { 5, 100, 1000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("5.示教中速"), DisplayName("R1示教中速"), Description("起始速度、最大速度、加速度")]
        public float[] MidSpeedR1
        {
            get => _midSpeedR1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _midSpeedR1 = value;
            }
        }

        private float[] _midSpeedR2 = new float[3] { 5, 100, 1000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("5.示教中速"), DisplayName("R2示教中速"), Description("起始速度、最大速度、加速度")]
        public float[] MidSpeedR2
        {
            get => _midSpeedR2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _midSpeedR2 = value;
            }
        }

        //private float[] _midSpeedR3 = new float[3] { 5, 100, 1000 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("5.示教中速"), DisplayName("R3示教中速"), Description("起始速度、最大速度、加速度")]
        //public float[] MidSpeedR3
        //{
        //    get => _midSpeedR3;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _midSpeedR3 = value;
        //    }
        //}

        //private float[] _midSpeedR4 = new float[3] { 5, 100, 1000 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("5.示教中速"), DisplayName("R4示教中速"), Description("起始速度、最大速度、加速度")]
        //public float[] MidSpeedR4
        //{
        //    get => _midSpeedR4;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _midSpeedR4 = value;
        //    }
        //}

        #endregion

        #region 示教高速

        private float[] _highSpeedX1 = new float[3] { 10, 100, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("6.示教高速"), DisplayName("X1示教高速"), Description("起始速度、最大速度、加速度")]
        public float[] HighSpeedX1
        {
            get => _highSpeedX1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _highSpeedX1 = value;
            }
        }

        private float[] _highSpeedY1 = new float[3] { 10, 100, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("6.示教高速"), DisplayName("Y1示教高速"), Description("起始速度、最大速度、加速度")]
        public float[] HighSpeedY1
        {
            get => _highSpeedY1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _highSpeedY1 = value;
            }
        }

        private float[] _highSpeedX2 = new float[3] { 10, 100, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("6.示教高速"), DisplayName("X2示教高速"), Description("起始速度、最大速度、加速度")]
        public float[] HighSpeedX2
        {
            get => _highSpeedX2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _highSpeedX2 = value;
            }
        }

        private float[] _highSpeedY2 = new float[3] { 10, 100, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("6.示教高速"), DisplayName("Y2示教高速"), Description("起始速度、最大速度、加速度")]
        public float[] HighSpeedY2
        {
            get => _highSpeedY2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _highSpeedY2 = value;
            }
        }

        private float[] _highSpeedZ2 = new float[3] { 10, 100, 500 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("6.示教高速"), DisplayName("Z2示教高速"), Description("起始速度、最大速度、加速度")]
        public float[] HighSpeedZ2
        {
            get => _highSpeedZ2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _highSpeedZ2 = value;
            }
        }

        private float[] _highSpeedR1 = new float[3] { 10, 200, 1000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("6.示教高速"), DisplayName("R1示教高速"), Description("起始速度、最大速度、加速度")]
        public float[] HighSpeedR1
        {
            get => _highSpeedR1;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _highSpeedR1 = value;
            }
        }

        private float[] _highSpeedR2 = new float[3] { 10, 200, 1000 };

        [Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        [Category("6.示教高速"), DisplayName("R2示教高速"), Description("起始速度、最大速度、加速度")]
        public float[] HighSpeedR2
        {
            get => _highSpeedR2;
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 30) value[0] = 30;
                if (value[1] < 0) value[1] = 0;
                if (value[1] > 500) value[1] = 500;
                if (value[2] < 0) value[2] = 0;
                if (value[2] > 4000) value[2] = 4000;
                _highSpeedR2 = value;
            }
        }

        //private float[] _highSpeedR3 = new float[3] { 10, 200, 1000 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("6.示教高速"), DisplayName("R3示教高速"), Description("起始速度、最大速度、加速度")]
        //public float[] HighSpeedR3
        //{
        //    get => _highSpeedR3;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _highSpeedR3 = value;
        //    }
        //}

        //private float[] _highSpeedR4 = new float[3] { 10, 200, 1000 };

        //[Editor(typeof(HidePropertyViewArrayEdit), typeof(UITypeEditor))]
        //[Category("6.示教高速"), DisplayName("R4示教高速"), Description("起始速度、最大速度、加速度")]
        //public float[] HighSpeedR4
        //{
        //    get => _highSpeedR4;
        //    set
        //    {
        //        if (value[0] < 0) value[0] = 0;
        //        if (value[0] > 30) value[0] = 30;
        //        if (value[1] < 0) value[1] = 0;
        //        if (value[1] > 500) value[1] = 500;
        //        if (value[2] < 0) value[2] = 0;
        //        if (value[2] > 4000) value[2] = 4000;
        //        _highSpeedR4 = value;
        //    }
        //}

        #endregion

        #endregion

        #region 端口IO配置

        [Category("7.端口配置"), DisplayName("定位上相机光源")]
        public EN_GoogolExtendOutput OutUpLightHX { get; set; } = EN_GoogolExtendOutput.Out0_0;

        //[Category("7.端口配置"), DisplayName("定位上相机同轴光源")]
        //public EN_GoogolExtendOutput OutUpLightTZ { get; set; } = EN_GoogolExtendOutput.Out1_4;

        [Category("7.端口配置"), DisplayName("飞达上相机光源")]
        public EN_GoogolExtendOutput OutFeederUpLight { get; set; } = EN_GoogolExtendOutput.Out0_1;

        [Category("7.端口配置"), DisplayName("左下相机光源")]
        public EN_GoogolExtendOutput OutDownLight { get; set; } = EN_GoogolExtendOutput.Out0_2;

        [Category("7.端口配置"), DisplayName("右下相机光源")]
        public EN_GoogolExtendOutput OutRightDownLight { get; set; } = EN_GoogolExtendOutput.Out0_3;

        [Category("7.端口配置"), DisplayName("飞达1左吹气")]
        public EN_GoogolExtendOutput OutFeederVacuumRuptureLeft { get; set; } = EN_GoogolExtendOutput.Out1_4;

        [Category("7.端口配置"), DisplayName("飞达1右吹气")]
        public EN_GoogolExtendOutput OutFeederVacuumRuptureRight { get; set; } = EN_GoogolExtendOutput.Out1_5;
        [Category("7.端口配置"), DisplayName("飞达2左吹气")]
        public EN_GoogolExtendOutput OutFeeder2VacuumRuptureLeft { get; set; } = EN_GoogolExtendOutput.Out1_2;

        [Category("7.端口配置"), DisplayName("飞达2右吹气")]
        public EN_GoogolExtendOutput OutFeeder2VacuumRuptureRight { get; set; } = EN_GoogolExtendOutput.Out1_3;

        [Category("7.端口配置"), DisplayName("1#气缸上")]
        [Browsable(true)]
        public EN_GoogolExtendOutput OutCylinderUpNo1 { get; set; } = EN_GoogolExtendOutput.Out0_12;

        [Category("7.端口配置"), DisplayName("2#气缸上")]
        [Browsable(true)]
        public EN_GoogolExtendOutput OutCylinderUpNo2 { get; set; } = EN_GoogolExtendOutput.Out0_13;

        [Category("7.端口配置"), DisplayName("3#气缸上")]
        [Browsable(true)]
        public EN_GoogolExtendOutput OutCylinderUpNo3 { get; set; } = EN_GoogolExtendOutput.Out0_14;

        [Category("7.端口配置"), DisplayName("4#气缸上")]
        [Browsable(true)]
        public EN_GoogolExtendOutput OutCylinderUpNo4 { get; set; } = EN_GoogolExtendOutput.Out0_15;

        [Category("7.端口配置"), DisplayName("1#气缸下")]
        public EN_GoogolExtendOutput OutCylinderDownNo1 { get; set; } = EN_GoogolExtendOutput.Out1_0;

        [Category("7.端口配置"), DisplayName("2#气缸下")]
        public EN_GoogolExtendOutput OutCylinderDownNo2 { get; set; } = EN_GoogolExtendOutput.Out1_1;

        [Category("7.端口配置"), DisplayName("3#气缸下")]
        public EN_GoogolExtendOutput OutCylinderDownNo3 { get; set; } = EN_GoogolExtendOutput.Out1_2;

        [Category("7.端口配置"), DisplayName("4#气缸下")]
        public EN_GoogolExtendOutput OutCylinderDownNo4 { get; set; } = EN_GoogolExtendOutput.Out1_3;

        [Category("7.端口配置"), DisplayName("1#真空吸")]
        public EN_GoogolExtendOutput OutVacuumSuctionNo1 { get; set; } = EN_GoogolExtendOutput.Out0_4;

        [Category("7.端口配置"), DisplayName("2#真空吸")]
        public EN_GoogolExtendOutput OutVacuumSuctionNo2 { get; set; } = EN_GoogolExtendOutput.Out0_6;

        [Category("7.端口配置"), DisplayName("3#真空吸")]
        public EN_GoogolExtendOutput OutVacuumSuctionNo3 { get; set; } = EN_GoogolExtendOutput.Out0_8;

        [Category("7.端口配置"), DisplayName("4#真空吸")]
        public EN_GoogolExtendOutput OutVacuumSuctionNo4 { get; set; } = EN_GoogolExtendOutput.Out0_10;

        [Category("7.端口配置"), DisplayName("1#真空破")]
        public EN_GoogolExtendOutput OutVacuumRuptureNo1 { get; set; } = EN_GoogolExtendOutput.Out0_5;

        [Category("7.端口配置"), DisplayName("2#真空破")]
        public EN_GoogolExtendOutput OutVacuumRuptureNo2 { get; set; } = EN_GoogolExtendOutput.Out0_7;

        [Category("7.端口配置"), DisplayName("3#真空破")]
        public EN_GoogolExtendOutput OutVacuumRuptureNo3 { get; set; } = EN_GoogolExtendOutput.Out0_9;

        [Category("7.端口配置"), DisplayName("4#真空破")]
        public EN_GoogolExtendOutput OutVacuumRuptureNo4 { get; set; } = EN_GoogolExtendOutput.Out0_11;

        [Category("7.端口配置"), DisplayName("1#气缸上到位")]
        public EN_GoogolExtendInput InCylinderUpReadyNo1 { get; set; } = EN_GoogolExtendInput.In0_0;

        [Category("7.端口配置"), DisplayName("2#气缸上到位")]
        public EN_GoogolExtendInput InCylinderUpReadyNo2 { get; set; } = EN_GoogolExtendInput.In0_1;

        [Category("7.端口配置"), DisplayName("3#气缸上到位")]
        public EN_GoogolExtendInput InCylinderUpReadyNo3 { get; set; } = EN_GoogolExtendInput.In0_2;

        [Category("7.端口配置"), DisplayName("4#气缸上到位")]
        public EN_GoogolExtendInput InCylinderUpReadyNo4 { get; set; } = EN_GoogolExtendInput.In0_3;

        [Category("7.端口配置"), DisplayName("1#气缸下到位")]
        [Browsable(true)]
        public EN_GoogolExtendInput InCylinderDownReadyNo1 { get; set; } = EN_GoogolExtendInput.In1_0;

        [Category("7.端口配置"), DisplayName("2#气缸下到位")]
        [Browsable(true)]
        public EN_GoogolExtendInput InCylinderDownReadyNo2 { get; set; } = EN_GoogolExtendInput.In1_1;

        [Category("7.端口配置"), DisplayName("3#气缸下到位")]
        [Browsable(true)]
        public EN_GoogolExtendInput InCylinderDownReadyNo3 { get; set; } = EN_GoogolExtendInput.In1_2;

        [Category("7.端口配置"), DisplayName("4#气缸下到位")]
        [Browsable(true)]
        public EN_GoogolExtendInput InCylinderDownReadyNo4 { get; set; } = EN_GoogolExtendInput.In1_3;

        [Category("7.端口配置"), DisplayName("1#物料OK")]
        public EN_GoogolExtendInput InMaterialReadyNo1 { get; set; } = EN_GoogolExtendInput.In0_4;

        [Category("7.端口配置"), DisplayName("2#物料OK")]
        public EN_GoogolExtendInput InMaterialReadyNo2 { get; set; } = EN_GoogolExtendInput.In0_5;

        [Category("7.端口配置"), DisplayName("3#物料OK")]
        public EN_GoogolExtendInput InMaterialReadyNo3 { get; set; } = EN_GoogolExtendInput.In0_6;

        [Category("7.端口配置"), DisplayName("4#物料OK")]
        public EN_GoogolExtendInput InMaterialReadyNo4 { get; set; } = EN_GoogolExtendInput.In0_7;

        #endregion

        #region 核心参数勿动

        [Category("8.核心参数勿动"), DisplayName("是否为双控气缸机台"), ReadOnly(true)]
        public bool IsCylinderDoubleSwitch { get; set; } = true;

        [Category("8.核心参数勿动"), DisplayName("贴装轴安全X（相机右避让阈值）"), ReadOnly(true)]
        public float SafeX2 { get; set; } = 210;

        [Category("8.核心参数勿动"), DisplayName("贴装轴安全Y（距原点最大安全距离）"), ReadOnly(true)]
        public float SafeY2 { get; set; } = 120;

        [Category("8.核心参数勿动"), DisplayName("相机轴X最大值（略小于最大行程）"), ReadOnly(true)]
        public float X1Max { get; set; } = 320;

        [Category("8.核心参数勿动"), DisplayName("相机轴Y最大值（略小于最大行程）"), ReadOnly(true)]
        public float Y1Max { get; set; } = 250;

        [Category("8.核心参数勿动"), DisplayName("贴装轴X最大值（略小于最大行程）"), ReadOnly(true)]
        public float X2Max { get; set; } = 300;

        [Category("8.核心参数勿动"), DisplayName("贴装轴Y最大值（略小于最大行程）"), ReadOnly(true)]
        public float Y2Max { get; set; } = 460;

        [Category("8.核心参数勿动"), DisplayName("贴装轴Z最大值（略小于最大行程）"), ReadOnly(true)]
        public float Z2Max { get; set; } = 40;
        [Category("8.核心参数勿动"), DisplayName("贴装轴Z安全高度（在任何位置都处于绝对安全的一个高度）"), ReadOnly(true)]
        public float Z2Height { get; set; } = 10;
        [Category("8.核心参数勿动"), DisplayName("Y1轴带机构+Y2轴带机构最大值（略小于贴装Y轴带机构与相机Y轴带机构相撞）"), ReadOnly(true)]
        public float Y1Y2Max { get; set; } = 370;


        [Category("8.核心参数勿动"), DisplayName("X1轴带机构+X2轴带机构最大值（略小于贴装X轴带机构与相机X轴带机构相撞）"), ReadOnly(true)]
        public float X1X2Max { get; set; } = 260;


        [Category("8.核心参数勿动"), DisplayName("Y1轴+Y2轴带机构最大值（略小于贴装Y轴带机构与相机Y轴相撞）"), ReadOnly(true)]
        public float Y1Y2JGMax { get; set; } = 450;
        #endregion

        public List<float[]> GetSpeeds(En_SpeedType speed)
        {
            List<float[]> ret = new List<float[]>();
            switch (speed)
            {
                case En_SpeedType.Low:
                    ret.Add(LowSpeedX1);
                    ret.Add(LowSpeedY1);
                    //ret.Add(LowSpeedZ1);
                    ret.Add(LowSpeedX2);
                    ret.Add(LowSpeedY2);
                    ret.Add(LowSpeedZ2);
                    ret.Add(LowSpeedR1);
                    ret.Add(LowSpeedR2);
                    //ret.Add(LowSpeedR3);
                    //ret.Add(LowSpeedR4);
                    break;
                case En_SpeedType.Mid:
                    ret.Add(MidSpeedX1);
                    ret.Add(MidSpeedY1);

                    //ret.Add(MidSpeedZ1);
                    ret.Add(MidSpeedX2);
                    ret.Add(MidSpeedY2);
                    ret.Add(MidSpeedZ2);
                    ret.Add(MidSpeedR1);
                    ret.Add(MidSpeedR2);
                    //ret.Add(MidSpeedR3);
                    //ret.Add(MidSpeedR4);
                    break;
                case En_SpeedType.High:
                    ret.Add(HighSpeedX1);
                    ret.Add(HighSpeedY1);

                    //ret.Add(HighSpeedZ1);
                    ret.Add(HighSpeedX2);
                    ret.Add(HighSpeedY2);
                    ret.Add(HighSpeedZ2);
                    ret.Add(HighSpeedR1);
                    ret.Add(HighSpeedR2);
                    //ret.Add(HighSpeedR3);
                    //ret.Add(HighSpeedR4);
                    break;
                case En_SpeedType.Work:
                    ret.Add(WorkSpeedX1);
                    ret.Add(WorkSpeedY1);
                    //ret.Add(WorkSpeedZ1);
                    ret.Add(WorkSpeedX2);
                    ret.Add(WorkSpeedY2);
                    ret.Add(WorkSpeedZ2);
                    ret.Add(WorkSpeedR1);
                    ret.Add(WorkSpeedR2);
                    //ret.Add(WorkSpeedR3);
                    //ret.Add(WorkSpeedR4);
                    break;
            }
            return ret;
        }

        public float[] GetSpeed(En_AxisNum axis, En_SpeedType speed)
        {
            var speeds = GetSpeeds(speed);
            float[] ret = new float[3];
            switch (axis)
            {
                case En_AxisNum.X1:
                    ret = speeds[0];
                    break;
                case En_AxisNum.Y1:
                    ret = speeds[1];
                    break;
                case En_AxisNum.X2:
                    ret = speeds[2];
                    break;
                case En_AxisNum.Y2:
                    ret = speeds[3];
                    break;
                case En_AxisNum.Z2:
                    ret = speeds[4];
                    break;
                case En_AxisNum.R1:
                    ret = speeds[5];
                    break;
                case En_AxisNum.R2:
                    ret = speeds[6];
                    break;
                //case En_AxisNum.R3:
                //    ret = speeds[7];
                //    break;
                //case En_AxisNum.R4:
                //    ret = speeds[8];
                //    break;
            }
            return ret;
        }
    }
}
