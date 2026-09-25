using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using QA.Business.Define;

namespace QA.Business.Converts
{
    public class TriStateToConnectStringConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Collection<ResourceDictionary> appDictionaries = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary lanDictionary = appDictionaries.FirstOrDefault(t => t.Source != null && t.Source.ToString().Contains("Language"));
            if (lanDictionary == null)
                return value;

            if ((En_DeviceStatus)value == En_DeviceStatus.Connected)
            {
                return lanDictionary["连接"];
            }
            else if ((En_DeviceStatus)value == En_DeviceStatus.DisConnected)
            {
                return lanDictionary["断开"];
            }
            else if ((En_DeviceStatus)value == En_DeviceStatus.Disabled)
            {
                return lanDictionary["禁用"];
            }
            else
                return lanDictionary["Err"];
            //return ((bool)value) ? lanDictionary["连接"] : lanDictionary["断开"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
