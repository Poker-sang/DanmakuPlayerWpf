using System;
using System.Globalization;
using System.Windows.Data;
using BiliBulletScreenPlayer.Service;
using static System.Convert;

namespace BiliBulletScreenPlayer.Converter
{
    public class SliderToTextBlock : IValueConverter
    {
        public object Convert(object sliderValue, Type targetType, object parameter, CultureInfo culture) => ToDouble(sliderValue).ToTime();
        public object ConvertBack(object textBoxText, Type targetType, object parameter, CultureInfo culture) => null;
    }
}