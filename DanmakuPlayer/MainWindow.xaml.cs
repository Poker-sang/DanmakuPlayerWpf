using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;

// TODO: 减少内存占用
// TODO: 改变窗口大小
// TODO: 设置同屏弹幕上限
namespace DanmakuPlayer;

public partial class MainWindow : Window
{
    #region 操作

    public MainWindow()
    {
        InitializeComponent();
        SettingInit();
        Danmaku.ViewPort.SetTarget(BBackGround);
        BBackGround.Opacity = AppContext.WindowOpacity;
        MouseLeftButtonDown += (_, _) => DragMove();
        // handledEventsToo is true 事件才会被处理
        STime.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(STimeMouseButtonDown), true);
        STime.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(STimeMouseButtonUp), true);
        App.Timer.Tick += (_, _) =>
        {
            if (STime.Value < STime.Maximum)
            {
                if (App.Playing)
                    STime.Value += App.Interval;
                DanmakuImage.Rendering((float)STime.Value);
            }
            else
            {
                Pause();
                STime.Value = 0;
            }
        };
        App.Timer.Start();
    }

    private async void SettingOpen()
    {
        _ = await DSetting.ShowAndWaitAsync();
        if (!SettingResult)
            return;
        BBackGround.Opacity = AppContext.WindowOpacity;
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
            App.ClearPool();

            var xDoc = mode ? XDocument.Load(xml) : XDocument.Parse(xml);
            var tempPool = xDoc.Element("i")!.Elements("d");
            var context = new DanmakuContext();
            App.Pool = tempPool
                .Select(Danmaku.CreateDanmaku)
                .Where(t => t.Mode < 6)
                .OrderBy(t => t.Time)
                .Where(t => t.RenderInit(DanmakuImage.D2dContext, context))
                .ToArray();

            STime.Maximum = App.Pool[^1].Time + 10;
            TbTotalTime.Text = "/" + STime.Maximum.ToTime();
            STime.Value = 0;
            FadeOut("打开文件", false, "(￣3￣)");
        }
        catch (Exception)
        {
            FadeOut("━━Σ(ﾟДﾟ川)━ 不是标准弹幕文件", true, "​( ´･･)ﾉ(._.`) 你可以在 biliplus.com 获取");
        }

        if (TbBanner is not null)
            Grid.Children.Remove(TbBanner);
        BControl.IsHitTestVisible = true;
    }

    private void Resume()
    {
        App.Playing = true;
        BPauseResume.Content = new SymbolIcon { Symbol = SymbolRegular.Pause24 };
    }

    private void Pause()
    {
        App.Playing = false;
        BPauseResume.Content = new SymbolIcon { Symbol = SymbolRegular.Play24 };
    }

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

    #endregion

    #region 事件

    private void WDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount is 2)
            WindowState = WindowState is WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void WKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            {
                if (STime.Value - AppContext.FastForward < 0)
                    STime.Value = 0;
                else
                    STime.Value -= AppContext.FastForward;
                break;
            }
            case Key.Right:
            {
                if (STime.Value + AppContext.FastForward > STime.Maximum)
                    STime.Value = 0;
                else
                    STime.Value += AppContext.FastForward;
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
        var fileDialog = new Microsoft.Win32.OpenFileDialog
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
            ((Button)sender).Content = new SymbolIcon { Symbol = SymbolRegular.Pin24 };
            FadeOut("固定上层：关闭", false, "(°∀°)ﾉ");
        }
        else
        {
            Topmost = true;
            ((Button)sender).Content = new SymbolIcon { Symbol = SymbolRegular.PinOff24 };
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

    private bool _needResume;

    private void STimeMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        _needResume = App.Playing;
        Pause();
    }

    private void STimeMouseButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_needResume)
            Resume();
    }

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
