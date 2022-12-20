using DanmakuPlayer.Models;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace DanmakuPlayer.Services;

public static class DirectHelper
{
    public static IDWriteFactory Factory { get; } = DWrite.DWriteCreateFactory<IDWriteFactory>();

    private static ID2D1RenderTarget? _renderTarget;

    public static ID2D1RenderTarget? RenderTarget
    {
        get => _renderTarget;
        set
        {
            _renderTarget = value;
            ClearBrushes();
        }
    }

    /// <summary>
    /// 颜色和对应笔刷
    /// </summary>
    /// <remarks>依赖于<see cref="RenderTarget"/>、<see cref="AppConfig.DanmakuOpacity"/></remarks>
    public static Dictionary<int, ID2D1SolidColorBrush> Brushes { get; } = new();

    /// <summary>
    /// 字号和对应字体
    /// </summary>
    /// <remarks>依赖于<see cref="AppConfig.DanmakuFont"/>、<see cref="AppConfig.DanmakuScale"/></remarks>
    public static Dictionary<float, IDWriteTextFormat> TextFormats { get; } = new();

    /// <summary>
    /// 内容和对应渲染布局
    /// </summary>
    /// <remarks>依赖于<see cref="RenderTarget"/>、<see cref="TextFormats"/></remarks>
    public static Dictionary<string, IDWriteTextLayout> Layouts { get; } = new();

    /// <summary>
    /// 字号和对应文本框高度
    /// </summary>
    /// <remarks>依赖于<see cref="Layouts"/></remarks>
    public static Dictionary<int, float> LayoutHeights { get; } = new();

    #region Set类方法

    /// <summary>
    /// 重新加载<see cref="Brushes"/>
    /// </summary>
    public static void ClearBrushes()
    {
        foreach (var brush in Brushes)
            brush.Value.Dispose();
        Brushes.Clear();

        if (RenderTarget is null)
            return;

        // 白色
        Brushes[0xFFFFFF] = RenderTarget.CreateSolidColorBrush(new Color(0xFF, 0xFF, 0xFF, App.AppConfig.DanmakuOpacity));
        // 黄色
        Brushes[0xFFFF00] = RenderTarget.CreateSolidColorBrush(new Color(0xFF, 0xFF, 0x00, App.AppConfig.DanmakuOpacity));
        // 红色
        Brushes[0xFE0302] = RenderTarget.CreateSolidColorBrush(new Color(0xFE, 0x03, 0x02, App.AppConfig.DanmakuOpacity));
        // 蓝色
        Brushes[0x4266BE] = RenderTarget.CreateSolidColorBrush(new Color(0x42, 0x66, 0xBE, App.AppConfig.DanmakuOpacity));
        // 绿色
        Brushes[0x00CD00] = RenderTarget.CreateSolidColorBrush(new Color(0x00, 0xCD, 0x00, App.AppConfig.DanmakuOpacity));
        // 橘色
        Brushes[0xFF7204] = RenderTarget.CreateSolidColorBrush(new Color(0xFF, 0x72, 0x04, App.AppConfig.DanmakuOpacity));
        // 紫色
        Brushes[0xCC0273] = RenderTarget.CreateSolidColorBrush(new Color(0xCC, 0x02, 0x72, App.AppConfig.DanmakuOpacity));
    }

    /// <summary>
    /// 依次重新加载<see cref="LayoutHeights"/>
    /// </summary>
    public static void ClearLayoutHeights()
    {
        LayoutHeights.Clear();
        using var layout = Factory.CreateTextLayout("模板Template", TextFormats[25], 1000, 100);
        using var layout2 = Factory.CreateTextLayout("模板Template", TextFormats[18], 1000, 100);
        LayoutHeights.Add(25, layout.Metrics.Height);
        LayoutHeights.Add(18, layout2.Metrics.Height);
    }

    /// <summary>
    /// 依次重新加载<see cref="Layouts"/>、<see cref="LayoutHeights"/>
    /// </summary>
    public static void ClearLayouts()
    {
        foreach (var layout in Layouts)
            layout.Value.Dispose();
        Layouts.Clear();
        ClearLayoutHeights();
    }

    /// <summary>
    /// 依次重新加载<see cref="TextFormats"/>、<see cref="Layouts"/>、<see cref="LayoutHeights"/>
    /// </summary>
    public static void ClearTextFormats()
    {
        foreach (var textFormat in TextFormats)
            textFormat.Value.Dispose();
        TextFormats.Clear();

        TextFormats[25] = Factory.CreateTextFormat(App.AppConfig.DanmakuFont, 25 * App.AppConfig.DanmakuScale);
        TextFormats[18] = Factory.CreateTextFormat(App.AppConfig.DanmakuFont, 18 * App.AppConfig.DanmakuScale);
        ClearLayouts();
    }

    #endregion

    #region Get类方法

    public static IDWriteTextFormat GetTextFormat(this int size)
    {
        ArgumentNullException.ThrowIfNull(RenderTarget);
        if (!TextFormats.TryGetValue(size, out var value))
            TextFormats[size] = value = Factory.CreateTextFormat(App.AppConfig.DanmakuFont, size * App.AppConfig.DanmakuScale);
        return value;
    }

    public static unsafe ID2D1SolidColorBrush GetBrush(this in int color)
    {
        ArgumentNullException.ThrowIfNull(RenderTarget);
        fixed (int* ptr = &color)
        {
            var c = (byte*)ptr;
            if (!Brushes.TryGetValue(color, out var value))
                Brushes[color] = value = RenderTarget.CreateSolidColorBrush(new(
                    (float)c[2] / 0xFF,
                    (float)c[1] / 0xFF,
                    (float)c[0] / 0xFF,
                    App.AppConfig.DanmakuOpacity));
            return value;
        }
    }

    public static IDWriteTextLayout GetNewLayout(this Danmaku danmaku)
        => Factory.CreateTextLayout(danmaku.Text, danmaku.Size.GetTextFormat(), 1000, 100);

    #endregion

    #region D3dImage

    public static void SafeRelease(this CppObject? obj)
    {
        if (obj is { NativePointer: not 0 })
            obj.Dispose();
    }

    public static nint GetSharedHandle(ID3D11Texture2D texture)
    {
        using var resource = texture.QueryInterface<IDXGIResource>();
        return resource.SharedHandle;
    }

    public static Vortice.Direct3D9.Format TranslateFormat(ID3D11Texture2D texture) =>
        texture.Description.Format switch
        {
            Format.R10G10B10A2_UNorm => Vortice.Direct3D9.Format.A2B10G10R10,
            Format.R16G16B16A16_Float => Vortice.Direct3D9.Format.A16B16G16R16F,
            Format.B8G8R8A8_UNorm => Vortice.Direct3D9.Format.A8R8G8B8,
            _ => Vortice.Direct3D9.Format.Unknown
        };

    /// <summary>
    /// <seealso href="https://github.com/amerkoleci/Vortice.Windows.Samples/blob/main/src/Vortice.Framework/D3D11Application.cs"/>
    /// </summary>
    private static readonly Vortice.Direct3D.FeatureLevel[] _featureLevels =
    {
        Vortice.Direct3D.FeatureLevel.Level_11_1,
        Vortice.Direct3D.FeatureLevel.Level_11_0,
        Vortice.Direct3D.FeatureLevel.Level_10_1,
        Vortice.Direct3D.FeatureLevel.Level_10_0
    };

    public static void CreateDevice(out ID3D11Device device)
    {
        if (D3D11.D3D11CreateDevice(0, DriverType.Hardware, DeviceCreationFlags.BgraSupport, _featureLevels, out device, out _).Failure)
            throw new();
    }

    #endregion
}
