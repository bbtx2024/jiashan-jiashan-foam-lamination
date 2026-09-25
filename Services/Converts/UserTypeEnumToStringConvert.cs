using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using QA.Business.Define;

namespace QA.Pages.Converters
{
    public class UserTypeEnumToStringConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var userInfo = (UserType)value;
                return Enum.GetName(typeof(UserType), userInfo);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var userInfo = (string)value;
                return Enum.Parse(typeof(UserType), userInfo);
            }
            return null;
        }
    }
}
