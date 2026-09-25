using System.Diagnostics;
using System.Drawing.Drawing2D;
using BouncingScreensaver.Core;
using BouncingScreensaver.Windows.Configuration;

namespace BouncingScreensaver.Windows.UI;

internal sealed class SimulationHost : IDisposable
{
    private readonly ScreensaverSettings _settings;
    private readonly BounceSimulation _simulation;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<Control> _views = new();
    private LogoState _state;
    private Image? _logo;
    private long _lastTick;
    private long? _flashStarted;

    public SimulationHost(DesktopTopology topology, RectD initialArea, ScreensaverSettings settings, Image? initialLogo)
    {
        _settings = settings;
        _simulation = new BounceSimulation(topology);
        _logo = initialLogo;
        _state = CreateInitialState(initialArea, settings, initialLogo);
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += OnTick;
        _lastTick = _clock.ElapsedMilliseconds;
    }

    public void AddView(Control view) => _views.Add(view);

    public void Start() => _timer.Start();

    public void SetLogo(Image image)
    {
        var old = _logo;
        _logo = image;

        var newHeight = _state.Width * image.Height / Math.Max(1.0, image.Width);
        var centerY = _state.Y + _state.Height / 2.0;
        _state = _state with
        {
            Height = Math.Max(1, newHeight),
            Y = centerY - newHeight / 2.0
        };

        old?.Dispose();
        InvalidateViews();
    }

    public void Paint(Graphics graphics, RectD viewport)
    {
        graphics.Clear(ParseColor(_settings.BackgroundColor, Color.Black));
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        var destination = new RectangleF(
            (float)(_state.X - viewport.X),
            (float)(_state.Y - viewport.Y),
            (float)_state.Width,
            (float)_state.Height);

        if (_logo is not null)
        {
            graphics.DrawImage(_logo, destination);
        }
        else
        {
            DrawPlaceholder(graphics, destination);
        }

        var alpha = GetFlashAlpha();
        if (alpha > 0)
        {
            var flashColor = ParseColor(_settings.FlashColor, Color.FromArgb(123, 44, 255));
            using var brush = new SolidBrush(Color.FromArgb(alpha, flashColor));
            graphics.FillRectangle(brush, graphics.VisibleClipBounds);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _logo?.Dispose();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = _clock.ElapsedMilliseconds;
        var deltaSeconds = Math.Max(0, now - _lastTick) / 1000.0;
        _lastTick = now;

        var result = _simulation.Step(_state, deltaSeconds, _settings.CornerTolerancePx);
        _state = result.State;

        if (result.CornerHit && _settings.FlashEnabled)
        {
            _flashStarted = now;
        }

        InvalidateViews();
    }

    private void InvalidateViews()
    {
        foreach (var view in _views)
        {
            if (!view.IsDisposed)
            {
                view.Invalidate();
            }
        }
    }

    private int GetFlashAlpha()
    {
        if (_flashStarted is null || !_settings.FlashEnabled)
        {
            return 0;
        }

        var elapsed = _clock.ElapsedMilliseconds - _flashStarted.Value;
        var duration = Math.Max(50, _settings.FlashDurationMs);
        const int holdMs = 80;

        if (elapsed <= holdMs)
        {
            return 255;
        }

        if (elapsed >= duration)
        {
            _flashStarted = null;
            return 0;
        }

        var fadeDuration = Math.Max(1, duration - holdMs);
        var progress = (elapsed - holdMs) / (double)fadeDuration;
        return (int)Math.Clamp(255 * (1.0 - progress), 0, 255);
    }

    private static LogoState CreateInitialState(RectD area, ScreensaverSettings settings, Image? logo)
    {
        var width = Math.Max(80, area.Width * settings.LogoWidthPercent / 100.0);
        var aspect = logo is null ? 3.0 : logo.Width / Math.Max(1.0, logo.Height);
        var height = Math.Max(1, width / aspect);

        var maxX = Math.Max(area.Left, area.Right - width);
        var maxY = Math.Max(area.Top, area.Bottom - height);
        var x = Random.Shared.NextDouble() * Math.Max(0, maxX - area.Left) + area.Left;
        var y = Random.Shared.NextDouble() * Math.Max(0, maxY - area.Top) + area.Top;

        var component = settings.SpeedPxPerSecond / Math.Sqrt(2.0);
        var vx = Random.Shared.Next(2) == 0 ? component : -component;
        var vy = Random.Shared.Next(2) == 0 ? component : -component;

        return new LogoState(x, y, width, height, vx, vy);
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }

    private static void DrawPlaceholder(Graphics graphics, RectangleF destination)
    {
        using var pen = new Pen(Color.FromArgb(180, Color.White), 2);
        using var brush = new SolidBrush(Color.FromArgb(220, Color.White));
        using var font = new Font(SystemFonts.MessageBoxFont.FontFamily, Math.Max(8, destination.Height / 4), FontStyle.Bold);
        graphics.DrawRectangle(pen, destination.X, destination.Y, destination.Width, destination.Height);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString("LOGO", font, brush, destination, format);
    }
}
