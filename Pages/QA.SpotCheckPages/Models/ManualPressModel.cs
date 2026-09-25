using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using QA.Business.Component.Motion.Googol;

namespace QA.SpotCheckPages.Models
{
    public class ManualPressModel : INotifyPropertyChanged
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

        public class HeadModel
        {
            public string Head { get; set; } = "";
            public En_AxisNum Axis { get; set; } = En_AxisNum.R1;
        }

        public List<HeadModel> Heads { get; set; } = null;      //所有机头集合

        private int _sltHeadIdx = 0;                            //当前选中的机头idx
        public int SltNozzleIdx
        {
            get { return _sltHeadIdx; }
            set { _sltHeadIdx = value; NotifyOfPropertyChange(() => SltNozzleIdx); }
        }

        private string _curPress = "0";                         //当前机头压力
        public string CurPress
        {
            get { return _curPress; }
            set { _curPress = value; NotifyOfPropertyChange(() => CurPress); }
        }

        private float _lowPressCalibValue = 0;                  //低压校准数值
        public float LowPressCalibValue
        {
            get { return _lowPressCalibValue; }
            set { _lowPressCalibValue = value; NotifyOfPropertyChange(() => LowPressCalibValue); }
        }

        private float _highPressCalibValue = 0;                 //高压校准数值
        public float HighPressCalibValue
        {
            get { return _highPressCalibValue; }
            set { _highPressCalibValue = value; NotifyOfPropertyChange(() => HighPressCalibValue); }
        }

        private float _doPressTargetPos = 0;                    //执行空压目标位置
        public float DoPressTargetPos
        {
            get { return _doPressTargetPos; }
            set { _doPressTargetPos = value; NotifyOfPropertyChange(() => DoPressTargetPos); }
        }

        private float _doPressTargetPress = 0;                  //执行空压目标压力
        public float DoPressTargetPress
        {
            get { return _doPressTargetPress; }
            set { _doPressTargetPress = value; NotifyOfPropertyChange(() => DoPressTargetPress); }
        }

        private float _doPressSpeed = 5;                        //执行空压速度
        public float DoPressSpeed
        {
            get { return _doPressSpeed; }
            set { _doPressSpeed = value; NotifyOfPropertyChange(() => DoPressSpeed); }
        }


        private float _doPressBufferDis = 5;                        //执行空压缓冲速度
        public float DoPressBufferDis
        {
            get { return _doPressBufferDis; }
            set { _doPressBufferDis = value; NotifyOfPropertyChange(() => DoPressBufferDis); }
        }

        private float _doPressPosRange = 5;                        //执行空压位置范围
        public float DoPressPosRange
        {
            get { return _doPressPosRange; }
            set { _doPressPosRange = value; NotifyOfPropertyChange(() => DoPressPosRange); }
        }


        private float _doPressTimeout = 1000;                   //执行空压超时
        public float DoPressTimeout
        {
            get { return _doPressTimeout; }
            set { _doPressTimeout = value; NotifyOfPropertyChange(() => DoPressTimeout); }
        }

        private float _doPressLiftUp = 10;                      //执行空压上抬距离
        public float DoPressLiftUp
        {
            get { return _doPressLiftUp; }
            set { _doPressLiftUp = value; NotifyOfPropertyChange(() => DoPressLiftUp); }
        }

        private float _beforeRate = 1;                          //提前量
        public float BeforeRate
        {
            get { return _beforeRate; }
            set { _beforeRate = value; NotifyOfPropertyChange(() => BeforeRate); }
        }
        public ManualPressModel()
        {
            Heads = new List<HeadModel>()
            {
                new HeadModel(){ Head = "Head01", Axis = En_AxisNum.Z2, },
                new HeadModel(){ Head = "Head02", Axis = En_AxisNum.Z2, },
            };
        }
    }

}
