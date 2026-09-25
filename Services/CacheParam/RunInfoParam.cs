using System;
using System.ComponentModel;
using System.Linq.Expressions;
using QA_Infrastructure;

namespace QA.Business.CacheParam
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    public class RunInfoParam : INotifyPropertyChanged
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

        private int _totalSolderCount = 0;
        public int TotalSolderCount
        {
            get { return _totalSolderCount; }
            set { _totalSolderCount = value; NotifyOfPropertyChange(() => TotalSolderCount); }
        }

        //public TimeSpan LastTimeSpan { get; set; }

        public string CurTaskName { get; set; }

        public bool SwitchLanguage { get; set; }

        public string LastAllTimeSpan { get; set; } = string.Empty;

    }
}
