using DanmakuPlayer.Properties;
using System;

namespace DanmakuPlayer;

public static class AppContext
{
    /// <summary>
    /// ����͸����
    /// </summary>
    /// <remarks>WindowOpacity �� (0, 1)</remarks>
    public static double WindowOpacity { get; set; } = Settings.Default.WindowOpacity;

    /// <summary>
    /// ��Ļ��ʾ�ٶȣ�����ʱ��(second)��
    /// </summary>
    public static double Speed { get; set; } = Settings.Default.Speed;

    /// <summary>
    /// ����ٶ�(second)
    /// </summary>
    public static int FastForward { get; set; } = Settings.Default.FastForward;

    /// <summary>
    /// ��Ļ͸����
    /// </summary>
    /// <remarks>Opacity �� (0, 1)</remarks>
    public static float Opacity { get; set; } = Settings.Default.Opacity;

    private static double _playSpeed = Settings.Default.PlaySpeed;

    /// <summary>
    /// ����(times)
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
    /// �Ƿ������ص�
    /// </summary>
    public static bool AllowOverlap { get; set; } = true; //= Settings.Default.AllowOverlap;
}
