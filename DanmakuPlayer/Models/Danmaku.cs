using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Xml.Linq;

namespace DanmakuPlayer.Models;

/// <summary>
/// 弹幕
/// </summary>
/// <param name="Text">内容</param>
/// <param name="Time">出现时间</param>
/// <param name="Mode">模式</param>
/// <param name="Size">大小</param>
/// <param name="Color">颜色</param>
/// <param name="UnixTimeStamp">发送时间戳</param>
/// <param name="Pool">所属弹幕池</param>
/// <param name="UserHash">用户ID</param>
/// <param name="DatabaseRow">所在数据库行数</param>
public record Danmaku(
    string Text,
    float Time,
    DanmakuMode Mode,
    int Size,
    int Color,
    ulong UnixTimeStamp,
    DanmakuPool Pool,
    string UserHash,
    ulong DatabaseRow)
{
    public static readonly WeakReference<FrameworkElement> ViewPort = new(null!);

    public static double ViewWidth => ViewPort.Get().ActualWidth;

    public static double ViewHeight => ViewPort.Get().ActualHeight;

    /// <summary>
    /// 同行两条滚动弹幕间距时间(second)
    /// </summary>
    private const int Space = 5;

    /// <summary>
    /// 字号为25的文本框高度
    /// </summary>
    private static float LayoutHeight => DirectHelper.LayoutHeights[25];

    /// <summary>
    /// 视觉区域最多能出现多少弹幕
    /// </summary>
    public static int Count => (int)(ViewHeight / LayoutHeight);

    /// <summary>
    /// 是否需要渲染（取决于是否允许重叠）
    /// </summary>
    public bool NeedRender { get; private set; } = true;

    private float _showPositionY;

    /// <summary>
    /// 从<paramref name="xElement"/>获取弹幕
    /// </summary>
    public static Danmaku CreateDanmaku(XElement xElement)
    {
        var tempInfo = xElement.Attribute("p")!.Value.Split(',');
        var size = int.Parse(tempInfo[2]);
        return new(
            xElement.Value,
            float.Parse(tempInfo[0]),
            Enum.Parse<DanmakuMode>(tempInfo[1]),
            size,
            int.Parse(tempInfo[3]),
            ulong.Parse(tempInfo[4]),
            Enum.Parse<DanmakuPool>(tempInfo[5]),
            tempInfo[6],
            ulong.Parse(tempInfo[7]));
    }

    /// <summary>
    /// 初始化渲染
    /// </summary>
    public void RenderInit(DanmakuContext context, AppConfig appConfig)
    {
        var layoutExists = DirectHelper.Layouts.ContainsKey(ToString());
        var layout = layoutExists ? DirectHelper.Layouts[ToString()] : this.GetNewLayout();
        var layoutWidth = layout.Metrics.Width;
        // 将要占用空间的索引
        var roomIndex = 0;
        // 是否会覆盖到其他弹幕
        var overlap = true;

        bool DynamicDanmaku(IList<double> list)
        {
            // 在窗口大小内从上往下遍历
            for (var i = 0; i < list.Count; ++i)
                // 如果下一空间已经过了被占用时间
                if (Time >= list[i])
                {
                    roomIndex = i;
                    overlap = false;
                    break;
                }
                // 找出距离结束占用最快的空间
                else if (list[roomIndex] > list[i])
                    roomIndex = i;

            if (overlap && !appConfig.DanmakuAllowOverlap)
            {
                if (!layoutExists)
                    layout.Dispose();
                NeedRender = false;
                return false;
            }
            list[roomIndex] = (layoutWidth * appConfig.DanmakuDuration / (ViewWidth + layoutWidth)) + Space + Time;
            _showPositionY = roomIndex * LayoutHeight;
            return true;
        }

        switch (Mode)
        {
            case DanmakuMode.Roll:
                if (!DynamicDanmaku(context.RollRoom))
                    return;
                break;
            case DanmakuMode.Bottom:
            case DanmakuMode.Top:
                var start = Mode is DanmakuMode.Top ? 0 : (Count - 1);
                var step = Mode is DanmakuMode.Top ? 1 : -1;
                // 在窗口大小内从上往下遍历
                for (var i = start; 0 <= i && i < context.StaticRoom.Count; i += step)
                    // 如果下一空间已经过了被占用时间
                    if (Time >= context.StaticRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 找出距离结束占用最快的空间
                    else if (context.StaticRoom[roomIndex] > context.StaticRoom[i])
                        roomIndex = i;

                if (overlap && !appConfig.DanmakuAllowOverlap)
                {
                    if (!layoutExists)
                        layout.Dispose();
                    NeedRender = false;
                    return;
                }
                context.StaticRoom[roomIndex] = appConfig.DanmakuDuration + Time;
                _showPositionY = roomIndex * LayoutHeight;
                _staticPosition = new((float)(ViewWidth - layoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Inverse:
                if (!DynamicDanmaku(context.InverseRoom))
                    return;
                break;
            case DanmakuMode.Advanced:
            case DanmakuMode.Code:
            case DanmakuMode.Bas:
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(Mode);
                break;
        }

        if (!layoutExists)
            DirectHelper.Layouts.Add(ToString(), layout);
        NeedRender = true;
    }

    /// <summary>
    /// 静止弹幕显示的位置
    /// </summary>
    private Vector2 _staticPosition;

    public void OnRender(Vortice.Direct2D1.ID2D1RenderTarget renderTarget, float time, AppConfig appConfig)
    {
        // if (Time <= time && time - appConfig.Speed < Time)
        if (!NeedRender)
            return;
        var layout = DirectHelper.Layouts[ToString()];
        switch (Mode)
        {
            case DanmakuMode.Roll:
                renderTarget.DrawTextLayout(new((float)(ViewWidth - ((ViewWidth + layout.Metrics.Width) * (time - Time) / appConfig.DanmakuDuration)), _showPositionY), layout, Color.GetBrush());
                break;
            case DanmakuMode.Bottom:
            case DanmakuMode.Top:
                renderTarget.DrawTextLayout(_staticPosition, layout, Color.GetBrush());
                break;
            case DanmakuMode.Inverse:
                renderTarget.DrawTextLayout(new((float)(((ViewWidth + layout.Metrics.Width) * (time - Time) / appConfig.DanmakuDuration) - layout.Metrics.Width), _showPositionY), layout, Color.GetBrush());
                break;
            case DanmakuMode.Advanced:
            case DanmakuMode.Code:
            case DanmakuMode.Bas:
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(Mode);
                break;
        }
    }

    public override string ToString() => $"{Text},{Color},{Size}";
}
