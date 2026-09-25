namespace BouncingScreensaver.Core;

public readonly record struct LogoState(
    double X,
    double Y,
    double Width,
    double Height,
    double VelocityX,
    double VelocityY)
{
    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
}

public readonly record struct SimulationStepResult(
    LogoState State,
    bool HitHorizontalEdge,
    bool HitVerticalEdge,
    bool CornerHit);

public sealed class BounceSimulation
{
    private const double EdgeInset = 0.01;
    private readonly DesktopTopology _topology;

    public BounceSimulation(DesktopTopology topology)
    {
        _topology = topology;
    }

    public SimulationStepResult Step(LogoState state, double deltaSeconds, double cornerTolerancePixels)
    {
        var dt = Math.Clamp(deltaSeconds, 0, 0.05);
        if (dt <= 0)
        {
            return new SimulationStepResult(state, false, false, false);
        }

        var originalVelocityX = state.VelocityX;
        var originalVelocityY = state.VelocityY;

        var x = state.X;
        var y = state.Y;
        var velocityX = originalVelocityX;
        var velocityY = originalVelocityY;
        var hitX = false;
        var hitY = false;

        var proposedX = x + velocityX * dt;
        if (CanPlaceHorizontally(state with { X = proposedX, Y = y }, velocityX))
        {
            x = proposedX;
        }
        else
        {
            x = ResolveHorizontalContact(state with { X = x, Y = y }, proposedX, velocityX);
            velocityX = -velocityX;
            hitX = true;
        }

        var proposedY = y + velocityY * dt;
        var xAdjusted = state with { X = x, Y = y };
        if (CanPlaceVertically(xAdjusted with { Y = proposedY }, velocityY))
        {
            y = proposedY;
        }
        else
        {
            y = ResolveVerticalContact(xAdjusted, proposedY, velocityY);
            velocityY = -velocityY;
            hitY = true;
        }

        var next = state with
        {
            X = x,
            Y = y,
            VelocityX = velocityX,
            VelocityY = velocityY
        };

        var cornerHit = false;
        if (hitX || hitY)
        {
            var impactX = originalVelocityX >= 0 ? next.Right : next.Left;
            var impactY = originalVelocityY >= 0 ? next.Bottom : next.Top;
            cornerHit = _topology.IsNearExternalCorner(impactX, impactY, cornerTolerancePixels);
        }

        return new SimulationStepResult(next, hitX, hitY, cornerHit);
    }

    private bool CanPlaceHorizontally(LogoState state, double velocityX)
    {
        var probeX = velocityX >= 0 ? state.Right - EdgeInset : state.Left + EdgeInset;
        return _topology.ContainsPoint(probeX, state.Y + state.Height / 2.0);
    }

    private bool CanPlaceVertically(LogoState state, double velocityY)
    {
        var probeY = velocityY >= 0 ? state.Bottom - EdgeInset : state.Top + EdgeInset;
        return _topology.ContainsPoint(state.X + state.Width / 2.0, probeY);
    }

    private double ResolveHorizontalContact(LogoState state, double proposedX, double velocityX)
    {
        var allowed = state.X;
        var blocked = proposedX;

        for (var i = 0; i < 14; i++)
        {
            var mid = (allowed + blocked) / 2.0;
            if (CanPlaceHorizontally(state with { X = mid }, velocityX))
            {
                allowed = mid;
            }
            else
            {
                blocked = mid;
            }
        }

        return allowed;
    }

    private double ResolveVerticalContact(LogoState state, double proposedY, double velocityY)
    {
        var allowed = state.Y;
        var blocked = proposedY;

        for (var i = 0; i < 14; i++)
        {
            var mid = (allowed + blocked) / 2.0;
            if (CanPlaceVertically(state with { Y = mid }, velocityY))
            {
                allowed = mid;
            }
            else
            {
                blocked = mid;
            }
        }

        return allowed;
    }
}
