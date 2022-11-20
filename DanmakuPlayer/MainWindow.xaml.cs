using DanmakuPlayer.Controls;
using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using ModernWpf.Controls;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml.Linq;
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
        Danmaku.ViewPort = BBackGround;
        BBackGround.Opacity = GlobalSettings.WindowOpacity;
        MouseLeftButtonDown += (_, _) => DragMove();
        // handledEventsToo is true 事件才会被处理
        TimeSlider.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_MouseButtonDown), true);
        TimeSlider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseButtonUp), true);

        App.TimeCounter.Tick += (_, _) =>
        {
            if (TimeSlider.Value < TimeSlider.Maximum)
            {
                if (App.Playing)
                    TimeSlider.Value += App.Interval;
                DanmakuImage.InvalidateVisual((float)TimeSlider.Value);
            }
            else
            {
                Pause();
                TimeSlider.Value = 0;
                ScreenAllClear();
            }
        };
        App.TimeCounter.Start();
    }


    private async void SettingOpen()
    {
        var settingWindow = new SettingDialog();
        _ = await settingWindow.ShowAsync();
        if (!settingWindow.DialogResult)
            return;
        BBackGround.Opacity = GlobalSettings.WindowOpacity;
        FadeOut("设置已更新", 3000);
    }

    /// <summary>
    /// 加载xml文件
    /// </summary>
    /// <param name="xml">xml文件路径或字符串</param>
    /// <param name="mode">true为路径，false为字符串</param>
    private void XmlOpen(string xml, bool mode)
    {
        try
        {
            Pause();
            ScreenAllClear();
            TimeSlider.Maximum = 0;
            TimeSlider.Value = 0;
            App.Pool = null;

            var xDoc = mode ? XDocument.Load(xml) : XDocument.Parse(xml);
            var tempPool = xDoc.Element("i")!.Elements("d");
            var context = new DanmakuContext();
            App.Pool = tempPool
                .Select(Danmaku.CreateDanmaku)
                .Where(t => t.Mode < 6)
                .OrderBy(t => t.Time)
                .Where(t => t.RenderInit(DanmakuImage.D2dContext, context))
                .ToArray();

            TimeSlider.Maximum = App.Pool[^1].Time + 10;
            TotalTimeBlock.Text = '/' + TimeSlider.Maximum.ToTime();
            TimeSlider.Value = 0;
            FadeOut("打开文件", 3000);
        }
        catch (Exception e)
        {
            FadeOut("*不是标准B站弹幕xml文件*\n您可以在 biliplus.com 获取", 3000);
        }
        if (Tb is not null)
            Grid.Children.Remove(Tb);
        BControl.IsHitTestVisible = true;
    }

    private void ScreenAllClear()
    {
        // _d2DRenderTarget.Clear(null);
        // GC.Collect();
    }

    private void Resume()
    {
        App.Playing = true;
        BPauseResume.Content = new SymbolIcon(Symbol.Pause);
    }

    private void Pause()
    {
        App.Playing = false;
        BPauseResume.Content = new SymbolIcon(Symbol.Play);
    }

    private void FadeOut(string message, int mSec)
    {
        TbPath.Text = message;
        TbPath.BeginAnimation(OpacityProperty, new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(mSec) });
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
                if (TimeSlider.Value - GlobalSettings.FastForward < 0)
                    TimeSlider.Value = 0;
                else
                    TimeSlider.Value -= GlobalSettings.FastForward;
                break;
            }
            case Key.Right:
            {
                if (TimeSlider.Value + GlobalSettings.FastForward > TimeSlider.Maximum)
                    TimeSlider.Value = 0;
                else
                    TimeSlider.Value += GlobalSettings.FastForward;
                break;
            }
            case Key.Space:
            {
                PauseResumeClick(null, null);
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
    private void WDrag_Enter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;

    private void WDrop(object sender, DragEventArgs e) => XmlOpen(((Array)e.Data.GetData(DataFormats.FileDrop))!.GetValue(0)!.ToString(), true);

    [GeneratedRegex("\"cid\":([0-9]+),")]
    private static partial Regex MyRegex();

    private async void BImportClick(object sender, RoutedEventArgs e)
    {
        var inputNameDialog = new InputNumberDialog();
        _ = await inputNameDialog.ShowAsync();
        if (inputNameDialog.DialogResult is not true)
            return;
        FadeOut("加载xml中...", 3000);

        var xmlString = "";
        try
        {
            var http = await new HttpClient().GetStringAsync("https://www.biliplus.com/video/" + inputNameDialog.Number);
            var xmlUri = @"http://comment.bilibili.com/";
            if (MyRegex().Match(http) is { Success: true } match)
                xmlUri += match.Groups[1].Value + ".xml";
            else
            {
                FadeOut("视频不存在！", 3000);
                return;
            }

            xmlString = await new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.Deflate }).GetStringAsync(xmlUri);
        }
        catch (Exception exception)
        {
            FadeOut(exception.Message, 3000);
        }
        XmlOpen(xmlString, false);
    }
    private void BFileClick(object sender, RoutedEventArgs e)
    {
        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择xml弹幕文件",
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
            ((Button)sender).Content = new SymbolIcon(Symbol.Pin);
            FadeOut("总在最前端：关闭", 3000);
        }
        else
        {
            Topmost = true;
            ((Button)sender).Content = new SymbolIcon(Symbol.UnPin);
            FadeOut("总在最前端：开启", 3000);
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

    private void Slider_MouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        _needResume = App.Playing;
        Pause();
    }

    private void Slider_MouseButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_needResume)
            Resume();
    }

    private void BFile_MouseEnter(object sender, MouseEventArgs e) => ImportButtons.Visibility = Visibility.Visible;

    private void BFile_MouseLeave(object sender, MouseEventArgs e) => ImportButtons.Visibility = Visibility.Hidden;

    private void BButtons_MouseEnter(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Visible;

    private void BButtons_MouseLeave(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Hidden;

    private void BControl_MouseEnter(object sender, MouseEventArgs e) => SpControl.Visibility = Visibility.Visible;

    private void BControl_MouseLeave(object sender, MouseEventArgs e) => SpControl.Visibility = Visibility.Hidden;

    #endregion
}
