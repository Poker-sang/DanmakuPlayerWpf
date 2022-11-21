using DanmakuPlayer.Services;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D9;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DanmakuPlayer.Controls;

/// <summary>
/// <seealso href="https://blog.lindexi.com/post/WPF-使用-SharpDX-在-D3DImage-显示.html"/>
/// </summary>
public abstract partial class SharpDx : FrameworkElement,IDisposable
{
    private readonly D3DImage _d3D = new();
    private SharpDX.Direct3D11.Device _device = null!;
    public DeviceContext D2dContext { get; set; } = null!;

    /// <inheritdoc />
    protected SharpDx()
    {
        Loaded += (_, _) => CreateAndBindTargets((int)ActualWidth, (int)ActualHeight);
        SizeChanged += (_, _) => CreateAndBindTargets((int)ActualWidth, (int)ActualHeight);
    }

    private void CreateAndBindTargets(int actualWidth, int actualHeight)
    {
        var width = Math.Max(actualWidth, 100);
        var height = Math.Max(actualHeight, 100);

        var renderDesc = new SharpDX.Direct3D11.Texture2DDescription
        {
            BindFlags = SharpDX.Direct3D11.BindFlags.RenderTarget | SharpDX.Direct3D11.BindFlags.ShaderResource,
            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            MipLevels = 1,
            SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            Usage = SharpDX.Direct3D11.ResourceUsage.Default,
            OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.Shared,
            CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
            ArraySize = 1
        };

        _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);

        var renderTarget = new SharpDX.Direct3D11.Texture2D(_device, renderDesc);

        var surface = renderTarget.QueryInterface<SharpDX.DXGI.Surface>();

        // var d2DFactory = new Factory();

        // var renderTargetProperties = new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied));

        // _d2DRenderTarget = new RenderTarget(d2DFactory, surface, renderTargetProperties);

        SetRenderTarget(renderTarget);

        _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);

        var creationProperties = new CreationProperties
        {
            DebugLevel = DebugLevel.Error,
            Options = DeviceContextOptions.EnableMultithreadedOptimizations,
            ThreadingMode = ThreadingMode.MultiThreaded,
        };

        D2dContext = new DeviceContext(surface, creationProperties);

        // CompositionTarget.Rendering += (_, _) => Rendering();

        InvalidateVisual();
    }


    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext) => drawingContext.DrawImage(_d3D, new Rect(new Size(_d3D.PixelWidth, _d3D.PixelHeight)));

    protected abstract void OnRender(RenderTarget renderTarget, float time);
    public async void Rendering(float time)
    {
        await Task.Run(() =>
        {
            D2dContext.BeginDraw();
            OnRender(D2dContext, time);
            D2dContext.EndDraw();
            _device.ImmediateContext.Flush();
        });

        _d3D.Lock();
        _d3D.AddDirtyRect(new Int32Rect(0, 0, _d3D.PixelWidth, _d3D.PixelHeight));
        _d3D.Unlock();
        InvalidateVisual();
    }

    private void SetRenderTarget(SharpDX.Direct3D11.Texture2D target)
    {
        var format = TranslateFormat(target);
        var handle = GetSharedHandle(target);

        var d3DDevice = new DeviceEx(new Direct3DEx(),
            0,
            DeviceType.Hardware,
            IntPtr.Zero,
            CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
            new PresentParameters
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = PlatformInvoke.DesktopWindowHandle,
                PresentationInterval = PresentInterval.Default
            });

        var texture = new Texture(d3DDevice, target.Description.Width, target.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref handle);

        using var surface = texture.GetSurfaceLevel(0);

        _d3D.Lock();
        _d3D.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
        _d3D.Unlock();
    }

    private static nint GetSharedHandle(SharpDX.Direct3D11.Texture2D texture)
    {
        using var resource = texture.QueryInterface<SharpDX.DXGI.Resource>();
        return resource.SharedHandle;
    }

    private static Format TranslateFormat(SharpDX.Direct3D11.Texture2D texture) =>
        texture.Description.Format switch
        {
            SharpDX.DXGI.Format.R10G10B10A2_UNorm => Format.A2B10G10R10,
            SharpDX.DXGI.Format.R16G16B16A16_Float => Format.A16B16G16R16F,
            SharpDX.DXGI.Format.B8G8R8A8_UNorm => Format.A8R8G8B8,
            _ => Format.Unknown
        };

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _device.Dispose();
        D2dContext.Dispose();
    }
}
