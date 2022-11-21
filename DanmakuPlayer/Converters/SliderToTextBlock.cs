using DanmakuPlayer.Services;
using System;
using System.Globalization;
using System.Windows.Data;
using static System.Convert;

namespace DanmakuPlayer.Converters;

public class SliderToTextBlock : IValueConverter
{
    public object Convert(object sliderValue, Type targetType, object parameter, CultureInfo culture) => ToDouble(sliderValue).ToTime();
    public object ConvertBack(object textBoxText, Type targetType, object parameter, CultureInfo culture) => null!;
}
