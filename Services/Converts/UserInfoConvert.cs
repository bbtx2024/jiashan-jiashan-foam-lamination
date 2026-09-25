using System;
using System.Globalization;
using System.Windows.Data;
using QA_Infrastructure;

namespace QA.Business.Converts
{
    public class UserInfoToUserNameConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UserInfo userInfo = (UserInfo)value;
            return userInfo.UserName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
