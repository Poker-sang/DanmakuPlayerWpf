using System;
using BiliBulletScreenPlayer.Properties;

namespace BiliBulletScreenPlayer;

internal static class GlobalSettings
{
    /// <summary>
    /// ����͸����
    /// </summary>
    /// <remarks>WindowOpacity �� (0, 1)</remarks>
    internal static double WindowOpacity { get; set; } = Settings.Default.WindowOpacity;

    /// <summary>
    /// ��Ļ��ʾ�ٶȣ�����ʱ��(second)��
    /// </summary>
    internal static double Speed { get; set; } = Settings.Default.Speed;

    /// <summary>
    /// ����ٶ�(second)
    /// </summary>
    internal static int FastForward { get; set; } = Settings.Default.FastForward;

    /// <summary>
    /// ��Ļ͸����
    /// </summary>
    /// <remarks>Opacity �� (0, 1)</remarks>
    internal static float Opacity { get; set; } = Settings.Default.Opacity;

    private static double _playSpeed = Settings.Default.PlaySpeed;

    /// <summary>
    /// ����(times)
    /// </summary>
    internal static double PlaySpeed
    {
        get => _playSpeed;
        set
        {
            if (Equals(_playSpeed, value))
                return;
            App.TimeCounter.Interval = new TimeSpan(0, 0, 0, 0, (int)(10000 / PlaySpeed));
            _playSpeed = value;
        }
    }

    /// <summary>
    /// �Ƿ������ص�
    /// </summary>
    internal static bool AllowOverlap { get; set; } = true; //= Settings.Default.AllowOverlap;
}
