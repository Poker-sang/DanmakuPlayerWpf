using System.Collections.Generic;

namespace DanmakuPlayer.Models;

/// <summary>
/// 用来统一各弹幕的上下文
/// </summary>
public class DanmakuContext
{
    public DanmakuContext()
    {
        for (var i = 0; i < Danmaku.Count; ++i)
        {
            StaticRoom.Add(-1);
            RollRoom.Add(-1);
        }
    }

    /// <summary>
    /// 静止弹幕空间
    /// </summary>
    /// <remarks>StaticRoom[i]：等到第i条空间空余时，进度条的时间</remarks>
    public List<double> StaticRoom { get; } = new(Danmaku.Count);

    /// <summary>
    /// 滚动弹幕空间
    /// </summary>
    /// <remarks>RollRoom[i]：等到第i条空间空余时，进度条的时间</remarks>
    public List<double> RollRoom { get; } = new(Danmaku.Count);

    /// <summary>
    /// 正在顶部弹幕数
    /// </summary>
    public int StaticTopCount { get; set; }

    /// <summary>
    /// 正在底部弹幕数
    /// </summary>
    public int StaticBottomCount { get; set; }

    /// <summary>
    /// 正在滚动弹幕数
    /// </summary>
    public int RollCount { get; set; }
}
