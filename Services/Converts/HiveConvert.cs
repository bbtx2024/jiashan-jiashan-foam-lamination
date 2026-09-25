using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Caliburn.Micro;
using QA.Business.Component.HIVE;
using QA.Business.Define;
using QA.Business.Interfaces;

namespace QA.Business.Converts
{
    /// <summary>
    /// 顶栏背景色，与HiveStatusToColorConvert相比，在1 2两种情况下不变色
    /// </summary>
    public class HiveStatusToTopColorConvert : IValueConverter
    {
        public static readonly BrushConverter brushConverter = new BrushConverter();

        //(Brush) brushConverter.ConvertFromString("#8b8c8d"),
        //(Brush) brushConverter.ConvertFromString("#8b8c8d"),
        //(Brush)brushConverter.ConvertFromString("#ccabd8"),
        //(Brush)brushConverter.ConvertFromString("#ffd7d4"),
        //(Brush)brushConverter.ConvertFromString("#eb7373"),

        Hive_Component _hive_Component = (Hive_Component)IoC.Get<IHive>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((EN_HiveStatus)value)
            {
                case EN_HiveStatus.Running:
                case EN_HiveStatus.Idle:
                    return (Brush)brushConverter.ConvertFromString("#8b8c8d");
                default:
                    return _hive_Component.ErrCode.StartsWith("PD-") ? Brushes.Blue : Brushes.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HiveStatusToColorConvert : IValueConverter
    {
        public static readonly BrushConverter brushConverter = new BrushConverter();

        //(Brush)brushConverter.ConvertFromString("#d3eb73"),
        //(Brush)brushConverter.ConvertFromString("#bfffff"),
        //(Brush)brushConverter.ConvertFromString("#ccabd8"),
        //(Brush)brushConverter.ConvertFromString("#ffd7d4"),
        //(Brush)brushConverter.ConvertFromString("#eb7373"),

        Hive_Component _hive_Component = (Hive_Component)IoC.Get<IHive>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((EN_HiveStatus)value)
            {
                case EN_HiveStatus.Running:
                    return (Brush)brushConverter.ConvertFromString("#00FF00");
                case EN_HiveStatus.Idle:
                    return Brushes.Yellow;
                default:
                    return _hive_Component.ErrCode.StartsWith("PD-") ? Brushes.Blue : Brushes.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HiveStatusToStringConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((EN_HiveStatus)value)
            {
                case EN_HiveStatus.Running:
                    return "Running";
                case EN_HiveStatus.Idle:
                    return "Idle";
                case EN_HiveStatus.Engineering:
                case EN_HiveStatus.PlannedDowntime:
                case EN_HiveStatus.Downtime:
                    return "Downtime";
                default:
                    return "Unknown";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
