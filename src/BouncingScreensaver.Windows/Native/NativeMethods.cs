using System.Runtime.InteropServices;

namespace BouncingScreensaver.Windows.Native;

internal static class NativeMethods
{
    private const int GwlStyle = -16;
    private const long WsChild = 0x40000000L;
    private const long WsPopup = 0x80000000L;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetParent(nint child, nint newParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(nint hwnd, out RECT rect);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr64(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr64(nint hwnd, int index, nint value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);

    internal static (int Width, int Height) GetClientSize(nint hwnd)
    {
        if (!GetClientRect(hwnd, out var rect))
        {
            return (320, 240);
        }
        return (Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top));
    }

    internal static void AttachAsChild(nint child, nint parent)
    {
        var style = GetWindowLongPtr64(child, GwlStyle).ToInt64();
        style |= WsChild;
        style &= ~WsPopup;
        SetWindowLongPtr64(child, GwlStyle, new nint(style));
        SetParent(child, parent);

        var (width, height) = GetClientSize(parent);
        SetWindowPos(child, nint.Zero, 0, 0, width, height, SwpNoZOrder | SwpNoActivate);
    }
}
