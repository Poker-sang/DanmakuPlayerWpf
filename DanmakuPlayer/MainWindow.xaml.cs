using DanmakuPlayer.Models;
using DanmakuPlayer.Resources;
using DanmakuPlayer.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using ProtoBuf;
using Wpf.Ui.Common;
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
        STime.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(TimeMouseButtonDown), true);
        STime.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(TimeMouseButtonUp), true);
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

    private async void SettingOpen() => await DSetting.ShowAsync();
    // FadeOut("设置已更新", false, "✧(≖ ◡ ≖✿)");
    // BBackGround.Opacity = App.AppConfig.WindowOpacity;

    /// <summary>
    /// 加载弹幕操作
    /// </summary>
    /// <param name="action"></param>
    private async Task LoadDanmaku(Func<Task<List<Danmaku>>> action)
    {
        try
        {
            Pause();
            STime.Maximum = 0;
            Time = 0;
            App.ClearPool();
            var tempPool = await action();

            FadeOut($"已获取{tempPool.Count}条弹幕，正在合并", false, "✧(≖ ◡ ≖✿)");

            App.Pool = (await DanmakuCombiner.Combine(tempPool)).OrderBy(t => t.Time).ToArray();

            var combineRate = App.Pool.Length * 100 / tempPool.Count;

            FadeOut($"已合并为{App.Pool.Length}条弹幕，合并率{combineRate}%，正在渲染", false, "('ヮ')");

            var renderedCount = App.RenderPool();

            var renderRate = renderedCount * 100 / App.Pool.Length;

            var totalRate = renderedCount * 100 / tempPool.Count;

            STime.Maximum = App.Pool[^1].Time + 10;
            TbTotalTime.Text = "/" + STime.Maximum.ToTime();

            FadeOut($"{App.Pool.Length}条弹幕已装载，渲染率{combineRate}%*{renderRate}%={totalRate}%", false, "(/・ω・)/");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            FadeOut(e.Message, true, "​( ´･_･)ﾉ(._.`) 发生异常了");
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
        _ = App.RenderPool();

        App.Timer.Start();
        TryResume();
    }

    private async void Import(int cId)
    {
        FadeOut("弹幕装填中...", false, "(｀・ω・´)");
        try
        {
            await LoadDanmaku(async () =>
            {
                var tempPool = new List<Danmaku>();
                for (var i = 0; ; ++i)
                {
                    await using var danmaku = await BiliApis.GetDanmaku(cId, i + 1);
                    if (danmaku is null)
                        break;
                    var reply = Serializer.Deserialize<DmSegMobileReply>(danmaku);
                    tempPool.AddRange(BiliHelper.ToDanmaku(reply.Elems));
                }

                return tempPool;
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            FadeOut(e.Message, true, "━━Σ(ﾟДﾟ川)━ 未知的异常");
        }
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

    #region 事件处理

    private void WindowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount is not 2)
            return;
        WindowState = WindowState is WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        DanmakuImage.CancelRender = true;
    }

    private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (App.Pool.Length is 0)
            return;

        DanmakuImage.CancelRender = true;
        DanmakuReload();
    }

    public void WindowKeyUp(object sender, KeyEventArgs e)
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

    /*
    private void WindowDragEnter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;

    private void WindowDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] data)
            LoadDanmaku(() =>
            {
                App.ClearPool();
                App.Pool = BiliHelper.ToDanmaku(XDocument.Load(data[0])).ToArray();
                App.RenderPool();
                return Task.CompletedTask;
            });
    }
    */

    private void ImportClick(object sender, RoutedEventArgs e) => DInput.ShowAsync(Import);

    private async void FileClick(object sender, RoutedEventArgs e)
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
            await LoadDanmaku(() => Task.FromResult(BiliHelper.ToDanmaku(XDocument.Load(fileDialog.FileName)).ToList()));
    }

    private void FrontClick(object sender, RoutedEventArgs e)
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

    private void SettingClick(object sender, RoutedEventArgs e) => SettingOpen();

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void PauseResumeClick(object sender, RoutedEventArgs e)
    {
        if (App.Playing)
            Pause();
        else
            Resume();
    }

    private void TimeMouseButtonDown(object sender, MouseButtonEventArgs e) => TryPause();

    private void TimeMouseButtonUp(object sender, MouseButtonEventArgs e) => TryResume();

    private void FileMouseEnter(object sender, MouseEventArgs e) => SpImportButtons.Visibility = Visibility.Visible;

    private void FileMouseLeave(object sender, MouseEventArgs e) => SpImportButtons.Visibility = Visibility.Hidden;

    private void ButtonsMouseEnter(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Visible;

    private void ButtonsMouseLeave(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Hidden;

    private void ControlMouseEnter(object sender, MouseEventArgs e) => DpControl.Visibility = Visibility.Visible;

    private void ControlMouseLeave(object sender, MouseEventArgs e) => DpControl.Visibility = Visibility.Hidden;

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion
}
