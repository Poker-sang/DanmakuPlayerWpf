using System;
using DanmakuPlayer.Properties;

namespace DanmakuPlayer;

internal static class GlobalSettings
{
    /// <summary>
    /// 窗口透明度
    /// </summary>
    /// <remarks>WindowOpacity ∈ (0, 1)</remarks>
    internal static double WindowOpacity { get; set; } = Settings.Default.WindowOpacity;

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    internal static double Speed { get; set; } = Settings.Default.Speed;

    /// <summary>
    /// 快进速度(second)
    /// </summary>
    internal static int FastForward { get; set; } = Settings.Default.FastForward;

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>Opacity ∈ (0, 1)</remarks>
    internal static float Opacity { get; set; } = Settings.Default.Opacity;

    private static double _playSpeed = Settings.Default.PlaySpeed;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    internal static double PlaySpeed
    {
        get => _playSpeed;
        set
        {
            if (Equals(_playSpeed, value))
                return;
            App.TimeCounter.Interval = TimeSpan.FromSeconds(App.Interval / PlaySpeed);
            _playSpeed = value;
        }
    }

    /// <summary>
    /// 是否允许重叠
    /// </summary>
    internal static bool AllowOverlap { get; set; } = Settings.Default.AllowOverlap;
}
