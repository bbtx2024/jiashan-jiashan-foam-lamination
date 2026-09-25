using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using QA.Business.Define;

namespace QA.Business
{
    /// <summary>
    /// PlcIOMode
    /// </summary>
    public class PlcIOMode : INotifyPropertyChanged
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


        private bool _cddIsPosition1;
        /// <summary>
        /// 相机是否在飞达1位置
        /// </summary>
        public bool CddIsPosition1
        {
            get { return _cddIsPosition1; }
            set { _cddIsPosition1 = value; NotifyOfPropertyChange(() => CddIsPosition1); }
        }
        private bool _cddIsPosition2;
        /// <summary>
        /// 相机是否在飞达2位置
        /// </summary>
        public bool CddIsPosition2
        {
            get { return _cddIsPosition2; }
            set { _cddIsPosition2 = value; NotifyOfPropertyChange(() => CddIsPosition2); }
        }
        private bool _feederTypeStatus;
        /// <summary>
        /// 飞达1有无料状态
        /// </summary>
        public bool FeederTypeStatus
        {
            get { return _feederTypeStatus; }
            set { _feederTypeStatus = value; NotifyOfPropertyChange(() => FeederTypeStatus); }
        }
        private bool _feederInPlaceStatus;
        /// <summary>
        /// 飞达1到位状态
        /// </summary>
        public bool FeederInPlaceStatus
        {
            get { return _feederInPlaceStatus; }
            set { _feederInPlaceStatus = value; NotifyOfPropertyChange(() => FeederInPlaceStatus); }
        }
        private bool _feeder2TypeStatus;
        /// <summary>
        /// 飞达2有无料状态
        /// </summary>
        public bool Feeder2TypeStatus
        {
            get { return _feeder2TypeStatus; }
            set { _feeder2TypeStatus = value; NotifyOfPropertyChange(() => Feeder2TypeStatus); }
        }
        private bool _feeder2InPlaceStatus;
        /// <summary>
        /// 飞达2到位状态
        /// </summary>
        public bool Feeder2InPlaceStatus
        {
            get { return _feeder2InPlaceStatus; }
            set { _feeder2InPlaceStatus = value; NotifyOfPropertyChange(() => Feeder2InPlaceStatus); }
        }


    }
}
