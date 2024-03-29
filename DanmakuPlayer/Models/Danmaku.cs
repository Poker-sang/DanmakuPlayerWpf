using DanmakuPlayer.Enums;
using DanmakuPlayer.Services;
using System;
using System.Numerics;
using System.Windows;
using Node = System.Collections.Generic.LinkedListNode<(float BottomPos, double Time)>;
using DanmakuRoomList = System.Collections.Generic.LinkedList<(float BottomPos, double Time)>;

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
public record Danmaku(
    string Text,
    float Time,
    DanmakuMode Mode,
    int Size,
    uint Color,
    ulong UnixTimeStamp,
    DanmakuPool Pool,
    string UserHash)
{
    public static readonly WeakReference<FrameworkElement> ViewPort = new(null!);

    public static double ViewWidth => ViewPort.Get().ActualWidth;

    public static double ViewHeight => ViewPort.Get().ActualHeight;

    public float LayoutHeight => Size.GetLayoutHeights();

    /// <summary>
    /// 同行两条滚动弹幕间距时间(second)
    /// </summary>
    public const int Space = 3;

    /// <summary>
    /// 是否需要渲染（取决于是否允许重叠）
    /// </summary>
    public bool NeedRender { get; private set; } = true;

    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <returns>是否需要渲染</returns>
    public bool RenderInit(DanmakuContext context, AppConfig appConfig)
    {
        var layoutExists = DirectHelper.Layouts.ContainsKey(ToString());
        var layout = layoutExists ? DirectHelper.Layouts[ToString()] : this.GetNewLayout();
        var layoutWidth = layout.Metrics.Width;
        // 如果覆盖了，并且不允许覆盖，则返回true，否则false
        bool OverlapPredicate(bool overlap)
        {
            // 是否覆盖
            if (!overlap || appConfig.DanmakuAllowOverlap)
                return false;
            if (!layoutExists)
                layout.Dispose();
            return true;
        };

        NeedRender = false;
        switch (Mode)
        {
            case DanmakuMode.Roll:
                if (!TopDownDanmaku(context.RollRoom, (layoutWidth * appConfig.DanmakuDuration / (ViewWidth + layoutWidth)) + Space + Time, OverlapPredicate))
                    return false;
                break;
            case DanmakuMode.Bottom:
                if (!BottomUpDanmaku(context.StaticRoom, appConfig.DanmakuDuration + Time, OverlapPredicate))
                    return false;
                _staticPosition = new((float)(ViewWidth - layoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Top:
                if (!TopDownDanmaku(context.StaticRoom, appConfig.DanmakuDuration + Time, OverlapPredicate))
                    return false;
                _staticPosition = new((float)(ViewWidth - layoutWidth) / 2, _showPositionY);
                break;
            case DanmakuMode.Inverse:
                if (!TopDownDanmaku(context.InverseRoom, (layoutWidth * appConfig.DanmakuDuration / (ViewWidth + layoutWidth)) + Space + Time, OverlapPredicate))
                    return false;
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
        return true;
    }

    public void OnRender(Vortice.Direct2D1.ID2D1RenderTarget renderTarget, float time, AppConfig appConfig)
    {
        // 外部实现逻辑：if (Time <= time && time - appConfig.Speed < Time)
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

    #region 私有成员

    /// <summary>
    /// 静止弹幕显示的位置
    /// </summary>
    private Vector2 _staticPosition;

    /// <summary>
    /// 弹幕显示的Y坐标
    /// </summary>
    private float _showPositionY;

    /// <summary>
    /// 覆盖区间的时间都低于最小的时间，并且不超出屏幕高度。如果返回true，则一定是目前的最优位置
    /// </summary>
    /// <param name="node"></param>
    /// <param name="startPos"></param>
    /// <param name="leastTime"></param>
    /// <param name="lastNode"></param>
    /// <param name="overlapped"></param>
    /// <returns></returns>
    private bool TopDownCanBePlaced(Node? node, float startPos, ref double leastTime, out Node? lastNode, out bool overlapped)
    {
        var intervalMostTime = (double)-Space;
        lastNode = null;
        overlapped = false;
        for (; node is not null; node = node.Next)
        {
            // 高于目前最小时间
            if (node.Value.Time > leastTime)
                return false;
            intervalMostTime = Math.Max(intervalMostTime, node.Value.Time);
            if (LayoutHeight < node.Value.BottomPos - startPos)
            {
                lastNode = node;
                // intervalMostTime <= refLeastTime
                leastTime = intervalMostTime;
                overlapped = Time < leastTime;
                // 既没有高于最小时间，也没有超过屏幕高度
                return true;
            }
        }
        // 超过屏幕
        return false;
    }

    /// <summary>
    /// 自上至下插入弹幕
    /// </summary>
    /// <param name="list"></param>
    /// <param name="occupyTime"></param>
    /// <param name="overlapPredicate"></param>
    /// <returns></returns>
    private bool TopDownDanmaku(DanmakuRoomList list, double occupyTime, Func<bool, bool> overlapPredicate)
    {
        // 所有区间中，最短的显示时间
        var leastTime = double.PositiveInfinity;
        // 本区间开始位置（上一个区间的结束位置）
        var lastPos = 0f;
        // 插入的第一个区间（靠上）
        var firstNode = (Node?)null;
        // 插入的最后一个区间（靠下）
        var lastNode = (Node?)null;
        // 应该放置的位置（取左上角位置）
        var pos = 0f;
        // 是否重叠
        var overlap = true;
        for (var current = list.First; current is not null; current = current.Next)
        {
            // 循环中一定会进一次该if分支
            if (TopDownCanBePlaced(current, lastPos, ref leastTime, out var outLastNode, out var overlapped))
            {
                firstNode = current;
                lastNode = outLastNode;
                pos = lastPos;
                overlap = overlapped;
                // 如果没有重叠，就采用这个位置
                if (!overlap)
                    break;
            }
            lastPos = current.Value.BottomPos;
        }

        if (overlapPredicate(overlap))
            return false;

        _showPositionY = pos;
        list.AddBefore(firstNode!, new Node((pos + LayoutHeight, occupyTime)));
        for (var current = firstNode!; current != lastNode!;)
        {
            current = current.Next!;
            list.Remove(current.Previous!);
        }
        return true;
    }

    private bool BottomUpCanBePlaced(Node? node, float startPos, ref double leastTime, out Node? lastNode, out bool overlapped)
    {
        var intervalMostTime = (double)-Space;
        lastNode = null;
        overlapped = false;
        for (; node is not null; node = node.Previous)
        {
            // 高于目前最小时间
            if (node.Value.Time > leastTime)
                return false;
            intervalMostTime = Math.Max(intervalMostTime, node.Value.Time);
            if (LayoutHeight < startPos - (node.Previous?.Value.BottomPos ?? 0))
            {
                lastNode = node;
                // intervalMostTime <= refLeastTime
                leastTime = intervalMostTime;
                overlapped = Time < leastTime;
                // 既没有高于最小时间，也没有超过屏幕高度
                return true;
            }
        }
        // 超过屏幕
        return false;
    }

    /// <summary>
    /// 自下至上插入弹幕
    /// </summary>
    /// <param name="list"></param>
    /// <param name="occupyTime"></param>
    /// <param name="overlapPredicate"></param>
    /// <returns></returns>
    private bool BottomUpDanmaku(DanmakuRoomList list, double occupyTime, Func<bool, bool> overlapPredicate)
    {
        // 所有区间中，最短的显示时间
        var leastTime = double.PositiveInfinity;
        // 插入的第一个区间（靠下）
        var firstNode = (Node?)null;
        // 插入的最后一个区间（靠上）
        var lastNode = (Node?)null;
        // 应该放置的位置（取左下角位置）
        var pos = 0f;
        // 是否重叠
        var overlap = true;
        for (var current = list.Last; current is not null; current = current.Previous)
        {
            // 本区间开始位置
            var lastPos = current.Value.BottomPos;
            // 循环中一定会进一次该if分支
            if (BottomUpCanBePlaced(current, lastPos, ref leastTime, out var outLastNode, out var overlapped))
            {
                firstNode = current;
                lastNode = outLastNode;
                pos = lastPos;
                overlap = overlapped;
                // 如果没有重叠，就采用这个位置
                if (!overlap)
                    break;
            }
        }

        if (overlapPredicate(overlap))
            return false;

        _showPositionY = pos - LayoutHeight;
        list.AddAfter(firstNode!, new Node((pos, occupyTime)));
        list.AddBefore(lastNode!, new Node((_showPositionY, lastNode!.Value.Time)));
        for (var current = firstNode!; current != lastNode;)
        {
            current = current.Previous!;
            list.Remove(current.Next!);
        }
        list.Remove(lastNode);
        return true;
    }

    #endregion
}
