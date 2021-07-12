using System;
using System.Globalization;
using System.Windows.Data;
using static System.Convert;

namespace BiliBulletScreenPlayer
{
	public class SliderToTextBlock : IValueConverter
	{
		public object Convert(object sliderValue, Type targetType, object parameter, CultureInfo culture) => App.SecToTime(ToDouble(sliderValue));
		public object ConvertBack(object textBoxText, Type targetType, object parameter, CultureInfo culture) => null;
	}
}