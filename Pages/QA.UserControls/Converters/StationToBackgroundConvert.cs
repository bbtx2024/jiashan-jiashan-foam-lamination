using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QA.UserControls.Converters
{
    public class StationToBackgroundConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value == int.Parse(parameter.ToString()))
            {
                return new SolidColorBrush(Color.FromRgb(0x00, 0x8D, 0x86));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
