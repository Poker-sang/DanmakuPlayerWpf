using ModernWpf;
using ModernWpf.Controls;
using System.Windows;
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
			ThemeManager.Current.ApplicationTheme = Default.Theme ? ApplicationTheme.Dark : ApplicationTheme.Light;
			InitializeComponent();
			MouseLeftButtonDown += (_, _) => DragMove();
			SWindowOpacity.Value = App.WindowOpacity;
			TgTheme.IsOn = Default.Theme;
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
				case 1:
					SFastForward.Value = 5;
					SPlaySpeed.Value = 1;
					break;
				case 2:
					SSpeed.Value = 15;
					SOpacity.Value = 0.7;
					break;
			}
		}

		private void BSave_Click(object sender, RoutedEventArgs e)
		{
			Default.WindowOpacity = App.WindowOpacity = SWindowOpacity.Value;
			Default.Theme = TgTheme.IsOn;
			Default.FastForward = App.FastForward = (int)SFastForward.Value;
			Default.PlaySpeed = App.PlaySpeed = SPlaySpeed.Value;
			Default.Speed = App.Speed = (int)SSpeed.Value;
			Default.Opacity = App.Opacity = SOpacity.Value;
			Default.Save();
			DialogResult = true;
			Close();
		}
		private void BCancel_Click(object sender, RoutedEventArgs e) => Close();

		private void TgTheme_OnToggled(object sender, RoutedEventArgs e) => ThemeManager.Current.ApplicationTheme = ((ToggleSwitch)sender).IsOn ? ApplicationTheme.Dark : ApplicationTheme.Light;
	}
}
