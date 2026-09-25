using BouncingScreensaver.Core;
using BouncingScreensaver.Windows.Native;

namespace BouncingScreensaver.Windows.UI;

internal sealed class PreviewWindow : RenderWindow
{
    private readonly nint _parentHandle;

    public PreviewWindow(SimulationHost host, RectD viewport, nint parentHandle)
        : base(host, viewport, fullScreen: false)
    {
        _parentHandle = parentHandle;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        ControlBox = false;
        Text = string.Empty;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        NativeMethods.AttachAsChild(Handle, _parentHandle);
    }
}
