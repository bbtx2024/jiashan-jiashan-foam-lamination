using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Interfaces;

namespace QA.Pages.Models
{
    public class SystemConfigItemModel : PropertyChangedBase
    {
        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set {
                _itemName = value;
                NotifyOfPropertyChange(() => ItemName);
            } 
        }

        //private int _level;

        //public int Level
        //{
        //    get => _level;
        //    set => SetProperty(ref _level, value);
        //}

        //private IParam _paramPropertyObject;
        //public IParam ParamPropertyObject
        //{
        //    get => _paramPropertyObject;
        //    set => SetProperty(ref _paramPropertyObject, value);
        //}

        //private ObservableCollection<SystemConfigItemModel> _childSystemConfigItemModels;
        //public ObservableCollection<SystemConfigItemModel> ChildSystemConfigItemModels
        //{
        //    get => _childSystemConfigItemModels;
        //    set => SetProperty(ref _childSystemConfigItemModels, value);
        //}
    }
}
