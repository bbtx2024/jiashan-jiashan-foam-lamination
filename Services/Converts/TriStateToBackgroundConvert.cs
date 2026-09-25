using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using QA.Business.Define;

namespace QA.Business.Converts
{
    public class TriStateToBackgroundConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((En_DeviceStatus)value == En_DeviceStatus.Connected)
            {
                return new SolidColorBrush(Color.FromArgb(125, 0, 255, 0));
            }
            else if ((En_DeviceStatus)value == En_DeviceStatus.DisConnected)
            {
                return new SolidColorBrush(Color.FromArgb(125, 255, 0, 0));
            }
            else if ((En_DeviceStatus)value == En_DeviceStatus.Disabled)
            {
                return new SolidColorBrush(Color.FromArgb(125, 133, 133, 133));
            }
            else
                return new SolidColorBrush(Color.FromArgb(125, 133, 133, 133));

            //return ((bool)value) ? new SolidColorBrush(Color.FromArgb(125, 0, 255, 0)) : new SolidColorBrush(Color.FromArgb(125, 255, 0, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
