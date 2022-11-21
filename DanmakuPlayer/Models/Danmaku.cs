﻿using DanmakuPlayer.Controls;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Windows;
using System.Xml.Linq;
using static System.Convert;

namespace DanmakuPlayer.Models;

/// <summary>
/// 弹幕
/// </summary>
/// <param name="Time"> 出现时间</param>
/// <param name="Mode">模式（4：底端，5：顶端，其他：滚动）</param>
/// <param name="Size">大小</param>
/// <param name="Color">颜色</param>
/// <param name="Text">内容</param>
/// <param name="Layout">拥有的文本框</param>
internal record Danmaku(
    float Time,
    int Mode,
    int Size,
    int Color,
    string Text,
    TextLayout Layout)
{
    public static FrameworkElement ViewPort = null!;
    public static double ViewWidth => ViewPort.ActualWidth;
    public static double ViewHeight => ViewPort.ActualHeight;

    static Danmaku()
    {
        var layout = new TextLayout(DanmakuImage.Factory, "模板Template", DanmakuImage.Format, 1000, 50);
        LayoutHeight = layout.Metrics.Height;
    }

    /// <summary>
    /// 同行两条滚动弹幕间距时间(second)
    /// </summary>
    private const int Space = 5;

    /// <summary>
    /// 文本框宽度
    /// </summary>
    private float LayoutWidth => Layout.Metrics.Width;

    /// <summary>
    /// 文本框高度
    /// </summary>
    private static float LayoutHeight { get; }

    /// <summary>
    /// 视觉区域最多能出现多少弹幕
    /// </summary>
    public static int Count => (int)(ViewHeight / LayoutHeight);

    private SolidColorBrush _brush = null!;

    private float _showPositionY;

    public static Danmaku CreateDanmaku(XElement xElement)
    {
        var tempInfo = xElement.Attribute("p")!.Value.Split(',');
        return new(
         ToSingle(tempInfo[0]),
         ToInt32(tempInfo[1]),
         ToInt32(tempInfo[2]),
         ToInt32(tempInfo[3]),
         xElement.Value,
         new(DanmakuImage.Factory, xElement.Value, DanmakuImage.Format, 1000, 32));
    }

    public bool RenderInit(RenderTarget renderTarget, DanmakuContext context)
    {
        _brush = DanmakuImage.GetBrush(Color, renderTarget);

        // 将要占用空间的索引
        var roomIndex = 0;
        // 是否会覆盖到其他弹幕
        var overlap = true;
        switch (Mode)
        {
            // 底部
            case 4:
            // 顶部
            case 5:
                var start = Mode is 5 ? 0 : (Count - 1);
                var step = Mode is 5 ? 1 : -1;
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

                if (overlap && !GlobalSettings.AllowOverlap)
                    return false;
                context.StaticRoom[roomIndex] = GlobalSettings.Speed + Time;
                _showPositionY = roomIndex * LayoutHeight;
                _showPosition = new RawVector2((float)(ViewWidth - LayoutWidth) / 2, _showPositionY);
                break;
            // 滚动
            default:
                // 在窗口大小内从上往下遍历
                for (var i = 0; i < context.RollRoom.Count; ++i)
                    // 如果下一空间已经过了被占用时间
                    if (Time >= context.RollRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 找出距离结束占用最快的空间
                    else if (context.RollRoom[roomIndex] > context.RollRoom[i])
                        roomIndex = i;

                if (overlap && !GlobalSettings.AllowOverlap)
                    return false;
                context.RollRoom[roomIndex] = (LayoutWidth * GlobalSettings.Speed / (ViewWidth + LayoutWidth)) + Space + Time;
                _showPositionY = roomIndex * LayoutHeight;
                break;
        }

        return true;
    }

    private RawVector2 _showPosition;

    public void OnRender(RenderTarget renderTarget, float timeNow)
    {
        if (Time <= timeNow && timeNow - GlobalSettings.Speed < Time)
        {
            switch (Mode)
            {
                // 底部
                case 4:
                // 顶部
                case 5:
                    renderTarget.DrawTextLayout(_showPosition, Layout, _brush);
                    break;
                // 滚动
                default:
                    renderTarget.DrawTextLayout(new RawVector2((float)(ViewWidth - ((ViewWidth + LayoutWidth) * (timeNow - Time) / GlobalSettings.Speed)), _showPositionY), Layout, _brush);
                    break;
            }
        }
    }
}
