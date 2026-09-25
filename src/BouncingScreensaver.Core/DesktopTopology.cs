namespace BouncingScreensaver.Core;

public sealed class DesktopTopology
{
    private const double ProbeOffset = 0.5;
    private readonly RectD[] _monitors;
    private readonly ExternalCorner[] _externalCorners;

    public DesktopTopology(IEnumerable<RectD> monitors)
    {
        _monitors = monitors.Where(m => m.Width > 0 && m.Height > 0).ToArray();
        if (_monitors.Length == 0)
        {
            throw new ArgumentException("At least one monitor rectangle is required.", nameof(monitors));
        }

        _externalCorners = BuildExternalCorners();
    }

    public IReadOnlyList<RectD> Monitors => _monitors;
    public IReadOnlyList<ExternalCorner> ExternalCorners => _externalCorners;

    public bool ContainsPoint(double x, double y) => _monitors.Any(m => m.Contains(x, y));

    public bool IsNearExternalCorner(double x, double y, double tolerancePixels)
    {
        var tolerance = Math.Max(0, tolerancePixels);
        return _externalCorners.Any(c =>
            Math.Abs(c.Point.X - x) <= tolerance &&
            Math.Abs(c.Point.Y - y) <= tolerance);
    }

    private ExternalCorner[] BuildExternalCorners()
    {
        var corners = new List<ExternalCorner>();

        foreach (var monitor in _monitors)
        {
            TryAddCorner(corners, monitor.Left, monitor.Top, CornerKind.TopLeft,
                -ProbeOffset, +ProbeOffset, +ProbeOffset, -ProbeOffset);
            TryAddCorner(corners, monitor.Right, monitor.Top, CornerKind.TopRight,
                +ProbeOffset, +ProbeOffset, -ProbeOffset, -ProbeOffset);
            TryAddCorner(corners, monitor.Left, monitor.Bottom, CornerKind.BottomLeft,
                -ProbeOffset, -ProbeOffset, +ProbeOffset, +ProbeOffset);
            TryAddCorner(corners, monitor.Right, monitor.Bottom, CornerKind.BottomRight,
                +ProbeOffset, -ProbeOffset, -ProbeOffset, +ProbeOffset);
        }

        return corners
            .DistinctBy(c => (Math.Round(c.Point.X, 3), Math.Round(c.Point.Y, 3), c.Kind))
            .ToArray();
    }

    private void TryAddCorner(
        ICollection<ExternalCorner> corners,
        double x,
        double y,
        CornerKind kind,
        double outwardX,
        double inwardY,
        double inwardX,
        double outwardY)
    {
        var horizontalOutside = !ContainsPoint(x + outwardX, y + inwardY);
        var verticalOutside = !ContainsPoint(x + inwardX, y + outwardY);

        if (horizontalOutside && verticalOutside)
        {
            corners.Add(new ExternalCorner(new PointD(x, y), kind));
        }
    }
}
