using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using static System.Convert;

namespace BiliBulletScreenPlayer.Model;

internal record BulletScreen
{
    /// <summary>
    /// 出现时间
    /// </summary>
    public readonly int Time;

    /// <summary>
    /// 模式（4：底端，5：顶端，其他：滚动）
    /// </summary>
    public readonly int Mode;

    ///// <summary>
    ///// 大小
    ///// </summary>
    //public readonly int Size;

    /// <summary>
    /// 内容
    /// </summary>
    private readonly string _text;

    /// <summary>
    /// 颜色
    /// </summary>
    private readonly int _color;

    /// <summary>
    /// 拥有的文本框
    /// </summary>
    private readonly TextBlock _textBlock = new();

    /// <summary>
    /// 高度
    /// </summary>
    /// <remarks>实际数据约31.75</remarks>
    private const int Height = 32;

    /// <summary>
    /// 同行两条滚动弹幕间距时间(second)
    /// </summary>
    private const int Space = 5;

    public static MainWindow Window;

    public BulletScreen(XElement xElement)
    {
        var tempInfo = xElement.Attribute("p")!.Value.Split(',');
        Time = (int)ToDouble(tempInfo[0]);
        Mode = ToInt32(tempInfo[1]);
        //Size = Convert.ToInt32(tempInfo[2]);
        _color = ToInt32(tempInfo[3]);
        _text = xElement.Value;
    }

    public void Start(Storyboard storyboard, int timeNow)
    {
        _textBlock.Style = (Style)_textBlock.FindResource("BulletScreenBlock");
        _textBlock.Opacity = GlobalSettings.Opacity;
        _textBlock.Text = _text;
        _textBlock.Foreground = new SolidColorBrush(Color.FromRgb((byte)((_color & 0xFF0000) >> 0x10), (byte)((_color & 0xFF00) >> 8), (byte)(_color & 0xFF)));

        // 将要占用空间的索引
        var roomIndex = 0;
        // 是否会覆盖到其他弹幕
        var overlap = true;
        switch (Mode)
        {
            //底部
            case 4:
                if (GlobalSettings.StaticDownLimit is not -1 && App.StaticDownCount >= GlobalSettings.StaticDownLimit)
                    return;
                for (var i = (int)(Window.ActualHeight / Height - 1); i >= 0; --i)
                    // 如果下一空间还从未有过弹幕
                    if (i >= App.StaticRoom.Count)
                    {
                        roomIndex = i;
                        while (i >= App.StaticRoom.Count)
                            App.StaticRoom.Add(GlobalSettings.Speed + timeNow);
                        overlap = false;
                        break;
                    }
                    // 如果下一空间已经过了被占用时间
                    else if (timeNow >= App.StaticRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    else if (App.StaticRoom[roomIndex] > App.StaticRoom[i])
                        roomIndex = i;

                if (overlap && !GlobalSettings.AllowOverlap)
                    return;
                ++App.StaticUpCount;
                _ = Window.Canvas.Children.Add(_textBlock);
                _textBlock.UpdateLayout();

                App.StaticRoom[roomIndex] = GlobalSettings.Speed + timeNow;
                Canvas.SetTop(_textBlock, roomIndex * Height);
                Canvas.SetLeft(_textBlock, (Window.ActualWidth - _textBlock.ActualWidth) / 2);
                App.TimeCounter.Tick += StaticBulletScreen;
                break;
            //顶部
            case 5:
                if (GlobalSettings.StaticUpLimit is not -1 && App.StaticUpCount >= GlobalSettings.StaticUpLimit)
                    return;
                // 在窗口大小内从上往下遍历
                for (var i = 0; i < Window.ActualHeight / Height; ++i)
                    // 如果下一空间还从未有过弹幕
                    if (i >= App.StaticRoom.Count)
                    {
                        App.StaticRoom.Add(GlobalSettings.Speed + timeNow);
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 如果下一空间已经过了被占用时间
                    else if (timeNow >= App.StaticRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    else if (App.StaticRoom[roomIndex] > App.StaticRoom[i])
                        roomIndex = i;

                if (overlap && !GlobalSettings.AllowOverlap)
                    return;
                ++App.StaticDownCount;
                _ = Window.Canvas.Children.Add(_textBlock);
                _textBlock.UpdateLayout();
                // 下面同
                App.StaticRoom[roomIndex] = GlobalSettings.Speed + timeNow;
                Canvas.SetTop(_textBlock, roomIndex * Height);
                Canvas.SetLeft(_textBlock, (Window.ActualWidth - _textBlock.ActualWidth) / 2);
                App.TimeCounter.Tick += StaticBulletScreen;
                break;
            //滚动
            default:
                if (GlobalSettings.RollLimit is not -1 && App.RollCount >= GlobalSettings.RollLimit)
                    return;
                // 在窗口大小内从上往下遍历
                for (var i = 0; i < Window.ActualHeight / Height; ++i)
                    // 如果下一空间还从未有过弹幕
                    if (i >= App.RollRoom.Count)
                    {
                        App.RollRoom.Add(0);
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 如果下一空间已经过了被占用时间
                    else if (timeNow >= App.RollRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 以上都不符合，就先记录最早被占用的空间
                    else if (App.RollRoom[roomIndex] > App.RollRoom[i])
                        roomIndex = i;

                if (overlap && !GlobalSettings.AllowOverlap)
                    return;
                ++App.RollCount;
                _ = Window.Canvas.Children.Add(_textBlock);
                _textBlock.UpdateLayout();

                App.RollRoom[roomIndex] = (int)(_textBlock.ActualWidth * GlobalSettings.Speed / (Window.ActualWidth + _textBlock.ActualWidth) + Space) + timeNow;
                Canvas.SetTop(_textBlock, roomIndex * Height);
                RollBulletScreen(storyboard);
                return;
        } 
    }

    private int _delayTime;
    private void StaticBulletScreen(object sender, EventArgs e)
    {
        if (_delayTime < GlobalSettings.Speed)
            ++_delayTime;
        else
        {
            App.TimeCounter.Tick -= StaticBulletScreen;
            switch (Mode)
            {
                case 4:
                    --App.StaticDownCount;
                    break;
                case 5:
                    --App.StaticUpCount;
                    break;
            }
            Window.Canvas.Children.Remove(_textBlock);
        }
    }

    private void RollBulletScreen(Storyboard storyboard)
    {
        var rollDa = new DoubleAnimation
        {
            From = Window.ActualWidth,
            To = -_textBlock.ActualWidth,
            Duration = TimeSpan.FromSeconds(GlobalSettings.Speed)
        };
        Storyboard.SetTarget(rollDa, _textBlock);
        storyboard.Children.Add(rollDa);
        storyboard.Completed += (_, _) =>
        {
            --App.RollCount;
            Window.Canvas.Children.Remove(_textBlock);
        };
    }
}