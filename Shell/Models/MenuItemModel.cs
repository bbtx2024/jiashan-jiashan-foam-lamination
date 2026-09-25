using System.Windows.Media;
using Caliburn.Micro;
using FontAwesome5;
using QA_Infrastructure;

namespace QA.IntelligentEquipment.Models
{
    public class MenuItemModel : PropertyChangedBase
    {
        private EFontAwesomeIcon _iconName;
        public EFontAwesomeIcon IconName
        {
            get => _iconName;
            set
            {
                _iconName = value;
                NotifyOfPropertyChange(() => IconName);
            }
        }

        private string _tagName;
        public string TagName
        {
            get => _tagName;
            set
            {
                _tagName = value;
                NotifyOfPropertyChange(() => TagName);
            }
        }

        //根据值进行颜色切换
        private int _showColorType;
        public int ShowColorType
        {
            get => _showColorType;
            set
            {
                _showColorType = value;
                NotifyOfPropertyChange(() => ShowColorType);
            }
        }

        private SolidColorBrush _backGroudBrushes;
        public SolidColorBrush BackGroudBrushes
        {
            get => _backGroudBrushes;
            set
            {
                _backGroudBrushes = value;
                NotifyOfPropertyChange(() => BackGroudBrushes);
            }
        }

        private bool _isNeedLogin;
        public bool IsNeedLogin
        {
            get => _isNeedLogin;
            set
            {
                _isNeedLogin = value;
                NotifyOfPropertyChange(() => IsNeedLogin);
            }
        }

        private EN_UserType _loginType;
        public EN_UserType LoginType
        {
            get => _loginType;
            set
            {
                _loginType = value;
                NotifyOfPropertyChange(() => LoginType);
            }
        }
    }
}
