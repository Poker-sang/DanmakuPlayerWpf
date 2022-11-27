using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace DanmakuPlayer;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainWindow()
    {
        App.Window = this;
        InitializeComponent();
        Danmaku.ViewPort.SetTarget(BBackGround);
        BBackGround.Opacity = App.AppConfig.WindowOpacity;
        MouseLeftButtonDown += (_, _) => DragMove();
        // handledEventsToo is true 事件才会被处理
        STime.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(STimeMouseButtonDown), true);
        STime.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(STimeMouseButtonUp), true);
        App.Timer.Tick += (_, _) =>
        {
            if (Time < STime.Maximum)
            {
                if (App.Playing)
                    Time += App.AppConfig.Interval;
                DanmakuImage.Rendering((float)Time, App.AppConfig);
            }
            else
            {
                Pause();
                Time = 0;
            }
        };
        App.Timer.Start();
    }

    #region 操作

    private double Time
    {
        get => STime.Value;
        set
        {
            STime.Value = value;
            OnPropertyChanged(nameof(TimeText));
        }
    }

    private string TimeText => Time.ToTime();

    private async void SettingOpen()
    {
        _ = await DSetting.ShowAndWaitAsync();
        // BBackGround.Opacity = App.AppConfig.WindowOpacity;
        // FadeOut("设置已更新", false, "✧(≖ ◡ ≖✿)");
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
            Time = 0;
            App.LoadPool(mode ? XDocument.Load(xml) : XDocument.Parse(xml));

            STime.Maximum = App.Pool[^1].Time + 10;
            TbTotalTime.Text = "/" + STime.Maximum.ToTime();
            FadeOut($"{App.Pool.Length}条弹幕已装载", false, "(/・ω・)/");
        }
        catch (Exception)
        {
            FadeOut("━━Σ(ﾟДﾟ川)━ 不是标准弹幕文件", true, "​( ´･･)ﾉ(._.`) 你可以手动在 biliplus.com 获取");
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

    public void DanmakuReload(Action? action = null)
    {
        TryPause();
        App.Timer.Stop();

        action?.Invoke();
        App.RenderPool();

        App.Timer.Start();
        TryResume();
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
        if (App.Pool.Length is 0)
            return;

        DanmakuReload();
    }

    public void WKeyUp(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            {
                if (Time - App.AppConfig.PlayFastForward < 0)
                    Time = 0;
                else
                    Time -= App.AppConfig.PlayFastForward;
                break;
            }
            case Key.Right:
            {
                if (Time + App.AppConfig.PlayFastForward > STime.Maximum)
                    Time = 0;
                else
                    Time += App.AppConfig.PlayFastForward;
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

    private async void BImportClick(object sender, RoutedEventArgs e)
    {
        _ = await DInput.ShowAndWaitAsync();
        if (InputResult is null)
            return;
        FadeOut("弹幕装填中...", false, "(｀・ω・´)");

        try
        {
            var cidList = new List<string>();
            var http = await ("https://api.bilibili.com/x/player/pagelist?bvid=" + InputResult).DownloadJsonAsync();
            if (http.RootElement.TryGetProperty("data", out var ja))
            {
                foreach (var je in ja.EnumerateArray())
                    if (je.TryGetProperty("cid", out var cid))
                        cidList.Add(cid.GetRawText());
                // TODO: 分P
                // if (cidList.Count > 1)
                XmlOpen(await ($"http://comment.bilibili.com/{cidList[0]}.xml").DownloadStringAsync(), false);
                return;
            }
            FadeOut("视频不存在！", true, "〒_〒");
        }
        catch (Exception exception)
        {
            FadeOut(exception.Message, true, "未处理的异常");
        }

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
        InputResult = null;
        _ = ((Dialog)sender).Hide();
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion
}
