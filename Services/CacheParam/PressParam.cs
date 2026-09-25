using System;
using System.ComponentModel;
using System.Linq.Expressions;
using QA_Infrastructure;

namespace QA.Business.CacheParam
{

    [SaveParam(FileType.JSON)]
    [Serializable]
    [Description("压力校准参数设置")]
    public class PressParam
    {
        public PressSingleParam[] SglParam = new PressSingleParam[4];
        public float TargetHeight { get; set; } = 0;
        public float TargetPress { get; set; } = 0;
        public float BufferHeight { get; set; } = 0;
        public float BufferSpeed { get; set; } = 0;
        public float PositionRange { get; set; } = 0;

        public float LiftUp { get; set; } = 0;
        public float PressTimeout { get; set; } = 0;

        public PressParam()
        {
            for (int i = 0; i < SglParam.Length; i++)
            {
                SglParam[i] = new PressSingleParam();
            }
        }
    }

    [Serializable]
    public class PressSingleParam
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyOfPropertyChange(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public void ChangeProperty<T>(Expression<Func<T>> expression)
        {
            MemberExpression member = (MemberExpression)expression.Body;
            string propName = member.Member.Name;
            NotifyOfPropertyChange(propName);
        }

        public float Amax { get; set; } = 5;//16383
        public float Amin { get; set; } = 0;
        public float Dmax { get; set; } = 20;
        public float Dmin { get; set; } = 0;



        public bool DoLowCalib(float amin, float dmin)
        {
            try
            {
                this.Amin = amin;
                this.Dmin = dmin;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            return false;
        }

        public bool DoHighCalib(float amax, float dmax)
        {
            try
            {
                this.Amax = amax;
                this.Dmax = dmax;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            return false;
        }

        public float GetCalibedPress(float Aget)
        {
            try
            {
                return (Aget - Amin) * (Dmax - Dmin) / (Amax - Amin) + Dmin;
            }
            catch (Exception)
            {
                return Aget;
            }
        }
    }
}
