using System;
using System.Globalization;
using System.Windows.Data;

namespace QA.IntelligentEquipment.Converters
{
    public class AlarmChangeColorConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "black";
            }
            else
            {
                if ((bool)value == true)
                {
                    return "White";
                }
                else
                {
                    return "red";
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
