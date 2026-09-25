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
    public class StatusToForeGroudConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush originalColor = Brushes.Transparent;
            if (value != null)
            {
                byte flag = (byte)value;
                switch (flag)
                {
                    case 1:
                        {
                            return Brushes.LawnGreen;
                        }
                    case 2:
                        {
                            return Brushes.Red;
                        }
                }
            }

            return originalColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
