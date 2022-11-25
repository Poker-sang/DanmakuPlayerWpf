using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using static DanmakuPlayer.Properties.Settings;

namespace DanmakuPlayer;

public partial class MainWindow
{
    public void SettingInit()
    {
        // CbTheme.SelectedIndex = Default.Theme;
        SWindowOpacity.Value = App.AppConfig.WindowOpacity;
        SFastForward.Value = App.AppConfig.PlayFastForward;
        SPlaySpeed.Value = App.AppConfig.PlaySpeed;
        SSpeed.Value = App.AppConfig.DanmakuSpeed;
        SOpacity.Value = App.AppConfig.DanmakuOpacity;
    }

    private void BDefaultClick(object sender, RoutedEventArgs e)
    {
        //switch (Control.SelectedIndex)
        //{
        //    case 0:
        //        SWindowOpacity.Value = 0.2;
        //        break;
        //    case 1:
        //        SFastForward.Value = 5;
        //        SPlaySpeed.Value = 1;
        //        break;
        //    case 2:
        //        SSpeed.Value = 15;
        //        SOpacity.Value = 0.7;
        //        break;
        //}
        //e.Cancel = true;
    }

    private bool SettingResult { get; set; }

    private void BSaveClick(object sender, RoutedEventArgs e)
    {
        App.AppConfig.WindowOpacity = Default.WindowOpacity = SWindowOpacity.Value;
        // Default.Theme = CbTheme.SelectedIndex;
        App.AppConfig.PlayFastForward = Default.PlayFastForward = (int)SFastForward.Value;
        App.AppConfig.PlaySpeed = Default.PlaySpeed = SPlaySpeed.Value;
        App.AppConfig.DanmakuSpeed = Default.DanmakuSpeed = (int)SSpeed.Value;
        App.AppConfig.DanmakuOpacity = Default.DanmakuOpacity = (float)SOpacity.Value;
        App.AppConfig.Save();
        SettingResult = true;
        _ = ((Dialog)sender).Hide();
    }

    private void CbThemeSelectionChanged(object sender, RoutedEventArgs e)
    {
        return;
        // Theme.Apply(ToTheme(((ComboBox)sender).SelectedIndex));
    }

    private static ThemeType ToTheme(int theme) =>
        theme switch
        {
            0 => (Theme.GetSystemTheme() is SystemThemeType.Dark ? ThemeType.Dark : ThemeType.Light),
            1 => ThemeType.Light,
            2 => ThemeType.Dark,
            3 => ThemeType.HighContrast,
            _ => throw new ArgumentOutOfRangeException(nameof(ComboBox.SelectedIndex)),
        };
}
