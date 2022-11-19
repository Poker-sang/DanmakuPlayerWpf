using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using System;
using System.Windows;
using System.Windows.Interop;

namespace BiliBulletScreenPlayer.Services;

public static class SharpDx
{
    public static SharpDX.DirectWrite.Factory Factory { get; } = new();

    public static TextFormat Format { get; } = new(Factory, "微软雅黑", 20)
    {
        TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
    };

    public static RenderTarget CreateAndBindTargets(MainWindow window)
    {
        var width = Math.Max((int)window.ActualWidth, 100);
        var height = Math.Max((int)window.ActualHeight, 100);

        var renderDesc = new Texture2DDescription
        {
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            OptionFlags = ResourceOptionFlags.Shared,
            CpuAccessFlags = CpuAccessFlags.None,
            ArraySize = 1
        };

        var device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

        var renderTarget = new Texture2D(device, renderDesc);

        var surface = renderTarget.QueryInterface<SharpDX.DXGI.Surface>();

        var d2DFactory = new SharpDX.Direct2D1.Factory();

        var renderTargetProperties =
            new RenderTargetProperties(new PixelFormat(SharpDX.DXGI.Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));

        var d2DRenderTarget = new RenderTarget(d2DFactory, surface, renderTargetProperties);

        SetRenderTarget(renderTarget, window);

        device.ImmediateContext.Rasterizer.SetViewport(0, 0, (int)window.ActualWidth, (int)window.ActualHeight);

        return d2DRenderTarget;
    }

    private static void SetRenderTarget(Texture2D target, MainWindow window)
    {
        var format = TranslateFormat(target);
        var handle = GetSharedHandle(target);

        var presentParams = GetPresentParameters(window);
        const CreateFlags createFlags = CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve;

        var d3DContext = new Direct3DEx();
        var d3DDevice = new DeviceEx(d3DContext, 0, DeviceType.Hardware, IntPtr.Zero, createFlags,
            presentParams);

        var renderTarget = new Texture(d3DDevice, target.Description.Width, target.Description.Height, 1,
              SharpDX.Direct3D9.Usage.RenderTarget, format, Pool.Default, ref handle);

        using var surface = renderTarget.GetSurfaceLevel(0);
        window.KsyosqStmckfy.Lock();
        window.KsyosqStmckfy.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
        window.KsyosqStmckfy.Unlock();
    }

    private static SharpDX.Direct3D9.PresentParameters GetPresentParameters(Window window) =>
        new()
        {
            Windowed = true,
            SwapEffect = SharpDX.Direct3D9.SwapEffect.Discard,
            DeviceWindowHandle = new WindowInteropHelper(window).Handle,
            PresentationInterval = PresentInterval.Default
        };

    private static IntPtr GetSharedHandle(Texture2D texture)
    {
        using var resource = texture.QueryInterface<SharpDX.DXGI.Resource>();
        return resource.SharedHandle;
    }

    private static SharpDX.Direct3D9.Format TranslateFormat(Texture2D texture)
        => texture.Description.Format switch
        {
            SharpDX.DXGI.Format.R10G10B10A2_UNorm => SharpDX.Direct3D9.Format.A2B10G10R10,
            SharpDX.DXGI.Format.R16G16B16A16_Float => SharpDX.Direct3D9.Format.A16B16G16R16F,
            SharpDX.DXGI.Format.B8G8R8A8_UNorm => SharpDX.Direct3D9.Format.A8R8G8B8,
            _ => SharpDX.Direct3D9.Format.Unknown,
        };
}
