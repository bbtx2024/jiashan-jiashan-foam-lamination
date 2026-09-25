using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace QA.Pages.Converters
{
    public class ComponentStateToBackGroudConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush solidColorBrush = Brushes.Gray;
            if (null != values)
            {
                bool isConnect = (bool)values[0];
                bool isUse = (bool)values[1];

                if (isUse)
                {
                    if (isConnect)
                    {
                        return Brushes.LawnGreen;
                    }
                    return Brushes.Red;
                }
            }

            return solidColorBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
