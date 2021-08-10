using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using static BiliBulletScreenPlayer.Properties.Settings;

namespace BiliBulletScreenPlayer
{
	/// <summary>
	/// SettingWindow.xaml 的交互逻辑
	/// </summary>
	public partial class SettingWindow : Window
	{
		public SettingWindow(MainWindow owner)
		{
			Owner = owner;
			InitializeComponent();
			MouseLeftButtonDown += (_, _) => DragMove();
			SWindowOpacity.Value = App.WindowOpacity;
			SFastForward.Value = App.FastForward;
			SPlaySpeed.Value = App.PlaySpeed;
			SSpeed.Value = App.Speed;
			SOpacity.Value = App.Opacity;
		}

		private void BDefault_Click(object sender, RoutedEventArgs e)
		{
			switch (Control.SelectedIndex)
			{
				case 0:
					SWindowOpacity.Value = 0.2;
					break;
				case 2:
					SFastForward.Value = 5;
					SPlaySpeed.Value = 1;
					break;
				case 1:
					SSpeed.Value = 15;
					SOpacity.Value = 0.7;
					break;
			}
		}

		private void BSave_Click(object sender, RoutedEventArgs e)
		{
			Default.WindowOpacity = App.WindowOpacity = SWindowOpacity.Value;
			Default.FastForward = App.FastForward = (int)SFastForward.Value;
			Default.PlaySpeed = App.PlaySpeed = SPlaySpeed.Value;
			Default.Speed = App.Speed = (int)SSpeed.Value;
			Default.Opacity = App.Opacity = SOpacity.Value;
			Close();
		}
		private void BCancel_Click(object sender, RoutedEventArgs e) => Close();

	}
}
