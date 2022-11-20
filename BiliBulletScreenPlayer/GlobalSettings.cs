using System;
using BiliBulletScreenPlayer.Properties;

namespace BiliBulletScreenPlayer;

internal static class GlobalSettings
{
    /// <summary>
    /// ����͸����(percentage)
    /// </summary>
    internal static double WindowOpacity { get; set; } = Settings.Default.WindowOpacity;

    /// <summary>
    /// ��Ļ��ʾ�ٶȣ�����ʱ��(second)��
    /// </summary>
    internal static int Speed { get; set; } = Settings.Default.Speed;

    /// <summary>
    /// ����ٶ�(second)
    /// </summary>
    internal static int FastForward { get; set; } = Settings.Default.FastForward;

    /// <summary>
    /// ��Ļ͸����(percentage)
    /// </summary>
    internal static double Opacity { get; set; } = Settings.Default.Opacity;

    private static double _playSpeed = Settings.Default.PlaySpeed;

    /// <summary>
    /// ����(times)
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
    /// �ϲ㵯Ļ��
    /// </summary>
    internal static int StaticUpLimit { get; set; } = Settings.Default.StaticUpLimit;

    /// <summary>
    /// ���ڵײ���Ļ��
    /// </summary>
    internal static int StaticDownLimit { get; set; } = Settings.Default.StaticDownLimit;

    /// <summary>
    /// ���ڹ�����Ļ��
    /// </summary>
    internal static int RollLimit { get; set; } = Settings.Default.RollLimit;

    /// <summary>
    /// �����ص�
    /// </summary>
    internal static bool AllowOverlap { get; set; } = Settings.Default.AllowOverlap;
}