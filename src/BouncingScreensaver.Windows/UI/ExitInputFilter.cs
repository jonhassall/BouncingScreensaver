using System.Diagnostics;

namespace BouncingScreensaver.Windows.UI;

internal sealed class ExitInputFilter : IMessageFilter
{
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;

    private readonly Action _exit;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Point _startPosition = Cursor.Position;
    private bool _exiting;

    public ExitInputFilter(Action exit)
    {
        _exit = exit;
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (_exiting)
        {
            return false;
        }

        if (m.Msg is WmKeyDown or WmSysKeyDown or WmLButtonDown or WmRButtonDown or WmMButtonDown)
        {
            Exit();
            return true;
        }

        if (m.Msg == WmMouseMove && _clock.ElapsedMilliseconds > 700)
        {
            var current = Cursor.Position;
            if (Math.Abs(current.X - _startPosition.X) > 8 || Math.Abs(current.Y - _startPosition.Y) > 8)
            {
                Exit();
                return true;
            }
        }

        return false;
    }

    private void Exit()
    {
        _exiting = true;
        _exit();
    }
}
