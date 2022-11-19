using BiliBulletScreenPlayer.Controls;
using BiliBulletScreenPlayer.Models;
using BiliBulletScreenPlayer.Services;
using ModernWpf.Controls;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace BiliBulletScreenPlayer;

public partial class MainWindow : Window
{
    #region 操作

    public MainWindow()
    {
        InitializeComponent();
        BulletScreen.ViewPort = BBackGround;
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
                    ++TimeSlider.Value;
                DrawBulletScreens((float)TimeSlider.Value);
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
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _d2DRenderTarget = SharpDx.CreateAndBindTargets(this);
        CompositionTarget.Rendering += (_, _) =>
        {
            var now = DateTime.Now;
            if (TimeSlider.Value < TimeSlider.Maximum)
            {
                if (App.Playing)
                    TimeSlider.Value += (now - _lastTime).TotalSeconds;
                DrawBulletScreens((float)TimeSlider.Value);
            }
            else
            {
                Pause();
                TimeSlider.Value = 0;
                ScreenAllClear();
            }
            _lastTime = now;
        };
        XmlOpen(@"C:\Users\poker\Downloads\846520235.xml", true);
    }

    private static DateTime _lastTime;
    private static int _lastTime2;

    private void DrawBulletScreens(float time)
    {
        _d2DRenderTarget.BeginDraw();
        Debug.WriteLine(DateTime.Now.Millisecond - _lastTime2);
        _d2DRenderTarget.Clear(null);

        _lastTime2 = DateTime.Now.Millisecond;
        var last = false;
        for (var index = 0; index < App.Pool.Length; index++)
        {
            var now = App.Pool[index].OnRender(_d2DRenderTarget, time);
            if (!now && last)
                break;
            last = now;
        }

        _d2DRenderTarget.EndDraw();
        KsyosqStmckfy.Lock();
        KsyosqStmckfy.AddDirtyRect(new Int32Rect(0, 0, KsyosqStmckfy.PixelWidth, KsyosqStmckfy.PixelHeight));
        KsyosqStmckfy.Unlock();
    }

    private RenderTarget _d2DRenderTarget = null!;

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
            var tempList = tempPool.Select(BulletScreen.CreateBulletScreen).ToList();
            for (var i = 0; i < tempList.Count;)
                if (tempList[i].Mode > 5)
                    tempList.RemoveAt(i);
                else
                    ++i;
            App.Pool = tempList.OrderBy(b => b.Time).ToArray();

            TimeSlider.Maximum = App.Pool[^1].Time + 10;
            TotalTimeBlock.Text = '/' + TimeSlider.Maximum.ToTime();
            TimeSlider.Value = 0;
            var context = new BulletScreenContext();
            foreach (var bulletScreen in App.Pool)
                bulletScreen.RenderInit(_d2DRenderTarget, context);
            FadeOut("打开文件", 3000);
        }
        catch (Exception)
        {
            FadeOut("*不是标准B站弹幕xml文件*\n您可以在 biliplus.com 获取", 3000);
        }
        if (Tb is not null)
            Grid.Children.Remove(Tb);
        BControl.IsHitTestVisible = true;
    }
    private void ScreenAllClear()
    {
        //_d2DRenderTarget.Clear(null);
        GC.Collect();
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
            WindowState = WindowState is System.Windows.WindowState.Maximized ? System.Windows.WindowState.Normal : System.Windows.WindowState.Maximized;
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

    private void Slider_MouseButtonDown(object sender, MouseButtonEventArgs e) => Pause();
    private void Slider_MouseButtonUp(object sender, MouseButtonEventArgs e)
    {
        ScreenAllClear();
        Resume();
    }

    private void BFile_MouseEnter(object sender, MouseEventArgs e) => ImportButtons.Visibility = Visibility.Visible;
    private void BFile_MouseLeave(object sender, MouseEventArgs e) => ImportButtons.Visibility = Visibility.Hidden;
    private void BButtons_MouseEnter(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Visible;
    private void BButtons_MouseLeave(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Hidden;
    private void BControl_MouseEnter(object sender, MouseEventArgs e) => SpControl.Visibility = Visibility.Visible;
    private void BControl_MouseLeave(object sender, MouseEventArgs e) => SpControl.Visibility = Visibility.Hidden;
    [GeneratedRegex("\"cid\":([0-9]+),")]
    private static partial Regex MyRegex();

    #endregion
}
