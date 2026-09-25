using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using QA_Infrastructure;

namespace QA.Pages.Converters
{
    public class DecryptPasswordConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var convertVal = (string)value;
                return SoftSecurity.MD5Decrypt(convertVal);
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var convertVal = (string)value;
                if (!string.IsNullOrEmpty(convertVal))
                {
                    return SoftSecurity.MD5Encrypt(convertVal);
                }
            }

            return "";
        }
    }
}
