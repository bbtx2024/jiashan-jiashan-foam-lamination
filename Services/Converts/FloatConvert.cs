using System;
using System.Globalization;
using System.Windows.Data;

namespace QA.Business.Converts
{
    public class FloatToPercentConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"{((float)value * 100.0f).ToString("F2")}%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
