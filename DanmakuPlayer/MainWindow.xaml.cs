﻿using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using Microsoft.Win32;
using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace DanmakuPlayer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        App.Window = this;
        InitializeComponent();
        SettingInit();
        Danmaku.ViewPort.SetTarget(BBackGround);
        BBackGround.Opacity = App.AppConfig.WindowOpacity;
        MouseLeftButtonDown += (_, _) => DragMove();
        // handledEventsToo is true 事件才会被处理
        STime.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(STimeMouseButtonDown), true);
        STime.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(STimeMouseButtonUp), true);
        App.Timer.Tick += (_, _) =>
        {
            if (STime.Value < STime.Maximum)
            {
                if (App.Playing)
                    STime.Value += App.AppConfig.Interval;
                DanmakuImage.Rendering((float)STime.Value, App.AppConfig);
            }
            else
            {
                Pause();
                STime.Value = 0;
            }
        };
        App.Timer.Start();
    }

    #region 操作

    private async void SettingOpen()
    {
        _ = await DSetting.ShowAndWaitAsync();
        if (!SettingResult)
            return;
        BBackGround.Opacity = App.AppConfig.WindowOpacity;
        FadeOut("设置已更新", false, "✧(≖ ◡ ≖✿)");
    }
    
    /// <summary>
    /// 加载xml文件
    /// </summary>
    /// <param name="xml">xml文件路径或字符串</param>
    /// <param name="mode"><see langword="true"/>为路径，<see langword="false"/>为字符串</param>
    private void XmlOpen(string xml, bool mode)
    {
        try
        {
            Pause();
            STime.Maximum = 0;
            STime.Value = 0;
            App.LoadPool(mode ? XDocument.Load(xml) : XDocument.Parse(xml));

            STime.Maximum = App.Pool[^1].Time + 10;
            TbTotalTime.Text = "/" + STime.Maximum.ToTime();
            STime.Value = 0;
            FadeOut("弹幕已装载", false, "(/・ω・)/");
        }
        catch (Exception)
        {
            FadeOut("━━Σ(ﾟДﾟ川)━ 不是标准弹幕文件", true, "​( ´･･)ﾉ(._.`) 你可以在 biliplus.com 获取");
        }

        if (TbBanner is not null)
            Grid.Children.Remove(TbBanner);
        BControl.IsHitTestVisible = true;
    }

    /// <summary>
    /// 出现信息并消失
    /// </summary>
    /// <param name="message">提示信息</param>
    /// <param name="isError"><see langword="true"/>为错误信息，<see langword="false"/>为提示信息</param>
    /// <param name="hint">信息附加内容</param>
    /// <param name="mSec">信息持续时间</param>
    private void FadeOut(string message, bool isError, string? hint = null, int mSec = 3000)
    {
        RootSnackBar.Title = message;
        RootSnackBar.Timeout = mSec;
        if (isError)
        {
            RootSnackBar.Message = hint ?? "错误";
            RootSnackBar.Icon = SymbolRegular.Important24;
            RootSnackBar.Appearance = ControlAppearance.Danger;
        }
        else
        {
            RootSnackBar.Message = hint ?? "提示";
            RootSnackBar.Icon = SymbolRegular.Info24;
            RootSnackBar.Appearance = ControlAppearance.Transparent;
        }

        _ = RootSnackBar.Show();
    }

    #region 播放及暂停

    private bool _needResume;

    private void TryPause()
    {
        _needResume = App.Playing;
        Pause();
    }

    private void TryResume()
    {
        if (_needResume)
            Resume();
        _needResume = false;
    }

    private void Resume()
    {
        App.Playing = true;
        BPauseResume.Icon = SymbolRegular.Pause24;
    }

    private void Pause()
    {
        App.Playing = false;
        BPauseResume.Icon = SymbolRegular.Play24;
    }

    #endregion

    #endregion

    #region 事件

    private void WDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount is not 2)
            return;
        WindowState = WindowState is WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        DanmakuImage.CancelRender = true;
    }

    private void WSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if(App.Pool.Length is 0)
            return;

        TryPause();
        App.Timer.Stop();

        App.RenderPool();

        App.Timer.Start();
        TryResume();
    }

    private void WKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            {
                if (STime.Value - App.AppConfig.PlayFastForward < 0)
                    STime.Value = 0;
                else
                    STime.Value -= App.AppConfig.PlayFastForward;
                break;
            }
            case Key.Right:
            {
                if (STime.Value + App.AppConfig.PlayFastForward > STime.Maximum)
                    STime.Value = 0;
                else
                    STime.Value += App.AppConfig.PlayFastForward;
                break;
            }
            case Key.Space:
            {
                PauseResumeClick(null!, null!);
                break;
            }
            case Key.Tab:
            {
                SettingOpen();
                break;
            }
            default: break;
        }
    }

    private void WDragEnter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;

    private void WDrop(object sender, DragEventArgs e) => XmlOpen(((Array)e.Data.GetData(DataFormats.FileDrop)!).GetValue(0)!.ToString()!, true);

    [GeneratedRegex("\"cid\":([0-9]+),")]
    private static partial Regex MyRegex();

    private async void BImportClick(object sender, RoutedEventArgs e)
    {
        _ = await DInput.ShowAndWaitAsync();
        if (InputResult is null)
            return;
        FadeOut("弹幕装填中...", false, "(｀・ω・´)");

        var xmlString = "";
        try
        {
            var http = await new HttpClient().GetStringAsync("https://www.biliplus.com/video/" + InputResult);
            var xmlUri = @"http://comment.bilibili.com/";
            if (MyRegex().Match(http) is { Success: true } match)
                xmlUri += match.Groups[1].Value + ".xml";
            else
            {
                FadeOut("视频不存在！", true, "〒_〒");
                return;
            }

            xmlString = await new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.Deflate }).GetStringAsync(xmlUri);
        }
        catch (Exception exception)
        {
            FadeOut(exception.Message, true);
        }

        XmlOpen(xmlString, false);
    }

    private void BFileClick(object sender, RoutedEventArgs e)
    {
        var fileDialog = new OpenFileDialog
        {
            Title = "选择弹幕文件",
            Filter = "弹幕文件|*.xml",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            ValidateNames = true,
            CheckPathExists = true,
            CheckFileExists = true
        };
        _ = fileDialog.ShowDialog();
        if (fileDialog.FileName is not "")
            XmlOpen(fileDialog.FileName, true);
    }

    private void BFrontClick(object sender, RoutedEventArgs e)
    {
        if (Topmost)
        {
            Topmost = false;
            ((Button)sender).Icon = SymbolRegular.Pin24;
            FadeOut("固定上层：关闭", false, "(°∀°)ﾉ");
        }
        else
        {
            Topmost = true;
            ((Button)sender).Icon = SymbolRegular.PinOff24;
            FadeOut("固定上层：开启", false, "(・ω< )★");
        }
    }

    private void BSettingClick(object sender, RoutedEventArgs e) => SettingOpen();

    private void BCloseClick(object sender, RoutedEventArgs e) => Close();

    private void PauseResumeClick(object sender, RoutedEventArgs e)
    {
        if (App.Playing)
            Pause();
        else
            Resume();
    }

    private void STimeMouseButtonDown(object sender, MouseButtonEventArgs e) => TryPause();

    private void STimeMouseButtonUp(object sender, MouseButtonEventArgs e) => TryResume();

    private void BFileMouseEnter(object sender, MouseEventArgs e) => SpImportButtons.Visibility = Visibility.Visible;

    private void BFileMouseLeave(object sender, MouseEventArgs e) => SpImportButtons.Visibility = Visibility.Hidden;

    private void BButtonsMouseEnter(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Visible;

    private void BButtonsMouseLeave(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Hidden;

    private void BControlMouseEnter(object sender, MouseEventArgs e) => DpControl.Visibility = Visibility.Visible;

    private void BControlMouseLeave(object sender, MouseEventArgs e) => DpControl.Visibility = Visibility.Hidden;

    private void BCancelDialogClick(object sender, RoutedEventArgs e)
    {
        SettingResult = false;
        InputResult = null;
        _ = ((Dialog)sender).Hide();
    }

    #endregion
}
