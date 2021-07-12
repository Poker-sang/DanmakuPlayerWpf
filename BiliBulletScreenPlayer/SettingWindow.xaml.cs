using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Configuration;
using System.Diagnostics;

namespace BiliBulletScreenPlayer
{
	/// <summary>
	/// SettingWindow.xaml 的交互逻辑
	/// </summary>
	public partial class SettingWindow : Window
	{
		public SettingWindow(SaveSettings @default)
		{
			InitializeComponent();
			BaseCanvas.MouseLeftButtonDown += (o, e) => { DragMove(); };
			Initialize(@default);
		}

		private void Initialize(SaveSettings save)
		{
			SWindowOpacity.Value = save.WindowOpacity * 10;
			TBWindowOpacity.Text = save.WindowOpacity.ToString();
			SSpeed.Value = save.Speed;
			TBSpeed.Text = save.Speed.ToString();
			SOpacity.Value = save.Opacity * 10;
			TbOpacity.Text = save.Opacity.ToString();
			switch (save.Ratio)
			{
				case 0.5: Ratio05.IsChecked = true; break;
				case 0.75: Ratio075.IsChecked = true; break;
				case 1: Ratio1.IsChecked = true; break;
				case 1.25: Ratio125.IsChecked = true; break;
				case 1.5: Ratio15.IsChecked = true; break;
				case 2: Ratio2.IsChecked = true; break;
			}
			switch (save.Range)
			{
				case 1: Range1.IsChecked = true; break;
				case 3: Range3.IsChecked = true; break;
				case 5: Range5.IsChecked = true; break;
				case 10: Range10.IsChecked = true; break;
				case 15: Range15.IsChecked = true; break;
				case 20: Range20.IsChecked = true; break;
			}
		}
		public delegate void EndThis(SaveSettings settings);
		public EndThis SaveSettings = null;

		private void BDefault_Click(object sender, RoutedEventArgs e) => Initialize(new SaveSettings() { Speed = 5, Range = 5, Opacity = 0.7, Ratio = 1, WindowOpacity = 0.2 });
		private void BSave_Click(object sender, RoutedEventArgs e)
		{
			double ratio = 0;
			if ((bool)Ratio05.IsChecked!)
				ratio = 0.5;
			else if ((bool)Ratio075.IsChecked!)
				ratio = 0.75;
			else if ((bool)Ratio1.IsChecked!)
				ratio = 1;
			else if ((bool)Ratio125.IsChecked!)
				ratio = 1.25;
			else if ((bool)Ratio15.IsChecked!)
				ratio = 1.5;
			else if ((bool)Ratio2.IsChecked!)
				ratio = 2;
			var range = 0;
			if ((bool)Range1.IsChecked!)
				range = 1;
			else if ((bool)Range3.IsChecked!)
				range = 3;
			else if ((bool)Range5.IsChecked!)
				range = 5;
			else if ((bool)Range10.IsChecked!)
				range = 10;
			else if ((bool)Range15.IsChecked!)
				range = 15;
			else if ((bool)Range20.IsChecked!)
				range = 20;

			var temp= new SaveSettings() { Speed = (int)SSpeed.Value, Range = range, Opacity = SOpacity.Value / 10, Ratio = ratio, WindowOpacity = SWindowOpacity.Value / 10 };
			var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			config.AppSettings.Settings.Clear();
			config.AppSettings.Settings.Add("Speed", temp.Speed.ToString());
			config.AppSettings.Settings.Add("Range", temp.Range.ToString());
			config.AppSettings.Settings.Add("Opacity", temp.Opacity.ToString());
			config.AppSettings.Settings.Add("Ratio", temp.Ratio.ToString());
			config.AppSettings.Settings.Add("WindowOpacity", temp.WindowOpacity.ToString());
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("appSettings");

			SaveSettings(temp);
			Close();
		}
		private void BCancel_Click(object sender, RoutedEventArgs e) => Close();

		private void SWindowOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) 
		{
			SWindowOpacity.Value = Math.Round(SWindowOpacity.Value, 0);
			TBWindowOpacity.Text = (SWindowOpacity.Value / 10).ToString();
		}
		private void SSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			SSpeed.Value = Math.Round(SSpeed.Value, 0);
			TBSpeed.Text = SSpeed.Value.ToString();
		}
		private void SOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			SOpacity.Value = Math.Round(SOpacity.Value, 0);
			TbOpacity.Text = (SOpacity.Value / 10).ToString();
		}

		private void Ratio_Click(object sender, RoutedEventArgs e)
		{
			Ratio05.IsChecked = false;
			Ratio075.IsChecked = false;
			Ratio1.IsChecked = false;
			Ratio125.IsChecked = false;
			Ratio15.IsChecked = false;
			Ratio2.IsChecked = false;
			((CheckBox)sender).IsChecked = true;
		}

		private void Range_Click(object sender, RoutedEventArgs e)
		{
			Range1.IsChecked = false;
			Range3.IsChecked = false;
			Range5.IsChecked = false;
			Range10.IsChecked = false;
			Range15.IsChecked = false;
			Range20.IsChecked = false;
			((CheckBox)sender).IsChecked = true;
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("cmd", $"/c start https://github.com/Poker-sang/BiliBulletScreenPlayer") { CreateNoWindow = true });
	}
}
