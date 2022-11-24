using DanmakuPlayer.Services;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Vortice.Direct3D11;
using Vortice.Direct3D9;
using Vortice.DXGI;
using SharpGen.Runtime;
using System.Windows.Media.Effects;
using Windows.Graphics.DirectX.Direct3D11;

namespace DanmakuPlayer.Controls;

/// <summary>
/// <seealso href="https://blog.lindexi.com/post/WPF-使用-SharpDX-在-D3DImage-显示.html"/>
/// </summary>
public abstract class DanmakuImageBase : FrameworkElement, IDisposable
{
    private readonly D3DImage _d3D = new();

    private ID3D11Device _device = null!;

    public ID2D1DeviceContext D2dContext { get; set; } = null!;

    private bool _cancelRender;

    /// <summary>
    /// <seealso href="https://github.com/amerkoleci/Vortice.Windows.Samples/blob/main/src/Vortice.Framework/D3D11Application.cs"/>
    /// </summary>
    private static readonly Vortice.Direct3D.FeatureLevel[] _featureLevels = {
        Vortice.Direct3D.FeatureLevel.Level_11_1,
        Vortice.Direct3D.FeatureLevel.Level_11_0,
        Vortice.Direct3D.FeatureLevel.Level_10_1,
        Vortice.Direct3D.FeatureLevel.Level_10_0
    };

    /// <inheritdoc />
    protected DanmakuImageBase() => Loaded += (_, _) => CreateAndBindTargets((int)ActualWidth, (int)ActualHeight);

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

        if (D3D11.D3D11CreateDevice(IntPtr.Zero, DriverType.Hardware, DeviceCreationFlags.BgraSupport, _featureLevels, out _device, out var immediateContext).Failure)
            throw new();

        /*using*/ var texture2D = _device.CreateTexture2D(renderDesc);

        /*using*/ var surface = texture2D.QueryInterface<IDXGISurface>();

        // var d2DFactory = new Factory();

        // var renderTargetProperties = new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied));

        // _d2DRenderTarget = new RenderTarget(d2DFactory, surface, renderTargetProperties);

        SetRenderTarget(texture2D);

        immediateContext.RSSetViewport(0, 0, width, height);

        var creationProperties = new CreationProperties
        {
            DebugLevel = DebugLevel.Error,
            Options = DeviceContextOptions.EnableMultithreadedOptimizations,
            ThreadingMode = ThreadingMode.MultiThreaded,
        };

        D2dContext = D2D1.D2D1CreateDeviceContext(surface, creationProperties);

        // CompositionTarget.Rendering += (_, _) => Rendering();

        // InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        if (!IsLoaded)
            return;
        drawingContext.DrawImage(_d3D, new(new(_d3D.PixelWidth, _d3D.PixelHeight)));
    }

    protected abstract void OnRender(ID2D1RenderTarget renderTarget, float time);

    public async void Rendering(float time)
    {
        if (!IsLoaded)
            return;
        if (_cancelRender)
        {
            // CreateAndBindTargets((int)ActualWidth, (int)ActualHeight);
            Dispose();
            return;
        }

        await Task.Run(() =>
        {
            D2dContext.BeginDraw();
            OnRender(D2dContext, time);
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
        var format = TranslateFormat(target);
        var handle = GetSharedHandle(target);

        /*using*/ var d3d9Ex = D3D9.Direct3DCreate9Ex();
        /*using*/ var d3DDevice = d3d9Ex.CreateDeviceEx(
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

        /*using*/ var texture = d3DDevice.CreateTexture(target.Description.Width, target.Description.Height, 1,
            Vortice.Direct3D9.Usage.RenderTarget, format, Pool.Default, ref handle);

        /*using*/ var surface = texture.GetSurfaceLevel(0);

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

    private static nint GetSharedHandle(ID3D11Texture2D texture)
    {
        using var resource = texture.QueryInterface<IDXGIResource>();
        return resource.SharedHandle;
    }

    private static Vortice.Direct3D9.Format TranslateFormat(ID3D11Texture2D texture) =>
        texture.Description.Format switch
        {
            Vortice.DXGI.Format.R10G10B10A2_UNorm => Vortice.Direct3D9.Format.A2B10G10R10,
            Vortice.DXGI.Format.R16G16B16A16_Float => Vortice.Direct3D9.Format.A16B16G16R16F,
            Vortice.DXGI.Format.B8G8R8A8_UNorm => Vortice.Direct3D9.Format.A8R8G8B8,
            _ => Vortice.Direct3D9.Format.Unknown
        };

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _d3D.Freeze();
        _d3D.Lock();
        SafeRelease(D2dContext);
        SafeRelease(_device);
    }

    private static void SafeRelease(CppObject? obj)
    {
        if (obj is { NativePointer: not (nint)0 })
            obj.Dispose();
    }
}
