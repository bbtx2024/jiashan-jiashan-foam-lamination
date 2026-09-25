using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using QA.Business.Define;
using QA.Business.Model;

namespace QA.Pages.Converters
{
    public class StringsToUserInfoMultiConvert : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != values && values.Length == 3)
            {
                return new UserInfo()
                {
                    Type = (UserType)Enum.Parse(typeof(UserType), values[0].ToString()),
                    UserName = values[1].ToString(),
                    Password = values[2].ToString()
                };
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
