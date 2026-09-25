using System;
using System.Globalization;
using System.Windows.Data;

namespace QA.IntelligentEquipment.Converters
{
    public class AutoRunChangeColorConvert : IValueConverter
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
                    return "green";
                }
                else
                {
                    return "White";
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
