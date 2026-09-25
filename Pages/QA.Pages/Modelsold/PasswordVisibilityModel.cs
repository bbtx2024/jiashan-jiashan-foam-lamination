using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using FontAwesome5;

namespace QA.Pages.Models
{
    public class PasswordVisibilityModel : PropertyChangedBase
    {
        private bool _isPasswordVisibility;
        public bool IsPasswordVisibility
        {
            get => _isPasswordVisibility;
            set {
                _isPasswordVisibility = value;
                NotifyOfPropertyChange(() => IsPasswordVisibility);
            } 
        }

        private EFontAwesomeIcon _iconName;
        public EFontAwesomeIcon IconName
        {
            get => _iconName;
            set {
                _iconName = value;
                NotifyOfPropertyChange(()=> IconName);
            } 
        }
    }
}
