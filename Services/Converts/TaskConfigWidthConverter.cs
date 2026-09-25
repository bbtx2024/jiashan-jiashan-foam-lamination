using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QA.Pages.Converters
{
    public class TaskConfigWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                double width = (double)value;
                if (parameter != null)
                {
                    int padding =System.Convert.ToInt32(parameter);
                    if (width > padding)
                    {
                        return width - padding;
                    }
                }

                return width;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
