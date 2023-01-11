using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using System;
using System.Linq;
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

        HttpClientHelper.Initialize();
        DirectHelper.ClearBrushes();
        DirectHelper.ClearTextFormats();
        ResetTimerInterval();
    }

    public static MainWindow Window { get; set; } = null!;

    /// <summary>
    /// 应用设置
    /// </summary>
    public static AppConfig AppConfig { get; }

    /// <summary>
    /// 是否正在播放（没有暂停）
    /// </summary>
    public static bool Playing { get; set; }

    /// <summary>
    /// 计时器
    /// </summary>
    public static DispatcherTimer Timer { get; } = new();

    public static void ResetTimerInterval() => Timer.Interval = TimeSpan.FromSeconds(AppConfig.Interval / AppConfig.PlaySpeed);

    /// <summary>
    /// 弹幕池
    /// </summary>
    public static Danmaku[] Pool { get; set; } = Array.Empty<Danmaku>();

    public static void ClearPool()
    {
        Pool = Array.Empty<Danmaku>();
        DirectHelper.ClearLayouts();
    }

    public static int RenderPool()
    {
        var context = new DanmakuContext();
        return Pool.Count(danmaku => danmaku.RenderInit(context, AppConfig));
    }
}
