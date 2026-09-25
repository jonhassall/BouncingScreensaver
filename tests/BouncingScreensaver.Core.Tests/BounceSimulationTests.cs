using BouncingScreensaver.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BouncingScreensaver.Core.Tests;

[TestClass]
public sealed class BounceSimulationTests
{
    [TestMethod]
    public void LogoCrossesSharedMonitorSeam()
    {
        var topology = new DesktopTopology(new[]
        {
            new RectD(0, 0, 1920, 1080),
            new RectD(1920, 0, 1920, 1080)
        });
        var simulation = new BounceSimulation(topology);
        var state = new LogoState(1810, 400, 200, 100, 300, 0);

        var result = simulation.Step(state, 0.05, 12);

        Assert.IsFalse(result.HitHorizontalEdge);
        Assert.IsTrue(result.State.X > state.X);
    }

    [TestMethod]
    public void LogoBouncesAtOuterDesktopEdge()
    {
        var topology = new DesktopTopology(new[] { new RectD(0, 0, 1920, 1080) });
        var simulation = new BounceSimulation(topology);
        var state = new LogoState(1715, 400, 200, 100, 300, 0);

        var result = simulation.Step(state, 0.05, 12);

        Assert.IsTrue(result.HitHorizontalEdge);
        Assert.IsTrue(result.State.VelocityX < 0);
        Assert.IsTrue(result.State.Right <= 1920.01);
    }

    [TestMethod]
    public void NearCornerCollisionTriggersWithinTolerance()
    {
        var topology = new DesktopTopology(new[] { new RectD(0, 0, 1920, 1080) });
        var simulation = new BounceSimulation(topology);
        var state = new LogoState(8, 7, 200, 100, -240, -240);

        var result = simulation.Step(state, 0.05, 12);

        Assert.IsTrue(result.CornerHit);
    }

    [TestMethod]
    public void NearCornerCollisionDoesNotTriggerOutsideTolerance()
    {
        var topology = new DesktopTopology(new[] { new RectD(0, 0, 1920, 1080) });
        var simulation = new BounceSimulation(topology);
        var state = new LogoState(40, 7, 200, 100, -240, -240);

        var result = simulation.Step(state, 0.05, 12);

        Assert.IsFalse(result.CornerHit);
    }

    [TestMethod]
    public void SharedSeamCornerIsNotExternalCorner()
    {
        var topology = new DesktopTopology(new[]
        {
            new RectD(0, 0, 1920, 1080),
            new RectD(1920, 0, 1920, 1080)
        });

        Assert.IsFalse(topology.IsNearExternalCorner(1920, 0, 1));
        Assert.IsTrue(topology.IsNearExternalCorner(0, 0, 1));
        Assert.IsTrue(topology.IsNearExternalCorner(3840, 1080, 1));
    }

    [TestMethod]
    public void NegativeMonitorCoordinatesAreSupported()
    {
        var topology = new DesktopTopology(new[]
        {
            new RectD(-1920, 0, 1920, 1080),
            new RectD(0, 0, 1920, 1080)
        });

        Assert.IsTrue(topology.ContainsPoint(-100, 500));
        Assert.IsTrue(topology.ContainsPoint(100, 500));
    }
}
