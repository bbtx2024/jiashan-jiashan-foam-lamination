using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Caliburn.Micro;
using QA.Business.Converts;
using QA.Business.Define;
using QA.Business.Manager;

namespace QA.Pages.Models
{
    public class TrayInfoModel : INotifyPropertyChanged
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

        private int _cavityNum;
        public int CavityNum
        {
            get { return _cavityNum; }
            set
            {
                _cavityNum = value;
                NotifyOfPropertyChange(() => CavityNum);
                NotifyOfPropertyChange(() => CavUseInfo);
            }
        }

        private bool _isUsed;
        public bool IsUsed
        {
            get { return _isUsed; }
            set
            {
                _isUsed = value;
                NotifyOfPropertyChange(() => IsUsed);
                NotifyOfPropertyChange(() => CavUseInfo);
                Status = _isUsed ? EN_TrayStatus.Common : EN_TrayStatus.Unuse;
            }
        }

        public string CavUseInfo
        {
            //get { return CavityNum + "穴" + (IsUsed ? "启用" : "禁用"); }
            get
            {
                var _paramManager = IoC.Get<ParamManager>();
                string strdata = CavityNum + "穴" + (IsUsed ? "启用" : "禁用");
                if (_paramManager.OtherSettingParam.LanguageIndex == 1)
                {
                    strdata = CavityNum + "Hole" + (IsUsed ? "Enable" : "Disable");
                }
                else if (_paramManager.OtherSettingParam.LanguageIndex == 2)
                {
                    strdata = CavityNum + "huyệt" + (IsUsed ? "Bật" : "Vô hiệu hóa");
                }
                return strdata;
            }
        }

        private EN_TrayStatus _status;
        public EN_TrayStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyOfPropertyChange(() => Status);
                NotifyOfPropertyChange(() => CavStateInfo);
            }
        }

        private static readonly TrayStatusToStringConvert trayStatusToStringConvert = new TrayStatusToStringConvert();
        public string CavStateInfo
        {
            get => (string)trayStatusToStringConvert.Convert(Status, null, null, null);
        }

        public string Str1
        {
            get;
        } = "";

        public string Str2
        {
            get;
        } = "";
    }
}
