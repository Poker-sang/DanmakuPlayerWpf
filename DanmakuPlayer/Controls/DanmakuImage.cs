using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;

namespace DanmakuPlayer.Controls;

public class DanmakuImage : SharpDx
{
    public static SharpDX.DirectWrite.Factory Factory { get; } = new();

    public static TextFormat Format { get; } = new(Factory, "微软雅黑", 20);

    public static Dictionary<int, SolidColorBrush> Brush { get; } = new();

    public static SolidColorBrush GetBrush(int color, RenderTarget renderTarget)
    {
        if (!Brush.TryGetValue(color, out var value))
            Brush[color] = value = new(renderTarget, new RawColor4(
                (float)(color & 0xFF0000) / 0xFF0000,
                (float)(color & 0xFF00) / 0xFF00,
                (float)(color & 0xFF) / 0xFF,
                AppContext.Opacity));

        return value;
    }

    protected override void OnRender(RenderTarget renderTarget, float time)
    {
        renderTarget.Clear(null);

        var firstIndex = Array.FindIndex(App.Pool, t => t.Time > time - AppContext.Speed);
        if (firstIndex is -1)
            return;
        var lastIndex = Array.FindLastIndex(App.Pool, t => t.Time <= time);
        if (lastIndex < firstIndex)
            return;
        foreach (var t in App.Pool[firstIndex..(lastIndex + 1)])
            t.OnRender(renderTarget, time);
    }
}
