using DanmakuPlayer.Models;
using System;
using System.Windows;
using System.Windows.Threading;

namespace DanmakuPlayer;

public partial class App : Application
{
    /// <summary>
    /// 是否正在播放（没有暂停）
    /// </summary>
    internal static bool Playing { get; set; }

    internal static double Interval => 0.04;

    /// <summary>
    /// 计时器
    /// </summary>
    internal static DispatcherTimer TimeCounter { get; } = new() { Interval = TimeSpan.FromSeconds(Interval / GlobalSettings.PlaySpeed) };

    /// <summary>
    /// 弹幕池
    /// </summary>
    internal static Danmaku[] Pool { get; set; } = Array.Empty<Danmaku>();
}
