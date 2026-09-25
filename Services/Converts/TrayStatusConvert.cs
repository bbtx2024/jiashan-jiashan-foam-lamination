using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using QA.Business.Define;

namespace QA.Business.Converts
{
    public class TrayStatusToColorConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((EN_TrayStatus)value)
            {
                case EN_TrayStatus.Unuse:
                case EN_TrayStatus.Empty:
                    return Brushes.LightGray;
                case EN_TrayStatus.Common:
                case EN_TrayStatus.NeedWork:
                    return Brushes.AliceBlue;
                case EN_TrayStatus.OK:
                    return Brushes.LightGreen;
                default:
                    return Brushes.OrangeRed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TrayStatusToStringConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((EN_TrayStatus)value)
            {
                case EN_TrayStatus.Empty:
                    return "空穴";
                case EN_TrayStatus.OK:
                    return "OK";
                case EN_TrayStatus.HaveTape:
                    return "已贴合";
                case EN_TrayStatus.MarkFail://后续错误类型仅占位，未使用
                    return "Mark定位失败";
                case EN_TrayStatus.TapeNotExist2:
                    return "Flex GND Tape缺失";
                case EN_TrayStatus.TapeLxzNotExist:
                    return "Flex GND Tape小离型纸缺失";
                case EN_TrayStatus.LxzNotRemoved1:
                    return "Alert Bumper大离型纸未撕";
                case EN_TrayStatus.LxzNotRemoved2:
                    return "Flex GND Tape大离型纸未撕";
                case EN_TrayStatus.XYOverRange1:
                    return "Alert Bumper贴装偏移";
                case EN_TrayStatus.XYOverRange2:
                    return "Flex GND Tape贴装偏移";
                case EN_TrayStatus.TPTapeNotExist:
                    return "TP Tape缺失";

                case EN_TrayStatus.Unuse:
                    return "禁用";
                case EN_TrayStatus.Common:
                    return "待料";
                case EN_TrayStatus.NeedWork:
                    return "等待处理";
                case EN_TrayStatus.CameraNoLink:
                    return "视觉失连";
                case EN_TrayStatus.TakePhotoOK:
                    return "拍照完成";
                default:
                    return "未知状态";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
