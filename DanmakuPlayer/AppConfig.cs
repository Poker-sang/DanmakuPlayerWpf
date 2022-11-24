using DanmakuPlayer.Properties;
using System;

namespace DanmakuPlayer;

public record AppConfig
{
    public AppConfig()
    {
        Theme = Settings.Default.Theme;
        WindowOpacity = Settings.Default.WindowOpacity;

        _playSpeed = Settings.Default.PlaySpeed;
        PlayFastForward = Settings.Default.PlayFastForward;

        DanmakuSpeed = Settings.Default.DanmakuSpeed;
        DanmakuOpacity = Settings.Default.DanmakuOpacity;
        DanmakuAllowOverlap = Settings.Default.DanmakuAllowOverlap;
    }

    public void Save() => Settings.Default.Save();

    #region 应用设置

    /// <summary>
    /// 主题
    /// </summary>
    /// <remarks>default: 0</remarks>
    public int Theme { get; set; } = 0;

    /// <summary>
    /// 窗口透明度
    /// </summary>
    /// <remarks>WindowOpacity ∈ (0, 1), default: 0.2</remarks>
    public double WindowOpacity { get; set; } = 0.2;

    #endregion

    #region 播放设置

    private double _playSpeed = 1;

    /// <summary>
    /// 倍速(times)
    /// </summary>
    /// <remarks>default: 1</remarks>
    public double PlaySpeed
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
    /// 快进速度(second)
    /// </summary>
    /// <remarks>default: 5</remarks>
    public int PlayFastForward { get; set; } = 5;

    #endregion

    #region 弹幕设置

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    /// <remarks>default: 15</remarks>
    public double DanmakuSpeed { get; set; } = 15;

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>Opacity ∈ (0, 1), default: 0.7</remarks>
    public float DanmakuOpacity { get; set; } = 0.7f;

    /// <summary>
    /// 是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuAllowOverlap { get; set; } = true;

    #endregion
}
