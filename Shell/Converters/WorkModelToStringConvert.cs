using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using QA.Business.Define;

namespace QA.IntelligentEquipment.Converters
{
    //public class WorkModelToStringConvert : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        try
    //        {
    //            Collection<ResourceDictionary> appDictionaries = Application.Current.Resources.MergedDictionaries;
    //            ResourceDictionary lanDictionary = appDictionaries.FirstOrDefault(t => t.Source != null && t.Source.ToString().Contains("Language"));
    //            if (lanDictionary == null)
    //                return "";

    //            if ((WorkMode)value == WorkMode.Product)
    //            {
    //                return lanDictionary["生产模式"];
    //            }
    //            else if ((WorkMode)value == WorkMode.Debug)
    //            {
    //                return lanDictionary["调试模式"];
    //            }
    //            else if ((WorkMode)value == WorkMode.PlannedDT)
    //            {
    //                return lanDictionary["计划停机模式"];
    //            }
    //            else
    //            {
    //                return "";
    //            }
    //        }
    //        catch (System.Exception)
    //        {
    //            return "";
    //        }
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
