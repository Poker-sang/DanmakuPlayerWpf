using DanmakuPlayer.Properties;
using DanmakuPlayer.Services;

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

        DanmakuDuration = Settings.Default.DanmakuDanmakuDuration;
        _danmakuOpacity = Settings.Default.DanmakuOpacity;
        _danmakuAllowOverlap = Settings.Default.DanmakuAllowOverlap;
        _danmakuFont = Settings.Default.DanmakuFont;
        _danmakuScale = Settings.Default.DanmakuScale;
    }

    public void Save()
    {
        Settings.Default.Theme = Theme;
        Settings.Default.WindowOpacity = _windowOpacity;

        Settings.Default.PlaySpeed = (float)_playSpeed;
        Settings.Default.PlayFastForward = PlayFastForward;
        Settings.Default.PlayFramePerSecond = _playFramePerSecond;

        Settings.Default.DanmakuDanmakuDuration = (int)DanmakuDuration;
        Settings.Default.DanmakuOpacity = _danmakuOpacity;
        Settings.Default.DanmakuAllowOverlap = _danmakuAllowOverlap;
        Settings.Default.DanmakuFont = _danmakuFont;
        Settings.Default.DanmakuScale = _danmakuScale;
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
        set
        {
            if (Equals(_windowOpacity, value))
                return;
            App.Window.BBackGround.Opacity = _windowOpacity = value;
        }
    }

    #endregion

    #region 播放设置

    private double _playSpeed = 1;
    private int _playFramePerSecond = 25;

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
            _playSpeed = value;
            App.ResetTimerInterval();
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
            if (Equals(_playFramePerSecond, value))
                return;
            _playFramePerSecond = value;
            Interval = 1d / _playFramePerSecond;
            App.ResetTimerInterval();
        }
    }

    /// <summary>
    /// Timer时间间隔
    /// </summary>
    public double Interval { get; private set; } = 0.04;

    #endregion

    #region 弹幕设置

    private float _danmakuOpacity = 0.7f;
    private bool _danmakuAllowOverlap = true;
    private string _danmakuFont = "微软雅黑";
    private float _danmakuScale = 1;

    /// <summary>
    /// 弹幕显示速度（过屏时间(second)）
    /// </summary>
    /// <remarks>DanmakuDuration ∈ [5, 20], default: 15</remarks>
    public float DanmakuDuration { get; set; } = 15;

    /// <summary>
    /// 弹幕透明度
    /// </summary>
    /// <remarks>Opacity ∈ [0.1, 1], default: 0.7</remarks>
    public float DanmakuOpacity
    {
        get => _danmakuOpacity;
        set
        {
            if (Equals(_danmakuOpacity, value))
                return;
            _danmakuOpacity = value;
            App.Window.DanmakuImage.Opacity = _danmakuOpacity;
        }
    }

    /// <summary>
    /// 弹幕是否允许重叠
    /// </summary>
    /// <remarks>default: <see langword="true"/></remarks>
    public bool DanmakuAllowOverlap
    {
        get => _danmakuAllowOverlap;
        set
        {
            if (Equals(_danmakuAllowOverlap, value))
                return;
            _danmakuAllowOverlap = value;
            App.Window.DanmakuReload();
        }
    }

    /// <summary>
    /// 弹幕字体
    /// </summary>
    /// <remarks>default: "微软雅黑"</remarks>
    public string DanmakuFont
    {
        get => _danmakuFont;
        set
        {
            if (Equals(_danmakuFont, value))
                return;
            _danmakuFont = value;
            App.Window.DanmakuReload(DirectHelper.ClearTextFormats);
        }
    }

    /// <summary>
    /// 弹幕大小缩放
    /// </summary>
    /// <remarks>DanmakuScale ∈ [0.5, 2], default: 1</remarks>
    public float DanmakuScale
    {
        get => _danmakuScale;
        set
        {
            if (Equals(_danmakuScale, value))
                return;
            _danmakuScale = value;
            App.Window.DanmakuReload(DirectHelper.ClearTextFormats);
        }
    }

    #endregion
}
