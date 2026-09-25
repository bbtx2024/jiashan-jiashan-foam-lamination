using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using HandyControl.Controls;
using QA.Business.Manager;
using QA_Infrastructure;

namespace QA.UserControls.ViewModels
{
    public class UserManageViewModel : Screen
    {
        private IWindowManager _windowManager;
        private IEventAggregator _eventAggregator;
        private MyUserManager _myUserManager;
        public override string DisplayName { get; set; } = "用户管理";

        public MyUserManager MyUserManagerProperty { get => _myUserManager; }

        public ObservableCollection<UserInfo> UserInfos { get; set; } = new ObservableCollection<UserInfo>();

        private UserInfo _sltUserInfo;
        public UserInfo SltUserInfo
        {
            get { return _sltUserInfo; }
            set { _sltUserInfo = value; NotifyOfPropertyChange(() => SltUserInfo); }
        }

        public List<EN_UserType> UserTypeList { get; set; }

        private EN_UserType _currUserType = EN_UserType.User;
        public EN_UserType CurrUserType
        {
            get { return _currUserType; }
            set { _currUserType = value; NotifyOfPropertyChange(() => CurrUserType); }
        }

        private string _currUserName = "Operator";
        public string CurrUserName
        {
            get { return _currUserName; }
            set { _currUserName = value; NotifyOfPropertyChange(() => CurrUserName); }
        }

        private string _currPassword = "";
        public string CurrPassword
        {
            get { return _currPassword; }
            set { _currPassword = value; NotifyOfPropertyChange(() => CurrPassword); }
        }

        [ImportingConstructor]
        public UserManageViewModel()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _myUserManager = IoC.Get<MyUserManager>();
            UserTypeList = new List<EN_UserType>()
            {
                EN_UserType.User,
                EN_UserType.SpotCheck,
                EN_UserType.Technologist,
                EN_UserType.System,
                EN_UserType.Admin
            };
            UpdateUserInfos();
        }

        /// <summary>
        /// 刷新所有用户信息
        /// </summary>
        public void UpdateUserInfos()
        {
            UserInfos.Clear();
            foreach (UserInfo info in _myUserManager.UserInfos)
            {
                UserInfos.Add(info.DeepCopy());
            }
        }

        public void SelectUserChange()
        {
            CurrUserType = SltUserInfo.Type;
            CurrUserName = SltUserInfo.UserName;
            CurrPassword = "";
        }

        /// <summary>
        /// 重置所有账号
        /// </summary>
        public void ResetAccounts()
        {
            _myUserManager.ResetAccounts();
            UpdateUserInfos();
        }

        /// <summary>
        /// 添加账号
        /// </summary>
        public void AddAccount()
        {
            if (CurrPassword == "")
            {
                MessageBox.Warning("密码不能为空！");
                return;
            }
            _myUserManager.AddUser(CurrUserName, CurrPassword, CurrUserType);
            UpdateUserInfos();
        }

        /// <summary>
        /// 修改账号
        /// </summary>
        public void ModifyAccount()
        {
            if (SltUserInfo.Type == EN_UserType.None)
            {
                MessageBox.Warning("需要先选中要修改的用户！");
                return;
            }
            if (CurrPassword == "")
            {
                MessageBox.Warning("密码不能为空！");
                return;
            }
            _myUserManager.DeleteUser(SltUserInfo.UserName);
            _myUserManager.AddUser(CurrUserName, CurrPassword, CurrUserType);
            UpdateUserInfos();
        }

        /// <summary>
        /// 删除账号
        /// </summary>
        public void DeleteAccount()
        {
            _myUserManager.DeleteUser(SltUserInfo.UserName);
            UpdateUserInfos();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow()
        {
            this.TryClose();
        }
    }
}
