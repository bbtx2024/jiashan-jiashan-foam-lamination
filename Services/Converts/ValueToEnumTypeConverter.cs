using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
//using Com.JiaShan.Model.Enum.PLC;

namespace QA.Pages.Converters
{
    public class ValueToEnumTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var enumVal = System.Convert.ToInt32(value);
                if (null != parameter)
                {
                    string paramStr = parameter.ToString();
//                     switch (paramStr)
//                     {
//                         case "FunctionCode":
//                             {
//                                 return Enum.GetName(typeof(FunctionCode), enumVal);
//                             }
//                         case "GrabHandNo":
//                             {
//                                 return Enum.GetName(typeof(HandNo), enumVal);
//                             }
//                         case "Mode":
//                             {
//                                 return Enum.GetName(typeof(RunMode), enumVal);
//                             }
//                     }
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var enumVal = value.ToString();
                if (null != parameter)
                {
                    string paramStr = parameter.ToString();
//                     switch (paramStr)
//                     {
//                         case "FunctionCode":
//                             {
//                                 return Enum.Parse(typeof(FunctionCode), enumVal);
//                             }
//                         case "GrabHandNo":
//                             {
//                                 return Enum.Parse(typeof(HandNo), enumVal);
//                             }
//                         case "Mode":
//                             {
//                                 return Enum.Parse(typeof(RunMode), enumVal);
//                             }
//                     }
                }
            }

            return value;
        }
    }
}
