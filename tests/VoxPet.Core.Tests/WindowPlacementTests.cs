using VoxPet.Core.Models;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class WindowPlacementTests
{
    private static readonly DesktopArea[] Screens = [new(0, 0, 1920, 1040), new(-1600, -200, 1600, 900)];
    [Fact]
    public void NegativeMonitorCoordinatesArePreserved()
    {
        var saved = new WindowPlacement(900, 600, -1300, -100);
        Assert.Equal(saved, saved.Fit(Screens));
    }
    [Fact]
    public void DisconnectedMonitorReturnsWindowToPrimary()
    {
        var saved = new WindowPlacement(900, 600, -1300, -100);
        var fitted = saved.Fit([Screens[0]]);
        Assert.Equal(new WindowPlacement(900, 600, 510, 220), fitted);
    }
    [Fact]
    public void OversizedWindowAndTaskbarSpaceAreClamped()
    {
        Assert.Equal(new WindowPlacement(640, 480, 10, 20, true), new WindowPlacement(1500, 1100, 500, 100, true).Fit([new(10, 20, 640, 480)]));
    }
    [Fact]
    public void PixelPositionAndDipSizeUseSuppliedScale()
    {
        var fitted = new WindowPlacement(1200, 900, 100, 100).Fit([new(0, 0, 1200, 900)], 1.5, 1.5);
        Assert.Equal(new WindowPlacement(800, 600, 0, 0), fitted);
    }
    [Fact]
    public void NonFiniteFieldsAndInvalidScreensRecover()
    {
        var saved = new WindowPlacement(double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.NaN);
        Assert.Equal(new WindowPlacement(1000, 800, 460, 120), saved.Fit([new(0, 0, 0, 0), Screens[0]]));
        Assert.Equal(new WindowPlacement(1000, 800), saved.Fit([]));
    }
    [Fact]
    public void BroadcastLimitsAndNonMaximizedModeAreRetained()
    {
        Assert.Equal(new WindowPlacement(1200, 200, 100, 50), new WindowPlacement(2000, 1, 100, 50, true).Fit(Screens, broadcast: true));
    }
}
