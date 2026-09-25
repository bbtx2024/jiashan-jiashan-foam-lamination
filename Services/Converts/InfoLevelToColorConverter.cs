using System;
using System.Globalization;
using System.Windows.Data;
using QA_Infrastructure;

namespace QA.Business.Converts
{
    public class InfoLevelToColorConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "black";
            }
            else
            {
                if ((EN_WARN_LEVEL)value == EN_WARN_LEVEL.Sucess)
                {
                    return "green";
                }
                else if ((EN_WARN_LEVEL)value == EN_WARN_LEVEL.Error || (EN_WARN_LEVEL)value == EN_WARN_LEVEL.CriticalError)
                {
                    return "red";
                }
                else if ((EN_WARN_LEVEL)value == EN_WARN_LEVEL.Warn)
                {
                    return "Orange";
                }
                else
                    return "black";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
