using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using QA.Business.Manager;
using QA_Infrastructure;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.UserControls.ViewModels
{
    public class LoginWindowViewModel : Screen
    {
        #region Field
        private MyUserManager _myUserManager;
        private ParamManager _paramManager;
        private bool usePreviousUserTypeAfterLogin;
        private string lastManagerCardId = "";
        private EN_UserType needUserType = 0;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "用户登录";

        private List<UserInfo> _userInfos = new List<UserInfo>();
        public List<UserInfo> UserInfos
        {
            get { return _userInfos; }
            set { _userInfos = value; NotifyOfPropertyChange(() => UserInfos); UserIndex = 0; }
        }

        private int _userIndex = 0;
        public int UserIndex
        {
            get { return _userIndex; }
            set { _userIndex = value; NotifyOfPropertyChange(() => UserIndex); }
        }

        private string _userName = "";
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; NotifyOfPropertyChange(() => UserName); }
        }

        private string _password = "";
        public string Password
        {
            get { return _password; }
            set { _password = value; NotifyOfPropertyChange(() => Password); }
        }

        public bool CommonLoginVisiable { get => _paramManager.OtherSettingParam.EnableCommonLogin; }
        public bool BC750LoginVisiable { get => !_paramManager.OtherSettingParam.EnableCommonLogin; }
        public bool UserManagerVaisiable { get => _myUserManager.CurrUserInfo.Type >= MyUserManager.userTypeManager; }
        #endregion

        public LoginWindowViewModel(EN_UserType userType, bool usePreviousUserTypeAfterLogin)
        {
            _myUserManager = IoC.Get<MyUserManager>();
            _paramManager = IoC.Get<ParamManager>();
            this.usePreviousUserTypeAfterLogin = usePreviousUserTypeAfterLogin;
            UserInfos = _myUserManager.UserInfos.FindAll(t => t.Type >= userType);
            needUserType = userType;
            Start();
        }

        public async void Start()
        {
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (needUserType > 0)
                    {
                        EN_UserType currCardType = BC750.currCardInfo.userType;
                        if (currCardType >= needUserType)
                        {
                            if (!usePreviousUserTypeAfterLogin)
                            {
                                _myUserManager.Login(currCardType);
                                _myUserManager.CurrUserID = BC750.currCardInfo.cardID;
                            }
                            TryClose(true);
                            return;
                        }
                        BC750.currCardInfo.userType = 0;
                    }
                    NotifyOfPropertyChange(() => CommonLoginVisiable);
                    NotifyOfPropertyChange(() => BC750LoginVisiable);
                    NotifyOfPropertyChange(() => UserManagerVaisiable);
                    await Task.Delay(50);
                }
            });
        }

        public void DoLogin()
        {
            var previousUserType = _myUserManager.CurrUserInfo.Type;
            if (!_myUserManager.Login(UserName, Password))
            {
                MessageBox.Error("登录失败");
            }
            else
            {
                if (usePreviousUserTypeAfterLogin)
                {
                    _myUserManager.Login(previousUserType);
                }
                TryClose(true);
            }
        }

        public void Cancel()
        {
            TryClose(false);
        }

        public void HandleInput(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoLogin();
            }
        }
    }
}
