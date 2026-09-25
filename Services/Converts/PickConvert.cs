using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using QA.Business.Define;

namespace QA.Business.Converts
{
    public class PickOpportunityToBackgroundConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush(Color.FromArgb(125, 0, 255, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PickOpportunityToEnableStringConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Collection<ResourceDictionary> appDictionaries = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary lanDictionary = appDictionaries.FirstOrDefault(t => t.Source != null && t.Source.ToString().Contains("Language"));
            if (lanDictionary == null)
                return value;
            switch ((EN_PickOpportunity)value)
            {
                case EN_PickOpportunity.AfterStart:
                    return lanDictionary["启动后"];
                case EN_PickOpportunity.AfterUpCam:
                    return lanDictionary["上视觉结束后"];
                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OncePickNumToBackgroundConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush(Color.FromArgb(125, 0, 255, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OncePickNumToEnableStringConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((EN_OncePickNum)value)
            {
                case EN_OncePickNum.One:
                    return "1";
                case EN_OncePickNum.Two:
                    return "2";
                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
