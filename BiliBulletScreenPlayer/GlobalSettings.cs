using System;
using BiliBulletScreenPlayer.Properties;

namespace BiliBulletScreenPlayer;

internal static class GlobalSettings
{
    /// <summary>
    /// 窗口透明度(percentage)
    /// </summary>
    internal static double WindowOpacity { get; set; } = Settings.Default.WindowOpacity;

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    internal static int Speed { get; set; } = Settings.Default.Speed;

    /// <summary>
    /// 快进速度(second)
    /// </summary>
    internal static int FastForward { get; set; } = Settings.Default.FastForward;

    /// <summary>
    /// 弹幕透明度(percentage)
    /// </summary>
    internal static double Opacity { get; set; } = Settings.Default.Opacity;

    private static double _playSpeed = Settings.Default.PlaySpeed;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    internal static double PlaySpeed
    {
        get => _playSpeed;
        set
        {
            if (Equals(_playSpeed, value)) return;
            App.TimeCounter.Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / PlaySpeed));
            _playSpeed = value;
        }
    }

    /// <summary>
    /// 上层弹幕数
    /// </summary>
    internal static int StaticUpLimit { get; set; } = Settings.Default.StaticUpLimit;

    /// <summary>
    /// 正在底部弹幕数
    /// </summary>
    internal static int StaticDownLimit { get; set; } = Settings.Default.StaticDownLimit;

    /// <summary>
    /// 正在滚动弹幕数
    /// </summary>
    internal static int RollLimit { get; set; } = Settings.Default.RollLimit;

    /// <summary>
    /// 允许重叠
    /// </summary>
    internal static bool AllowOverlap { get; set; } = Settings.Default.AllowOverlap;
}