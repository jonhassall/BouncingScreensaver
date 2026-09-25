using System.Drawing;
using System.Drawing.Imaging;
using BouncingScreensaver.Core;
using Vortice;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct2D1.D2D1;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;
using D2DAlphaMode = Vortice.DCommon.AlphaMode;
using D2DPixelFormat = Vortice.DCommon.PixelFormat;
using DxgiAlphaMode = Vortice.DXGI.AlphaMode;
using DxgiFormat = Vortice.DXGI.Format;
using GdiColor = System.Drawing.Color;
using GdiPixelFormat = System.Drawing.Imaging.PixelFormat;
using GdiSize = System.Drawing.Size;
using D3DFeatureLevel = Vortice.Direct3D.FeatureLevel;

namespace BouncingScreensaver.Windows.UI;

/// <summary>
/// Direct3D 11 swap-chain surface with a Direct2D render target.
/// </summary>
internal sealed class Direct2DRenderSurface : IDisposable
{
    private static readonly D3DFeatureLevel[] FeatureLevels =
    [
        D3DFeatureLevel.Level_11_0,
        D3DFeatureLevel.Level_10_1,
        D3DFeatureLevel.Level_10_0,
        D3DFeatureLevel.Level_9_3,
        D3DFeatureLevel.Level_9_2,
        D3DFeatureLevel.Level_9_1
    ];

    private readonly IntPtr _hwnd;
    private readonly IDXGIFactory2 _factory;
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _deviceContext;
    private readonly IDXGISwapChain1 _swapChain;
    private readonly ID2D1Factory _d2dFactory;
    private ID2D1RenderTarget? _renderTarget;
    private ID2D1Bitmap? _logoBitmap;
    private ID2D1Bitmap? _placeholderBitmap;
    private Image? _logoSource;
    private int _width;
    private int _height;

    public Direct2DRenderSurface(IntPtr hwnd, GdiSize size)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(hwnd));
        }

        _hwnd = hwnd;
        _width = Math.Max(1, size.Width);
        _height = Math.Max(1, size.Height);
        _factory = CreateDXGIFactory1<IDXGIFactory2>();

        try
        {
            (_device, _deviceContext) = CreateDevice();

            var swapChainDescription = new SwapChainDescription1
            {
                Width = (uint)_width,
                Height = (uint)_height,
                Format = DxgiFormat.B8G8R8A8_UNorm,
                BufferCount = 2,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = SampleDescription.Default,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = DxgiAlphaMode.Ignore
            };

            var fullscreenDescription = new SwapChainFullscreenDescription { Windowed = true };
            _swapChain = _factory.CreateSwapChainForHwnd(
                _device,
                _hwnd,
                swapChainDescription,
                fullscreenDescription);
            _factory.MakeWindowAssociation(_hwnd, WindowAssociationFlags.IgnoreAltEnter);

            _d2dFactory = D2D1CreateFactory<ID2D1Factory>(FactoryType.MultiThreaded);
            RecreateRenderTarget();
        }
        catch
        {
            _factory.Dispose();
            throw;
        }
    }

    public void UpdateLogo(Image? image)
    {
        if (ReferenceEquals(_logoSource, image))
        {
            return;
        }

        _logoSource = image;
        _logoBitmap?.Dispose();
        _logoBitmap = image is null ? null : CreateBitmap(image);
    }

    public void Resize(GdiSize size)
    {
        var width = Math.Max(1, size.Width);
        var height = Math.Max(1, size.Height);
        if (width == _width && height == _height)
        {
            return;
        }

        _logoBitmap?.Dispose();
        _logoBitmap = null;
        _placeholderBitmap?.Dispose();
        _placeholderBitmap = null;
        _renderTarget?.Dispose();
        _renderTarget = null;

        _swapChain.ResizeBuffers(2, (uint)width, (uint)height, DxgiFormat.B8G8R8A8_UNorm);
        _width = width;
        _height = height;
        RecreateRenderTarget();
    }

    public void Render(
        LogoState state,
        RectD viewport,
        GdiColor backgroundColor,
        GdiColor flashColor,
        int flashAlpha)
    {
        if (_renderTarget is null)
        {
            return;
        }

        var destination = new RawRectF(
            (float)(state.X - viewport.X),
            (float)(state.Y - viewport.Y),
            (float)(state.X - viewport.X + state.Width),
            (float)(state.Y - viewport.Y + state.Height));

        _renderTarget.BeginDraw();
        var drawCompleted = false;
        try
        {
            _renderTarget.Clear(ToColor4(backgroundColor));

            var bitmap = _logoBitmap ?? _placeholderBitmap;
            if (bitmap is not null)
            {
                _renderTarget.DrawBitmap(
                    bitmap,
                    (RawRectF?)destination,
                    1.0f,
                    BitmapInterpolationMode.Linear,
                    null);
            }

            if (flashAlpha > 0)
            {
                using var flashBrush = _renderTarget.CreateSolidColorBrush(
                    ToColor4(GdiColor.FromArgb(flashAlpha, flashColor)));
                var viewportRectangle = new RawRectF(0, 0, _width, _height);
                _renderTarget.FillRectangle(viewportRectangle, flashBrush);
            }

            var result = _renderTarget.EndDraw();
            drawCompleted = true;
            result.CheckError();
        }
        catch
        {
            if (!drawCompleted)
            {
                // EndDraw must be paired with BeginDraw even when drawing fails.
                try
                {
                    _renderTarget.EndDraw();
                }
                catch
                {
                    // The caller will switch to GDI+ after the original error.
                }
            }

            throw;
        }

        _swapChain.Present(1).CheckError();
    }

    public void Dispose()
    {
        _logoBitmap?.Dispose();
        _placeholderBitmap?.Dispose();
        _renderTarget?.Dispose();
        _d2dFactory.Dispose();
        _swapChain.Dispose();
        _deviceContext.ClearState();
        _deviceContext.Flush();
        _deviceContext.Dispose();
        _device.Dispose();
        _factory.Dispose();
    }

    private void RecreateRenderTarget()
    {
        using var surface = _swapChain.GetBuffer<IDXGISurface>(0);
        var properties = new RenderTargetProperties(
            new D2DPixelFormat(DxgiFormat.B8G8R8A8_UNorm, D2DAlphaMode.Premultiplied));
        _renderTarget = _d2dFactory.CreateDxgiSurfaceRenderTarget(surface, properties);
        using var placeholderImage = CreatePlaceholderImage();
        _placeholderBitmap = CreateBitmap(placeholderImage);

        if (_logoSource is not null)
        {
            _logoBitmap = CreateBitmap(_logoSource);
        }
    }

    private ID2D1Bitmap CreateBitmap(Image image)
    {
        using var bitmap = new Bitmap(image.Width, image.Height, GdiPixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.DrawImageUnscaled(image, 0, 0);
        }

        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, GdiPixelFormat.Format32bppPArgb);
        try
        {
            var properties = new BitmapProperties(
                new D2DPixelFormat(DxgiFormat.B8G8R8A8_UNorm, D2DAlphaMode.Premultiplied));
            return _renderTarget!.CreateBitmap(
                new SizeI(bitmap.Width, bitmap.Height),
                bitmapData.Scan0,
                (uint)Math.Abs(bitmapData.Stride),
                properties);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static Bitmap CreatePlaceholderImage()
    {
        var bitmap = new Bitmap(300, 100, GdiPixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(GdiColor.Transparent);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        using var pen = new Pen(GdiColor.FromArgb(180, GdiColor.White), 2);
        using var brush = new SolidBrush(GdiColor.FromArgb(220, GdiColor.White));
        using var font = new Font(SystemFonts.MessageBoxFont.FontFamily, 24, FontStyle.Bold);
        graphics.DrawRectangle(pen, 1, 1, bitmap.Width - 2, bitmap.Height - 2);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString("LOGO", font, brush, new RectangleF(0, 0, bitmap.Width, bitmap.Height), format);
        return bitmap;
    }

    private static (ID3D11Device Device, ID3D11DeviceContext Context) CreateDevice()
    {
        const DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport;
        var result = D3D11CreateDevice(
            null,
            DriverType.Hardware,
            creationFlags,
            FeatureLevels,
            out ID3D11Device device,
            out D3DFeatureLevel _,
            out ID3D11DeviceContext context);

        if (result.Failure)
        {
            result = D3D11CreateDevice(
                IntPtr.Zero,
                DriverType.Warp,
                creationFlags,
                FeatureLevels,
                out device,
                out _,
                out context);
            result.CheckError();
        }

        return (device, context);
    }

    private static Color4 ToColor4(GdiColor color) => new(
        color.R / 255.0f,
        color.G / 255.0f,
        color.B / 255.0f,
        color.A / 255.0f);
}
