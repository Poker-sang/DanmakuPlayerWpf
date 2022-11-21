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
    public static bool Playing { get; set; }

    public static double Interval => 0.04;

    /// <summary>
    /// 计时器
    /// </summary>
    public static DispatcherTimer Timer { get; } = new() { Interval = TimeSpan.FromSeconds(Interval / AppContext.PlaySpeed) };

    /// <summary>
    /// 弹幕池
    /// </summary>
    public static Danmaku[] Pool { get; set; } = Array.Empty<Danmaku>();

    public static void ClearPool()
    {
        foreach (var danmaku in Pool)
            danmaku.Dispose();
        Pool = Array.Empty<Danmaku>();
    }
}
