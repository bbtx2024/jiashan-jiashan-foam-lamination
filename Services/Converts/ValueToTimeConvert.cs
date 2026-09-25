using System;
using System.Globalization;
using System.Windows.Data;

namespace QA.Business.Converts
{
    public class FloatToSecondConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"{((float)value).ToString("F3")}s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
