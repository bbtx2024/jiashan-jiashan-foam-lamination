using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using FontAwesome5;

namespace QA.Pages.Models
{
    public class MenuItemModel : PropertyChangedBase
    {
        private EFontAwesomeIcon _iconName;
        public EFontAwesomeIcon IconName
        {
            get => _iconName;
            set {
                _iconName = value;
                NotifyOfPropertyChange(()=> IconName);
            } 
        }

        private string _menuName;
        public string MenuName
        {
            get => _menuName;
            set {
                _menuName = value;
                NotifyOfPropertyChange(()=> MenuName);
            } 
        }
    }
}
