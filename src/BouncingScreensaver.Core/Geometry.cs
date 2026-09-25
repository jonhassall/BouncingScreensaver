namespace BouncingScreensaver.Core;

public readonly record struct PointD(double X, double Y);

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2.0;
    public double CenterY => Y + Height / 2.0;

    public bool Contains(double x, double y) =>
        x >= Left && x < Right && y >= Top && y < Bottom;
}

public enum CornerKind
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public readonly record struct ExternalCorner(PointD Point, CornerKind Kind);
