using System;
using System.ComponentModel;
using QA.Business.Interfaces;
using QA_Infrastructure;

namespace QA.Business.Component.Clean
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("清洗设置")]
    public class CleanParam : IParam
    {
        [Category("0.启用")]
        [DisplayName("启用超时关闭焊台(1小时后自动休眠)")]
        public bool IsIdleClose378 { get; set; } = false;

        #region 空闲清洗
        [Category("1.空闲清洗工艺")]
        [DisplayName("空闲清洗")]
        public bool IsIdleClean { get; set; } = false;

        [Browsable(false)]
        public int timeoutClean { get; set; } = 10;

        [Category("1.空闲清洗工艺")]
        [DisplayName("空闲多久清洗(1分钟到60分钟)")]
        public int CleanDelayIdle
        {
            get
            {
                return timeoutClean;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 60) value = 60;
                timeoutClean = value;
            }
        }
        [Browsable(false)]
        public double IdleTinLenLimit { get; set; } = 10;
        [Category("1.空闲清洗工艺")]
        [DisplayName("空闲送锡长度")]
        public double IdleTinLen
        {
            get
            {
                return IdleTinLenLimit;
            }
            set
            {
                if (value <= 0.1) value = 0.1;
                if (value >= 30) value = 30;
                IdleTinLenLimit = value;
            }
        }
        [Browsable(false)]
        public double IdleTinRetlenLimit { get; set; } = 10;
        [Category("1.空闲清洗工艺")]
        [DisplayName("空闲回锡长度")]
        public double IdleTinRetlen
        {
            get
            {
                return IdleTinRetlenLimit;
            }
            set
            {
                if (value <= 0.1) value = 0.1;
                if (value >= 30) value = 30;
                IdleTinRetlenLimit = value;
            }
        }
        [Browsable(false)]
        public double IdleTinSpdLimit { get; set; } = 10;
        [Category("1.空闲清洗工艺")]
        [DisplayName("空闲送锡速度")]
        public double IdleTinSpd
        {
            get
            {
                return IdleTinSpdLimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 100) value = 100;
                IdleTinSpdLimit = value;
            }
        }
        [Browsable(false)]
        public int IdleCleanTimeLimit { get; set; } = 100;
        [Category("1.空闲清洗工艺")]
        [DisplayName("空闲清洗时间")]
        public int IdleCleanTime
        {
            get
            {
                return IdleCleanTimeLimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 5000) value = 5000;
                IdleCleanTimeLimit = value;
            }
        }

        [Browsable(false)]
        public int IdletimeoutCleanlimit { get; set; } = 3;

        [Category("1.空闲清洗工艺")]
        [DisplayName("空闲清洗次数")]
        public int IdleCleanCount
        {
            get
            {
                return IdletimeoutCleanlimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 10) value = 10;
                IdletimeoutCleanlimit = value;
            }
        }

        #endregion 空闲清洗

        [Category("1.清洗工艺")]
        [DisplayName("启用每工作几个产品清洗")]
        public bool IsCleanWorkCount { get; set; } = false;

        [Browsable(false)]
        public int CleanWorkCountLimit { get; set; } = 10;
        [Category("1.清洗工艺")]
        [DisplayName("每工作几个产品清洗")]
        public int CleanWorkCount
        {
            get
            {
                return CleanWorkCountLimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 100) value = 100;
                CleanWorkCountLimit = value;
            }
        }
        [Browsable(false)]
        public int CleanTimeLimit { get; set; } = 100;
        [Category("1.清洗工艺")]
        [DisplayName("清洗时间")]
        [Description("ms")]
        public int CleanTime
        {
            get
            {
                return CleanTimeLimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 5000) value = 5000;
                CleanTimeLimit = value;
            }
        }

        [Browsable(false)]
        public int timeoutCleanlimit { get; set; } = 3;

        [Category("1.清洗工艺")]
        [DisplayName("清洗次数")]
        public int CleanCount
        {
            get
            {
                return timeoutCleanlimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 10) value = 10;
                timeoutCleanlimit = value;
            }
        }

        [Browsable(false)]
        public double CleanTinLenlimit { get; set; } = 1;

        [Category("1.清洗工艺")]
        [DisplayName("清洗后送锡长度")]
        public double CleanTinLen
        {
            get
            {
                return CleanTinLenlimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 50) value = 50;
                CleanTinLenlimit = value;
            }
        }
        [Browsable(false)]
        public double CleanTinRetlenlimit { get; set; } = 1;

        [Category("1.清洗工艺")]
        [DisplayName("清洗后回锡长度")]
        public double CleanTinRetlen
        {
            get
            {
                return CleanTinRetlenlimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 50) value = 50;
                CleanTinRetlenlimit = value;
            }
        }

        [Browsable(false)]
        public double TinSpdLimit { get; set; } = 10;
        [Category("1.清洗工艺")]
        [DisplayName("送锡速度")]
        public double TinSpd
        {
            get
            {
                return TinSpdLimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 100) value = 100;
                TinSpdLimit = value;
            }
        }
        [Browsable(false)]
        public double TinPluseLimit { get; set; } = 80;
        [Category("1.清洗工艺")]
        [DisplayName("出锡脉冲当量")]
        public double TinPluse
        {
            get
            {
                return TinPluseLimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 200) value = 200;
                TinPluseLimit = value;
            }
        }
        [Browsable(false)]
        public int WheeltimeLimit { get; set; } = 100;
        [Category("1.清洗工艺")]
        [DisplayName("滚轮延时ms")]
        public int Wheeltime
        {
            get
            {
                return WheeltimeLimit;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 5000) value = 5000;
                WheeltimeLimit = value;
            }
        }
    }
}
