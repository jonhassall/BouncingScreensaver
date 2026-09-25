using BouncingScreensaver.Core;

namespace BouncingScreensaver.Windows.UI;

internal class RenderWindow : Form
{
    private readonly SimulationHost _host;
    private readonly RectD _viewport;

    public RenderWindow(SimulationHost host, RectD viewport, bool fullScreen)
    {
        _host = host;
        _viewport = viewport;
        DoubleBuffered = true;
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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _host.Paint(e.Graphics, _viewport);
    }
}
