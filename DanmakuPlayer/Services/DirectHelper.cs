using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DXGI;

namespace DanmakuPlayer.Services;

public static class DirectHelper
{
    public static IDWriteFactory Factory { get; } = DWrite.DWriteCreateFactory<IDWriteFactory>();

    public static IDWriteTextFormat TextFormat { get; } = Factory.CreateTextFormat("微软雅黑", 20);

    public static IDWriteTextLayout TemplateLayout { get; } = Factory.CreateTextLayout("模板Template", TextFormat, 1000, 50);

    public static Dictionary<int, ID2D1SolidColorBrush> Brush { get; } = new();

    public static ID2D1RenderTarget? RenderTarget
    {
        get => _renderTarget;
        set
        {
            Brush.Clear();
            _renderTarget = value;
        }
    }

    public static ID2D1SolidColorBrush GetBrush(this int color)
    {
        ArgumentNullException.ThrowIfNull(RenderTarget);
        if (!Brush.TryGetValue(color, out var value))
            Brush[color] = value = RenderTarget.CreateSolidColorBrush(new(
                (float)(color & 0xFF0000) / 0xFF0000,
                (float)(color & 0xFF00) / 0xFF00,
                (float)(color & 0xFF) / 0xFF,
                App.AppConfig.DanmakuOpacity));

        return value;
    }

    public static void SafeRelease(this CppObject? obj)
    {
        if (obj is { NativePointer: not (nint)0 })
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

    private static ID2D1RenderTarget? _renderTarget;

    public static void CreateDevice(out ID3D11Device device)
    {
        if (D3D11.D3D11CreateDevice(IntPtr.Zero, DriverType.Hardware, DeviceCreationFlags.BgraSupport, _featureLevels, out device, out _).Failure)
            throw new();
    }
}
