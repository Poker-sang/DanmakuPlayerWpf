using DanmakuPlayer.Properties;
using System;

namespace DanmakuPlayer;

public static class AppContext
{
    /// <summary>
    /// 窗口透明度
    /// </summary>
    /// <remarks>WindowOpacity ∈ (0, 1)</remarks>
    public static double WindowOpacity { get; set; } = Settings.Default.WindowOpacity;

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    public static double Speed { get; set; } = Settings.Default.Speed;

    /// <summary>
    /// 快进速度(second)
    /// </summary>
    public static int FastForward { get; set; } = Settings.Default.FastForward;

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>Opacity ∈ (0, 1)</remarks>
    public static float Opacity { get; set; } = Settings.Default.Opacity;

    private static double _playSpeed = Settings.Default.PlaySpeed;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    public static double PlaySpeed
    {
        get => _playSpeed;
        set
        {
            if (Equals(_playSpeed, value))
                return;
            App.Timer.Interval = TimeSpan.FromSeconds(App.Interval / PlaySpeed);
            _playSpeed = value;
        }
    }

    /// <summary>
    /// 是否允许重叠
    /// </summary>
    public static bool AllowOverlap { get; set; } = true; //= Settings.Default.AllowOverlap;
}
