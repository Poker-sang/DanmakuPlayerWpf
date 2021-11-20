﻿using System.Windows;
using ModernWpf;
using ModernWpf.Controls;
using static BiliBulletScreenPlayer.Properties.Settings;

namespace BiliBulletScreenPlayer.Control
{
    public partial class SettingDialog : ContentDialog
    {
        public SettingDialog()
        {
            ThemeManager.Current.ApplicationTheme = Default.Theme ? ApplicationTheme.Dark : ApplicationTheme.Light;
            InitializeComponent();
            SWindowOpacity.Value = App.WindowOpacity;
            TgTheme.IsOn = Default.Theme;
            SFastForward.Value = App.FastForward;
            SPlaySpeed.Value = App.PlaySpeed;
            SSpeed.Value = App.Speed;
            SOpacity.Value = App.Opacity;
        }
        public bool DialogResult { get; private set; }

        private void BDefault_Click(ContentDialog contentDialog, ContentDialogButtonClickEventArgs e)
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
            e.Cancel = true;
        }

        private void BSave_Click(ContentDialog contentDialog, ContentDialogButtonClickEventArgs e)
        {
            Default.WindowOpacity = App.WindowOpacity = SWindowOpacity.Value;
            Default.Theme = TgTheme.IsOn;
            Default.FastForward = App.FastForward = (int)SFastForward.Value;
            Default.PlaySpeed = App.PlaySpeed = SPlaySpeed.Value;
            Default.Speed = App.Speed = (int)SSpeed.Value;
            Default.Opacity = App.Opacity = SOpacity.Value;
            Default.Save();
            DialogResult = true;
            Hide();
        }
        private void BCancel_Click(ContentDialog contentDialog, ContentDialogButtonClickEventArgs e) => Hide();

        private void TgTheme_OnToggled(object sender, RoutedEventArgs e) => ThemeManager.Current.ApplicationTheme = ((ToggleSwitch)sender).IsOn ? ApplicationTheme.Dark : ApplicationTheme.Light;
    }
}
