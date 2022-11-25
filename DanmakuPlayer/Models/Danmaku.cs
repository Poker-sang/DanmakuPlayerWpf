using DanmakuPlayer.Controls;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using System;
using System.Numerics;
using System.Windows;
using System.Xml.Linq;
using static System.Convert;
using DanmakuPlayer.Services.ExtensionMethods;

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
public record Danmaku(
    float Time,
    DanmakuMode Mode,
    int Size,
    int Color,
    string Text,
    IDWriteTextLayout Layout) : IDisposable
{
    public static readonly WeakReference<FrameworkElement> ViewPort = new(null!);

    // private readonly WeakReference<ID2D1SolidColorBrush> _brush = new(null!);

    public static double ViewWidth => ViewPort.Get().ActualWidth;

    public static double ViewHeight => ViewPort.Get().ActualHeight;

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
    private static float LayoutHeight => DirectHelper.TemplateLayout.Metrics.Height;

    /// <summary>
    /// 视觉区域最多能出现多少弹幕
    /// </summary>
    public static int Count => (int)(ViewHeight / LayoutHeight);

    private float _showPositionY;

    /// <summary>
    /// 从<paramref name="xElement"/>获取弹幕
    /// </summary>
    public static Danmaku CreateDanmaku(XElement xElement)
    {
        var tempInfo = xElement.Attribute("p")!.Value.Split(',');
        return new(
            ToSingle(tempInfo[0]),
            Enum.Parse<DanmakuMode>(tempInfo[1]),
            ToInt32(tempInfo[2]),
            ToInt32(tempInfo[3]),
            xElement.Value,
            DirectHelper.Factory.CreateTextLayout(xElement.Value, DirectHelper.TextFormat, 1000, 32));
    }

    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <returns>是否要显示（取决于<paramref name="appConfig"/>是否允许重叠）</returns>
    public bool RenderInit(DanmakuContext context, AppConfig appConfig)
    {
        // 将要占用空间的索引
        var roomIndex = 0;
        // 是否会覆盖到其他弹幕
        var overlap = true;
        switch (Mode)
        {
            case DanmakuMode.Roll:
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

                if (overlap && !appConfig.DanmakuAllowOverlap)
                    return false;
                context.RollRoom[roomIndex] = (LayoutWidth * appConfig.DanmakuSpeed / (ViewWidth + LayoutWidth)) + Space + Time;
                _showPositionY = roomIndex * LayoutHeight;
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
                    return false;
                context.StaticRoom[roomIndex] = appConfig.DanmakuSpeed + Time;
                _showPositionY = roomIndex * LayoutHeight;
                _staticPosition = new((float)(ViewWidth - LayoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Inverse:
            case DanmakuMode.Advanced:
            case DanmakuMode.Code:
            case DanmakuMode.Bas:
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(Mode);
                break;
        }

        return true;
    }

    /// <summary>
    /// 静止弹幕显示的位置
    /// </summary>
    private Vector2 _staticPosition;

    public void OnRender(ID2D1RenderTarget renderTarget, float time, AppConfig appConfig)
    {
        // if (Time <= time && time - appConfig.Speed < Time)
        switch (Mode)
        {
            case DanmakuMode.Roll:
                renderTarget.DrawTextLayout(new((float)(ViewWidth - ((ViewWidth + LayoutWidth) * (time - Time) / appConfig.DanmakuSpeed)), _showPositionY), Layout, Color.GetBrush());
                break;
            case DanmakuMode.Bottom:
            case DanmakuMode.Top:
                renderTarget.DrawTextLayout(_staticPosition, Layout, Color.GetBrush());
                break;
            case DanmakuMode.Inverse:
            case DanmakuMode.Advanced:
            case DanmakuMode.Code:
            case DanmakuMode.Bas:
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(Mode);
                break;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Layout.Dispose();
    }
}
