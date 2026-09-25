using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;

namespace QA.SpotCheckPages.Models
{
    public class ManualPlcModel : INotifyPropertyChanged
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

        public ObservableCollection<ManualPlcAddrModel> ReadAddrValues { get; set; } = new ObservableCollection<ManualPlcAddrModel>();

        private ManualPlcAddrModel _sltReadAddrValue = null;        //当前选中的读取地址
        public ManualPlcAddrModel SltReadAddrValue
        {
            get { return _sltReadAddrValue; }
            set { _sltReadAddrValue = value; NotifyOfPropertyChange(() => SltReadAddrValue); }
        }

        public ObservableCollection<ManualPlcAddrModel> WriteAddrValues { get; set; } = new ObservableCollection<ManualPlcAddrModel>();

        private ManualPlcAddrModel _sltWriteAddrValue = null;       //当前选中的写入地址
        public ManualPlcAddrModel SltWriteAddrValue
        {
            get { return _sltWriteAddrValue; }
            set { _sltWriteAddrValue = value; NotifyOfPropertyChange(() => SltWriteAddrValue); }
        }

        public ManualPlcModel()
        {
            for (int i = 0; i < 20; i++)
            {
                ReadAddrValues.Add(new ManualPlcAddrModel()
                {
                    Addr = (ushort)(5000 + i),
                    Value = 0,
                });
            }
            for (int i = 0; i < 20; i++)
            {
                WriteAddrValues.Add(new ManualPlcAddrModel()
                {
                    Addr = (ushort)(5100 + i),
                    Value = 0,
                });
            }
        }

    }

    public class ManualPlcAddrModel : INotifyPropertyChanged
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

        private ushort _addr = 0;
        public ushort Addr
        {
            get { return _addr; }
            set { _addr = value; NotifyOfPropertyChange(() => _addr); }
        }

        public ushort _value = 0;
        public ushort Value
        {
            get { return _value; }
            set { _value = value; NotifyOfPropertyChange(() => Value); }
        }

    }

}
