using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QA.IntelligentEquipment.Converters
{
    public class TypeToColorConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromRgb(211, 211, 211));
            if (value != null)
            {
                int flag = (int)value;
                if (flag != 0)
                {
                    solidColorBrush.Color = Color.FromRgb(255, 0, 0);
                }
            }

            return solidColorBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
