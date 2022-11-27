using DanmakuPlayer.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Direct3D9;
using Vortice.DXGI;

namespace DanmakuPlayer.Controls;

/// <summary>
/// <seealso href="https://blog.lindexi.com/post/WPF-使用-SharpDX-在-D3DImage-显示.html"/>
/// </summary>
public abstract class DanmakuImageBase : FrameworkElement, IDisposable
{
    private readonly D3DImage _d3D = new();

    private ID3D11Device _device = null!;

    public ID2D1DeviceContext D2dContext { get; set; } = null!;

    public bool CancelRender;

    /// <inheritdoc />
    protected DanmakuImageBase() => Loaded += (_, _) =>
    {
        DirectHelper.CreateDevice(out _device);
        CreateAndBindTargets((int)ActualWidth, (int)ActualHeight);
    };

    private void CreateAndBindTargets(int actualWidth, int actualHeight)
    {
        var width = Math.Max(actualWidth, 100);
        var height = Math.Max(actualHeight, 100);

        var renderDesc = new Texture2DDescription
        {
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            MipLevels = 1,
            SampleDescription = new(1, 0),
            Usage = ResourceUsage.Default,
            MiscFlags = ResourceOptionFlags.Shared,
            CPUAccessFlags = CpuAccessFlags.None,
            ArraySize = 1
        };

        using var texture = _device.CreateTexture2D(renderDesc);

        using var surface = texture.QueryInterface<IDXGISurface>();

        SetRenderTarget(texture);

        _device.ImmediateContext.RSSetViewport(0, 0, width, height);

        var creationProperties = new CreationProperties
        {
            DebugLevel = DebugLevel.Error,
            Options = DeviceContextOptions.EnableMultithreadedOptimizations,
            ThreadingMode = ThreadingMode.MultiThreaded,
        };

        DirectHelper.RenderTarget = D2dContext = D2D1.D2D1CreateDeviceContext(surface, creationProperties);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext) => drawingContext.DrawImage(_d3D, new(new(_d3D.PixelWidth, _d3D.PixelHeight)));

    protected abstract void OnRender(ID2D1RenderTarget renderTarget, float time, AppConfig appConfig);

    public async void Rendering(float time, AppConfig appConfig)
    {
        if (CancelRender)
        {
            D2dContext.SafeRelease();
            CreateAndBindTargets((int)ActualWidth, (int)ActualHeight);
            CancelRender = false;
        }

        await Task.Run(() =>
        {
            D2dContext.BeginDraw();
            OnRender(D2dContext, time, appConfig);
            _ = D2dContext.EndDraw();
            _device.ImmediateContext.Flush();
        });

        _d3D.Lock();
        _d3D.AddDirtyRect(new(0, 0, _d3D.PixelWidth, _d3D.PixelHeight));
        _d3D.Unlock();
        InvalidateVisual();
    }

    private void SetRenderTarget(ID3D11Texture2D target)
    {
        var format = DirectHelper.TranslateFormat(target);
        var handle = DirectHelper.GetSharedHandle(target);

        using var d3d9Ex = D3D9.Direct3DCreate9Ex();
        using var d3DDevice = d3d9Ex.CreateDeviceEx(
            0,
            DeviceType.Hardware,
            IntPtr.Zero,
            CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
            new Vortice.Direct3D9.PresentParameters
            {
                Windowed = true,
                SwapEffect = Vortice.Direct3D9.SwapEffect.Discard,
                DeviceWindowHandle = PlatformInvoke.DesktopWindowHandle,
                PresentationInterval = PresentInterval.Default
            });

        using var texture = d3DDevice.CreateTexture(target.Description.Width, target.Description.Height, 1,
            Vortice.Direct3D9.Usage.RenderTarget, format, Pool.Default, ref handle);

        using var surface = texture.GetSurfaceLevel(0);

        try
        {
            _d3D.Lock();
            _d3D.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
        }
        finally
        {
            _d3D.Unlock();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _d3D.Freeze();
        _d3D.Lock();
        D2dContext.SafeRelease();
        _device.SafeRelease();
    }
}
