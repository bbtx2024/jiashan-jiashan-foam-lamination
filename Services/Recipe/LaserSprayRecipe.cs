using System;
using System.ComponentModel;
using System.Linq.Expressions;
using CSSI9075;

namespace QA.Business.Recipe
{
    [Serializable]
    public class LaserSprayRecipe : INotifyPropertyChanged, IRecipe
    {
        #region NotifyOfPropertyChange
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            MemberExpression member = (MemberExpression)property.Body;
            string propName = member.Member.Name;
            NotifyOfPropertyChange(propName);
        }
        #endregion

        private string _RecipeName = "";
        [Category("1.名称"), DisplayName("配方名称"), ReadOnly(false)]
        public string RecipeName
        {
            get { return _RecipeName; }
            set
            {
                _RecipeName = value;
                NotifyOfPropertyChange(() => RecipeName);
            }
        }

        private float Place1stHeightLimit = 10;
        [Category("2.贴合参数")]
        [DisplayName("一次高度")]
        [Description("mm，3mm-100mm")]
        public float Place1stHeight
        {
            get { return Place1stHeightLimit; }
            set
            {
                if (value < 3) value = 3;
                if (value > 100) value = 100;
                Place1stHeightLimit = value;
            }
        }

        private float[] place1stHeightSetSpeedLimit = new float[3] { 0, 20, 200 };
        [Category("2.贴合参数")]
        [DisplayName("一次高度后设置速度")]

        public float[] Place1stHeightSetSpeed
        {
            get { return place1stHeightSetSpeedLimit; }
            set
            {
                if (value[0] < 0) value[0] = 0;
                if (value[0] > 50) value[0] = 50;

                if (value[1] < 10) value[1] = 10;
                if (value[1] > 300) value[1] = 300;

                if (value[2] < 100) value[2] = 100;
                if (value[2] > 2000) value[2] = 2000;

                place1stHeightSetSpeedLimit = value;
            }
        }

        private int place1stDelayLimit = 200;
        [Category("2.贴合参数")]
        [DisplayName("保压时间")]
        [Description("ms，1ms-5000ms")]
        public int Place1stDelay
        {
            get { return place1stDelayLimit; }
            set
            {
                if (value < 1) value = 1;
                if (value > 5000) value = 5000;
                place1stDelayLimit = value;
            }
        }

        private int breakDelay = 200;
        [Category("2.贴合参数")]
        [DisplayName("真空破时间")]
        [Description("ms")]
        public int BreakDelay
        {
            get { return breakDelay; }
            set
            {
                if (value < 1) value = 1;
                if (value > 5000) value = 5000;
                breakDelay = value;
            }
        }

        [Category("3.压力参数")]
        [DisplayName("启用找压力模式")]
        public bool PosMode { get; set; } = false;

        private float PressLimit = 5;
        [Category("3.压力参数")]
        [DisplayName("目标压力")]
        [Description("N，1N-20N")]
        public float Press
        {
            get { return PressLimit; }
            set
            {
                if (value < 1) value = 1f;
                if (value > 20) value = 20f;
                PressLimit = value;
            }
        }

        private float beforePress = 1;
        [Category("3.压力参数")]
        [DisplayName("压力提前量")]
        [Description("N，0N-2N，压力到达“目标-提前量”时将会发送轴移动停止指令")]
        public float BeforePress
        {
            get { return beforePress; }
            set
            {
                if (value < 0) value = 0f;
                if (value > 2) value = 2f;
                if (value > Press) value = Press;
                beforePress = value;
            }
        }

        private float minPress = 4;
        [Category("3.压力参数")]
        [DisplayName("最小压力")]
        [Description("N，0N-20N，最终稳定压力低于该值则报警")]
        public float MinPress
        {
            get { return minPress; }
            set
            {
                if (value < 0) value = 0f;
                if (value > 20) value = 20f;
                minPress = value;
            }
        }

        private float maxPress = 6;
        [Category("3.压力参数")]
        [DisplayName("最大压力")]
        [Description("N，0N-20N，最终稳定压力高于该值则报警")]
        public float MaxPress
        {
            get { return maxPress; }
            set
            {
                if (value < 0) value = 0f;
                if (value > 20) value = 20f;
                maxPress = value;
            }
        }

        private ushort findPressTimeout = 5000;
        [Category("3.压力参数")]
        [DisplayName("找压力超时")]
        [Description("ms，1ms-10000ms")]
        public ushort FindPressTimeout
        {
            get
            {
                return findPressTimeout;
            }
            set
            {
                if (value <= 1) value = 1;
                if (value >= 10000) value = 10000;
                findPressTimeout = value;
            }
        }

        //确定下是否输入kgN
        public CSSI_FindPressParam GetFPParam(float plusez)
        {
            return new CSSI_FindPressParam()
            {
                //alarm = PosMode,
                //dirdisrange = DisRange * plusez,
                //dir_f = Press * 100,
                //down_maxf = MaxPress * 100,
                //slowdown_dis = SlowDis * plusez,
                //slowdown_speed = SlowSpeed * plusez,
                //fqOld = false,
            };
        }
    }

    /// <summary>
    /// 激光工艺参数
    /// </summary>
    [Serializable]
    public class LaserTechnologyParam
    {
        [Category("激光焊接参数"), DisplayName("激光功率")]
        public int LaserPower { get; set; }

        [Category("激光焊接参数"), DisplayName("出光时间")]
        public int LaserOnTime { get; set; }

        [Category("激光焊接参数"), DisplayName("关光时间")]
        public int LaserOffTime { get; set; }

        public override string ToString()
        {
            return "";
        }

        public string ToStringInfo()
        {
            return $"激光功率:{LaserPower},出光时间:{LaserOnTime},关光时间:{LaserOffTime}";
        }
    }

}
