using BiliBulletScreenPlayer.Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BiliBulletScreenPlayer;

public partial class App : Application
{
    /// <summary>
    /// 暂停
    /// </summary>
    internal static bool PlayPause { get; set; }
    internal static DispatcherTimer TimeCounter { get; } = new() { Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / GlobalSettings.PlaySpeed)) };
        
    /// <summary>
    /// Storyboard队列
    /// </summary>
    internal static Queue<Storyboard> SbQueue { get; } = new();

    /// <summary>
    /// 弹幕池
    /// </summary>
    internal static BulletScreen[] Pool { get; set; }

    /// <summary>
    /// 总弹幕池键值表
    /// </summary>
    internal static List<int> PoolIndex { get; set; }

    /// <summary>
    /// 静止弹幕空间（等到第n条空间空余时，的进度条时间）
    /// </summary>
    internal static List<int> StaticRoom { get; } = new();

    /// <summary>
    /// 滚动弹幕空间（等到第n条空间空余时，的进度条时间）
    /// </summary>
    internal static List<int> RollRoom { get; } = new();

    /// <summary>
    /// 上层弹幕数
    /// </summary>
    internal static int StaticUpCount { get; set; }

    /// <summary>
    /// 正在底部弹幕数
    /// </summary>
    internal static int StaticDownCount { get; set; }

    /// <summary>
    /// 正在滚动弹幕数
    /// </summary>
    internal static int RollCount { get; set; }
}