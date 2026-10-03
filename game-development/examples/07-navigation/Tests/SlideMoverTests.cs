namespace Course.Navigation.Tests;

public class SlideMoverTests
{
    [Fact]
    public void SlideAlong_WallFromOriginTo10x10_AttemptedDeltaIntoTheWall_ProjectsAlongIt()
    {
        // Wall from (0,0) to (10,10): direction (1,1). Attempted delta (8,-2) would cross to
        // the blocked side. dot((8,-2),(10,10)) = 80-20 = 60; dot((10,10),(10,10)) = 200;
        // ratio = 60/200 = 0.3; result = (10,10)*0.3 = (3,3) — the along-wall component only.
        var result = SlideMover.SlideAlong(
            wallStart: (0, 0),
            wallEnd: (10, 10),
            attemptedDelta: (8, -2));

        Assert.Equal(3.0, result.Dx, precision: 10);
        Assert.Equal(3.0, result.Dy, precision: 10);
    }

    [Fact]
    public void SlideAlong_DeltaAlreadyParallelToWall_IsUnchanged()
    {
        // A delta already running along the wall direction projects onto itself.
        var result = SlideMover.SlideAlong(
            wallStart: (0, 0),
            wallEnd: (10, 0),
            attemptedDelta: (5, 0));

        Assert.Equal(5.0, result.Dx, precision: 10);
        Assert.Equal(0.0, result.Dy, precision: 10);
    }

    [Fact]
    public void SlideAlong_DeltaPerpendicularToWall_ProjectsToZero()
    {
        // A delta straight into a horizontal wall has no along-wall component at all.
        var result = SlideMover.SlideAlong(
            wallStart: (0, 0),
            wallEnd: (10, 0),
            attemptedDelta: (0, -5));

        Assert.Equal(0.0, result.Dx, precision: 10);
        Assert.Equal(0.0, result.Dy, precision: 10);
    }
}
