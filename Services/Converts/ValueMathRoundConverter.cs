using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QA.Pages.Converters
{
    public class ValueMathRoundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var floatVar = System.Convert.ToDouble(value)  ;
                if (null!= parameter)
                {
                    var round =System.Convert.ToInt32(parameter) ;
                    return Math.Round(floatVar/10, round);
                }
                return floatVar;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
