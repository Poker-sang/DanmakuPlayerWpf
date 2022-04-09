using BiliBulletScreenPlayer.Control;
using BiliBulletScreenPlayer.Model;
using BiliBulletScreenPlayer.Service;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace BiliBulletScreenPlayer;

public partial class MainWindow : Window
{
    #region 操作

    public MainWindow()
    {
        InitializeComponent();
        BulletScreen.Window = this;
        BBackGround.Opacity = GlobalSettings.WindowOpacity;
        MouseLeftButtonDown += (_, _) => DragMove();
        // handledEventsToo == true 事件才会被处理
        TimeSlider.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_MouseButtonDown), true);
        TimeSlider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseButtonUp), true);

        var propertyPath = new PropertyPath("(Canvas.Left)");
        App.TimeCounter.Tick += (_, _) =>
        {
            if (TimeSlider.Value < TimeSlider.Maximum)
            {
                ++TimeSlider.Value;
                // 找到第一个
                var indexBegin = App.PoolIndex.IndexOf((int)TimeSlider.Value);
                if (indexBegin is -1)
                    return;
                var storyBoard = new Storyboard();
                do
                    App.Pool[indexBegin++].Start(storyBoard, (int)TimeSlider.Value);
                while (indexBegin < App.Pool.Length && App.Pool[indexBegin].Time == (int)TimeSlider.Value);
                // 如果没有滚动弹幕，就不添加Storyboard
                if (storyBoard.Children.Count is 0)
                    return;
                storyBoard.Completed += (_, _) => App.SbQueue.Dequeue();
                Storyboard.SetTargetProperty(storyBoard, propertyPath);
                App.SbQueue.Enqueue(storyBoard);
                storyBoard.Begin();
                GC.Collect();
            }
            else
            {
                Pause();
                TimeSlider.Value = 0;
                ScreenAllClear();
            }
        };
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
            App.PoolIndex = new List<int>();

            var xDoc = mode ? XDocument.Load(xml) : XDocument.Parse(xml);
            var tempPool = xDoc.Element("i")!.Elements("d");
            var tempList = tempPool.Select(each => new BulletScreen(each)).ToList();
            for (var i = 0; i < tempList.Count;)
                if (tempList[i].Mode > 5)
                    tempList.RemoveAt(i);
                else ++i;
            tempList = tempList.OrderBy(b => b.Time).ToList();
            App.Pool = new BulletScreen[tempList.Count];
            tempList.CopyTo(App.Pool);
            foreach (var t in App.Pool)
                App.PoolIndex.Add(t.Time);

            TimeSlider.Maximum = App.Pool[^1].Time + 10;
            TotalTimeBlock.Text = '/' + TimeSlider.Maximum.ToTime();
            TimeSlider.Value = 0;
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
        Canvas.Children.Clear();
        App.SbQueue.Clear();
        App.RollRoom.Clear();
        App.StaticRoom.Clear();
        App.StaticDownCount = 0;
        App.StaticUpCount = 0;
        App.RollCount = 0;
        GC.Collect();
    }
    private void Resume()
    {
        App.PlayPause = true;
        App.TimeCounter.Start();
        BPauseResume.Content = new FontIcon { Glyph = "\uEDB4" };
        if (App.Pool is not null)
            foreach (var each in App.SbQueue)
                each.Resume();
    }
    private void Pause()
    {
        App.PlayPause = false;
        App.TimeCounter.Stop();
        BPauseResume.Content = new FontIcon { Glyph = "\uEDB5" };
        if (App.Pool is not null)
            foreach (var each in App.SbQueue)
                each.Pause();
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
                    else TimeSlider.Value -= GlobalSettings.FastForward;
                    ScreenAllClear();
                    break;
                }
            case Key.Right:
                {
                    if (TimeSlider.Value + GlobalSettings.FastForward > TimeSlider.Maximum)
                        TimeSlider.Value = 0;
                    else TimeSlider.Value += GlobalSettings.FastForward;
                    ScreenAllClear();
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
        if (inputNameDialog.DialogResult is not true) return;
        FadeOut("加载xml中...", 3000);

        var xmlString = "";
        try
        {
            var http = await new HttpClient().GetStringAsync("https://www.biliplus.com/video/" + inputNameDialog.Number);
            var xmlUri = @"http://comment.bilibili.com/";
            if (new Regex(@"""cid"":([0-9]+),").Match(http) is { Success: true } match)
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
            ((Button)sender).Content = new FontIcon { Glyph = "\uE718" };
            FadeOut("总在最前端：关闭", 3000);
        }
        else
        {
            Topmost = true;
            ((Button)sender).Content = new FontIcon { Glyph = "\uE840" };
            FadeOut("总在最前端：开启", 3000);
        }
    }
    private void BSettingClick(object sender, RoutedEventArgs e) => SettingOpen();
    private void BCloseClick(object sender, RoutedEventArgs e) => Close();
    private void PauseResumeClick(object sender, RoutedEventArgs e)
    {
        if (App.PlayPause)
            Pause();
        else Resume();
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

    #endregion
}