using BouncingScreensaver.Core;

namespace BouncingScreensaver.Windows.UI;

internal class RenderWindow : Form
{
    private readonly SimulationHost _host;
    private readonly RectD _viewport;
    private Direct2DRenderSurface? _direct2D;

    public RenderWindow(SimulationHost host, RectD viewport, bool fullScreen)
    {
        _host = host;
        _viewport = viewport;
        DoubleBuffered = false;
        BackColor = Color.Black;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        ShowInTaskbar = !fullScreen;

        if (fullScreen)
        {
            FormBorderStyle = FormBorderStyle.None;
            Bounds = new Rectangle(
                (int)Math.Round(viewport.X),
                (int)Math.Round(viewport.Y),
                (int)Math.Round(viewport.Width),
                (int)Math.Round(viewport.Height));
            TopMost = true;
        }

        _host.AddView(this);
    }

    internal bool RenderFrame(
        LogoState state,
        Image? logo,
        Color backgroundColor,
        Color flashColor,
        int flashAlpha)
    {
        if (_direct2D is null)
        {
            return false;
        }

        try
        {
            _direct2D.UpdateLogo(logo);
            _direct2D.Render(state, _viewport, backgroundColor, flashColor, flashAlpha);
            return true;
        }
        catch
        {
            DisableDirect2D();
            return false;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        try
        {
            _direct2D = new Direct2DRenderSurface(Handle, ClientSize);
        }
        catch
        {
            _direct2D = null;
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        DisableDirect2D();
        base.OnHandleDestroyed(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_direct2D is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        try
        {
            _direct2D.Resize(ClientSize);
        }
        catch
        {
            DisableDirect2D();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (_direct2D is null)
        {
            base.OnPaintBackground(e);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_direct2D is not null)
        {
            return;
        }

        base.OnPaint(e);
        _host.Paint(e.Graphics, _viewport);
    }

    private void DisableDirect2D()
    {
        _direct2D?.Dispose();
        _direct2D = null;
    }
}
