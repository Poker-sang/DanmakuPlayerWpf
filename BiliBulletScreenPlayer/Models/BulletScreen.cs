using BiliBulletScreenPlayer.Services;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Windows;
using System.Xml.Linq;
using static System.Convert;

namespace BiliBulletScreenPlayer.Models;

/// <summary>
/// 弹幕
/// </summary>
/// <param name="Time"> 出现时间</param>
/// <param name="Mode">模式（4：底端，5：顶端，其他：滚动）</param>
/// <param name="Size">大小</param>
/// <param name="Color">颜色</param>
/// <param name="Text">内容</param>
/// <param name="Layout">拥有的文本框</param>
internal record BulletScreen(
    float Time,
    int Mode,
    int Size,
    int Color,
    string Text,
    TextLayout Layout)
{
    public static FrameworkElement ViewPort;
    public static double ViewWidth => ViewPort.ActualWidth;
    public static double ViewHeight => ViewPort.ActualHeight;

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
    private float LayoutHeight => Layout.Metrics.Height;

    private SolidColorBrush _brush;

    private float _showPositionY;

    public static BulletScreen CreateBulletScreen(XElement xElement)
    {
        var tempInfo = xElement.Attribute("p")!.Value.Split(',');
        return new(
         ToSingle(tempInfo[0]),
         ToInt32(tempInfo[1]),
         ToInt32(tempInfo[2]),
         ToInt32(tempInfo[3]),
         xElement.Value,
         new(SharpDx.Factory, xElement.Value, SharpDx.Format, 1000, 32));
    }

    public void RenderInit(RenderTarget renderTarget, BulletScreenContext context)
    {
        _brush = new(renderTarget, new RawColor4((float)(Color & 0xFF0000) / 0xFF0000, (float)(Color & 0xFF00) / 0xFF00, (float)(Color & 0xFF) / 0xFF, GlobalSettings.Opacity));

        // 将要占用空间的索引
        var roomIndex = 0;
        // 是否会覆盖到其他弹幕
        var overlap = true;
        // 视觉区域最多能出现多少弹幕
        var count = (int)(ViewHeight / LayoutHeight);
        switch (Mode)
        {
            //底部
            case 4:
                for (var i = count - 1; i >= 0; --i)
                    // 如果下一空间还从未有过弹幕
                    if (i >= context.StaticRoom.Count)
                    {
                        roomIndex = i;
                        while (i >= context.StaticRoom.Count)
                            context.StaticRoom.Add(GlobalSettings.Speed + Time);
                        overlap = false;
                        break;
                    }
                    // 如果下一空间已经过了被占用时间
                    else if (Time >= context.StaticRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 找出距离结束占用最快的空间
                    else if (context.StaticRoom[roomIndex] > context.StaticRoom[i])
                        roomIndex = i;

                if (overlap && !GlobalSettings.AllowOverlap)
                    return;
                context.StaticRoom[roomIndex] = GlobalSettings.Speed + Time;
                break;
            //顶部
            case 5:
                // 在窗口大小内从上往下遍历
                for (var i = 0; i < count; ++i)
                    // 如果下一空间还从未有过弹幕
                    if (i >= context.StaticRoom.Count)
                    {
                        context.StaticRoom.Add(GlobalSettings.Speed + Time);
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 如果下一空间已经过了被占用时间
                    else if (Time >= context.StaticRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 找出距离结束占用最快的空间
                    else if (context.StaticRoom[roomIndex] > context.StaticRoom[i])
                        roomIndex = i;

                if (overlap && !GlobalSettings.AllowOverlap)
                    return;
                context.StaticRoom[roomIndex] = GlobalSettings.Speed + Time;
                break;
            //滚动
            default:
                // 在窗口大小内从上往下遍历
                for (var i = 0; i < count; ++i)
                    // 如果下一空间还从未有过弹幕
                    if (i >= context.RollRoom.Count)
                    {
                        context.RollRoom.Add(0);
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 如果下一空间已经过了被占用时间
                    else if (Time >= context.RollRoom[i])
                    {
                        roomIndex = i;
                        overlap = false;
                        break;
                    }
                    // 找出距离结束占用最快的空间
                    else if (context.RollRoom[roomIndex] > context.RollRoom[i])
                        roomIndex = i;

                if (overlap && !GlobalSettings.AllowOverlap)
                    return;
                context.RollRoom[roomIndex] = LayoutWidth * GlobalSettings.Speed / (ViewWidth + LayoutWidth) + Space + Time;
                break;
        }

        _showPositionY = roomIndex * LayoutHeight;
    }

    public bool OnRender(RenderTarget renderTarget, float timeNow)
    {
        if (0 <= timeNow - Time && timeNow - Time < GlobalSettings.Speed)
        {
            switch (Mode)
            {
                //底部
                case 4:
                //顶部
                case 5:
                    renderTarget.DrawTextLayout(new RawVector2((float)(ViewWidth - LayoutWidth) / 2, _showPositionY),
                        Layout, _brush);
                    break;
                //滚动
                default:
                    Debug.WriteLine(ViewWidth - (ViewWidth + LayoutWidth) * (timeNow - Time) / GlobalSettings.Speed);
                    Debug.WriteLine(_showPositionY);
                    renderTarget.DrawTextLayout(
                        new RawVector2(
                            (float)(ViewWidth - (ViewWidth + LayoutWidth) * (timeNow - Time) / GlobalSettings.Speed),
                            _showPositionY), Layout, _brush);
                    break;
            }

            return true;
        }

        return false;
    }
}
