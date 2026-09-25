using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Message;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Manager
{
    public class MyUserManager
    {
        public static readonly string userNameOperator = "Operator";
        public static readonly string userNameEngineer = "Engineer";
        public static readonly string userNameManager = "Manager";
        public static readonly EN_UserType userTypeOperator = EN_UserType.User;
        public static readonly EN_UserType userTypeEngineer = EN_UserType.Technologist;
        public static readonly EN_UserType userTypeManager = EN_UserType.Admin;
        public static string TypeToString(EN_UserType userType)
        {
            if (userType == userTypeOperator)
            {
                return userNameOperator;
            }
            else if (userType == userTypeEngineer)
            {
                return userNameEngineer;
            }
            else if (userType == userTypeManager)
            {
                return userNameManager;
            }
            else
            {
                return "UnKnown";
            }
        }

        private UserManager _userManager;
        private readonly IEventAggregator _eventAggregator;
        public bool IsLoginDialogShowing = false;
        public UserInfo CurrUserInfo { get; set; }
        public string CurrUserID { get; set; }
        public List<UserInfo> UserInfos { get => _userManager.CurUserInfos; }

        public MyUserManager()
        {
            _userManager = UserManager.GetSingleton();
            _eventAggregator = IoC.Get<IEventAggregator>();
            //检测三个用户是否都存在，如果不对则需要重置所有用户
            bool needResetAccounts = _userManager.CurUserInfos.Count != 3;
            if (!needResetAccounts)
            {
                bool containsOperator = false;
                foreach (var user in _userManager.CurUserInfos)
                {
                    if (user.Type == userTypeOperator && user.UserName == userNameOperator)
                    {
                        containsOperator = true;
                        break;
                    }
                }
                bool containsEngineer = false;
                foreach (var user in _userManager.CurUserInfos)
                {
                    if (user.Type == userTypeEngineer && user.UserName == userNameEngineer)
                    {
                        containsEngineer = true;
                        break;
                    }
                }
                bool containsManager = false;
                foreach (var user in _userManager.CurUserInfos)
                {
                    if (user.Type == userTypeManager && user.UserName == userNameManager)
                    {
                        containsManager = true;
                        break;
                    }
                }
                if (!containsOperator || !containsEngineer || !containsManager)
                {
                    needResetAccounts = true;
                }
            }
            if (needResetAccounts)
            {
                ResetAccounts();
            }
        }

        /// <summary>
        /// 重置所有账号
        /// </summary>
        public void ResetAccounts()
        {
            while (_userManager.CurUserInfos.Count > 0)
            {
                _userManager.DeleteUser(_userManager.CurUserInfos[0].UserName);
            }
            _userManager.AddUser(userNameOperator, "1", userTypeOperator);
            _userManager.AddUser(userNameEngineer, "1", userTypeEngineer);
            _userManager.AddUser(userNameManager, "1", userTypeManager);
        }

        public void Login(string userName)
        {
            //此处需要添加自动解析密码代码，然后调用登录
            Login(userName, "1");
        }

        public void Login(EN_UserType userType)
        {
            //此处需要添加自动解析密码代码，然后调用登录
            if (userType == userTypeOperator)
            {
                Login(userNameOperator, "1");
            }
            else if (userType == userTypeEngineer)
            {
                Login(userNameEngineer, "1");
            }
            else if (userType == userTypeManager)
            {
                Login(userNameManager, "1");
            }
        }

        public bool Login(string userName, string password)
        {
            bool ret = _userManager.Login(userName, password);
            if (ret)
            {
                foreach (var user in UserInfos)
                {
                    if (user.UserName == userName)
                    {
                        CurrUserInfo = user;
                    }
                }
                PublishLoginSuccessMessage();
                NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"已登录，权限{TypeToString(CurrUserInfo.Type)}", En_Logout_Type.SystemParam, true);
            }
            return ret;
        }

        public bool AddUser(string userName, string password, EN_UserType userType)
        {
            return _userManager.AddUser(userName, password, userType);
        }

        public bool DeleteUser(string userName)
        {
            return _userManager.DeleteUser(userName);
        }

        public void PublishLoginSuccessMessage()
        {
            _eventAggregator.Publish(new LoginSuccessMessage(), action => { Task.Run(action); });
        }
    }
}
