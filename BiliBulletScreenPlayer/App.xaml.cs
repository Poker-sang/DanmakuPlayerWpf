using BiliBulletScreenPlayer.Models;
using System;
using System.Windows;
using System.Windows.Threading;

namespace BiliBulletScreenPlayer;

public partial class App : Application
{
    /// <summary>
    /// 是否正在播放（没有暂停）
    /// </summary>
    internal static bool Playing { get; set; }

    /// <summary>
    /// 计时器
    /// </summary>
    internal static DispatcherTimer TimeCounter { get; } = new() { Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / GlobalSettings.PlaySpeed)) };

    /// <summary>
    /// 弹幕池
    /// </summary>
    internal static BulletScreen[]Pool { get; set; } = Array.Empty<BulletScreen>();
}
