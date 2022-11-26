using DanmakuPlayer.Properties;

namespace DanmakuPlayer;

public record AppConfig
{
    public AppConfig()
    {
        Theme = Settings.Default.Theme;
        _windowOpacity = Settings.Default.WindowOpacity;

        _playSpeed = Settings.Default.PlaySpeed;
        PlayFastForward = Settings.Default.PlayFastForward;
        _playFramePerSecond = Settings.Default.PlayFramePerSecond;

        DanmakuSpeed = Settings.Default.DanmakuSpeed;
        DanmakuOpacity = Settings.Default.DanmakuOpacity;
        DanmakuAllowOverlap = Settings.Default.DanmakuAllowOverlap;
        DanmakuFont = Settings.Default.DanmakuFont;
        DanmakuScale = Settings.Default.DanmakuScale;
    }

    public void Save()
    {
        Settings.Default.Theme = Theme;
        Settings.Default.WindowOpacity = _windowOpacity;

        Settings.Default.PlaySpeed = (float)_playSpeed;
        Settings.Default.PlayFastForward = PlayFastForward;
        Settings.Default.PlayFramePerSecond = _playFramePerSecond;

        Settings.Default.DanmakuSpeed = (int)DanmakuSpeed;
        Settings.Default.DanmakuOpacity = DanmakuOpacity;
        Settings.Default.DanmakuAllowOverlap = DanmakuAllowOverlap;
        Settings.Default.DanmakuFont = DanmakuFont;
        Settings.Default.DanmakuScale = DanmakuScale;
        Settings.Default.Save();
    }

    #region 应用设置

    private double _windowOpacity = 0.2;

    /// <summary>
    /// 主题
    /// </summary>
    /// <remarks>default: 0</remarks>
    public int Theme { get; set; } = 0;

    /// <summary>
    /// 窗口透明度
    /// </summary>
    /// <remarks>WindowOpacity ∈ [0.1, 1], default: 0.2</remarks>
    public double WindowOpacity
    {
        get => _windowOpacity;
        set => App.Window.BBackGround.Opacity = _windowOpacity = value;
    }

    #endregion

    #region 播放设置

    private double _playSpeed = 1;
    private int _playFramePerSecond = 25;
    private double _interval = 0.04;

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
            App.ResetTimerInterval();
            _playSpeed = value;
        }
    }

    /// <summary>
    /// 快进速度(second)
    /// </summary>
    /// <remarks>default: 5</remarks>
    public int PlayFastForward { get; set; } = 5;

    /// <summary>
    /// 帧率(second)
    /// </summary>
    /// <remarks>PlayFramePerSecond ∈ [5, 100], default: 25</remarks>
    public int PlayFramePerSecond
    {
        get => _playFramePerSecond;
        set
        {
            _playFramePerSecond = value;
            Interval = 1d / _playFramePerSecond;
        }
    }

    /// <summary>
    /// Timer时间间隔
    /// </summary>
    public double Interval
    {
        get => _interval;
        set
        {
            App.ResetTimerInterval();
            _interval = value;
        }
    }

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
    /// <remarks>Opacity ∈ [0, 1], default: 0.7</remarks>
    public float DanmakuOpacity { get; set; } = 0.7f;

    /// <summary>
    /// 弹幕是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuAllowOverlap { get; set; } = true;

    /// <summary>
    /// 弹幕字体
    /// </summary>
    public string DanmakuFont { get; set; } = "";

    /// <summary>
    /// 弹幕大小缩放
    /// </summary>
    /// <remarks>DanmakuScale ∈ [0.5, 2], default: 1</remarks>
    public float DanmakuScale { get; set; } = 1;

    #endregion
}
