using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using QA.Business.Define;

namespace QA.Pages.Converters
{
    public class TaskConfigReadOnlyConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEnable = false;
            if (value != null)
            {
                var convertVal = (FunctionCode)value;
                if (parameter != null)
                {
                    var type = parameter.ToString();
                    switch (convertVal)
                    {
                        case FunctionCode.放料点:
                        case FunctionCode.上相机点:
                            {
                                if (type.Equals("CavityNo"))
                                {
                                    isEnable = true;
                                }
                                break;
                            }
                        case FunctionCode.取料点:
                            {
                                if (type.Equals("GrabLocation"))
                                {
                                    isEnable = true;
                                }
                                break;
                            }
                    }

                    if (type.Equals("GrabHandNo"))
                    {
                        if (convertVal != FunctionCode.上相机点)
                        {
                            isEnable = true;
                        }
                    }
                }
            }

            return isEnable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
