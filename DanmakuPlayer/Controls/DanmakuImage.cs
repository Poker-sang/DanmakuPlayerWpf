using System;
using Vortice.Direct2D1;

namespace DanmakuPlayer.Controls;

public class DanmakuImage : DanmakuImageBase
{
    protected override void OnRender(ID2D1RenderTarget renderTarget, float time, AppConfig appConfig)
    {
        renderTarget.Clear(null);

        var firstIndex = Array.FindIndex(App.Pool, t => t.Time > time - App.AppConfig.DanmakuSpeed);
        if (firstIndex is -1)
            return;
        var lastIndex = Array.FindLastIndex(App.Pool, t => t.Time <= time);
        if (lastIndex < firstIndex)
            return;
        foreach (var t in App.Pool[firstIndex..(lastIndex + 1)])
            t.OnRender(renderTarget, time, appConfig);
    }
}
