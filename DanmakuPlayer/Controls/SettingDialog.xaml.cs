using System;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DanmakuPlayer.Controls;

public partial class SettingDialog : UserControl
{
    public SettingDialog() => InitializeComponent();

    public Task ShowAsync() => Dialog.ShowAndWaitAsync();

    private void CancelDialogClick(object sender, RoutedEventArgs e) => ((Dialog)sender).Hide();

    private void DefaultClick(object sender, RoutedEventArgs e)
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

    private void CbThemeSelectionChanged(object sender, RoutedEventArgs e)
    {
        return;
        // Theme.Apply(ToTheme(((ComboBox)sender).SelectedIndex));
    }

    private static ThemeType ToTheme(int theme) =>
        theme switch
        {
            0 => Theme.GetSystemTheme() is SystemThemeType.Dark ? ThemeType.Dark : ThemeType.Light,
            1 => ThemeType.Light,
            2 => ThemeType.Dark,
            3 => ThemeType.HighContrast,
            _ => throw new ArgumentOutOfRangeException(nameof(ComboBox.SelectedIndex)),
        };

    private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => App.AppConfig.Save();

    private void ComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e) => App.AppConfig.Save();

    private void ButtonOnClick(object sender, RoutedEventArgs e) => App.AppConfig.Save();

    private static string[] FontFamilies
    {
        get
        {
            using var collection = new InstalledFontCollection();
            return collection.Families.Select(t => t.Name).ToArray();
        }
    }
}
