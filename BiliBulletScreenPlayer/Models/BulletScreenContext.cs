using System.Collections.Generic;

namespace BiliBulletScreenPlayer.Models;

public class BulletScreenContext
{
    public BulletScreenContext()
    {
        for (var i = 0; i < BulletScreen.Count; ++i)
        {
            StaticRoom.Add(-1);
            RollRoom.Add(-1);
        }
    }

    /// <summary>
    /// 静止弹幕空间
    /// </summary>
    /// <remarks>StaticRoom[i]：等到第i条空间空余时，进度条的时间</remarks>
    internal List<double> StaticRoom { get; } = new(BulletScreen.Count);

    /// <summary>
    /// 滚动弹幕空间
    /// </summary>
    /// <remarks>RollRoom[i]：等到第i条空间空余时，进度条的时间</remarks>
    internal List<double> RollRoom { get; } = new(BulletScreen.Count);

    /// <summary>
    /// 上层弹幕数
    /// </summary>
    internal int StaticUpCount { get; set; }

    /// <summary>
    /// 正在底部弹幕数
    /// </summary>
    internal int StaticDownCount { get; set; }

    /// <summary>
    /// 正在滚动弹幕数
    /// </summary>
    internal int RollCount { get; set; }
}
