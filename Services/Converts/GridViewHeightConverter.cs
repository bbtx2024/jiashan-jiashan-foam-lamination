using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QA.Pages.Converters
{
    public class GridViewHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double convertValue = 0d;
            int convertParam = 1;
            if (parameter != null)
            {
                convertParam = 4;
            }
            if (value != null)
            {
                convertValue = (double)value;
            }
            return convertValue / convertParam;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
