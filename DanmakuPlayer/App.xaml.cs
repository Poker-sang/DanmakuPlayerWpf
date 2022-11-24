using DanmakuPlayer.Models;
using System;
using System.Windows;
using System.Windows.Threading;

namespace DanmakuPlayer;

public partial class App : Application
{
    static App()
    {
        if (AppConfig is not { } appConfigurations
#if FIRST_TIME
        || true
#endif
           )
            AppConfig = new();
        else
            AppConfig = appConfigurations;
        Timer = new() { Interval = TimeSpan.FromSeconds(Interval / AppConfig.PlaySpeed) };
    }

    /// <summary>
    /// 应用设置
    /// </summary>
    public static AppConfig AppConfig { get; }

    /// <summary>
    /// 是否正在播放（没有暂停）
    /// </summary>
    public static bool Playing { get; set; }

    public static double Interval => 0.04;

    /// <summary>
    /// 计时器
    /// </summary>
    public static DispatcherTimer Timer { get; }

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
