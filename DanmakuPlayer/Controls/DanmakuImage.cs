using System;
using System.Collections.Generic;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace DanmakuPlayer.Controls;

public class DanmakuImage : DanmakuImageBase
{
    public static IDWriteFactory Factory { get; } = DWrite.DWriteCreateFactory<IDWriteFactory>();

    public static IDWriteTextFormat Format { get; } = Factory.CreateTextFormat("微软雅黑", 20);

    public static Dictionary<int, ID2D1SolidColorBrush> Brush { get; } = new();

    public static ID2D1SolidColorBrush GetBrush(int color, ID2D1RenderTarget renderTarget)
    {
        if (!Brush.TryGetValue(color, out var value))
            Brush[color] = value = renderTarget.CreateSolidColorBrush(new(
                (float)(color & 0xFF0000) / 0xFF0000,
                (float)(color & 0xFF00) / 0xFF00,
                (float)(color & 0xFF) / 0xFF,
                AppContext.Opacity));

        return value;
    }

    protected override void OnRender(ID2D1RenderTarget renderTarget, float time)
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
