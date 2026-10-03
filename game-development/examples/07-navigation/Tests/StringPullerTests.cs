namespace Course.Navigation.Tests;

public class StringPullerTests
{
    [Fact]
    public void Smooth_StraightOpenCorridor_ReducesToTwoWaypoints()
    {
        var grid = Grid.FromAscii(new[]
        {
            "..........",
        });
        var raw = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0)).Waypoints;
        Assert.Equal(10, raw.Count); // the raw grid path visits every cell

        var smoothed = StringPuller.Smooth(grid, raw);

        Assert.Equal(2, smoothed.Count);
        Assert.Equal(new GridCell(0, 0), smoothed[0]);
        Assert.Equal(new GridCell(9, 0), smoothed[1]);
    }

    [Fact]
    public void Smooth_DetourMapPath_ReducesElevenRawWaypointsToThree()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);
        var raw = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0)).Waypoints;
        Assert.Equal(11, raw.Count); // every cell the raw A* path actually stepped through

        var smoothed = StringPuller.Smooth(grid, raw);

        // (0,0) sees all the way to the turn at (4,4) in a straight line, and (4,4) sees all
        // the way to (9,0) — so the 11-cell zig-zag collapses to this single bend.
        Assert.Equal(new[] { new GridCell(0, 0), new GridCell(4, 4), new GridCell(9, 0) }, smoothed);
    }

    [Fact]
    public void Smooth_NeverSkipsOverAWall()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);
        var raw = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0)).Waypoints;

        var smoothed = StringPuller.Smooth(grid, raw);

        for (int i = 0; i < smoothed.Count - 1; i++)
        {
            // A straight line between consecutive smoothed waypoints must stay fully on open
            // ground — in particular it must still pass through the gap row, not the wall.
            if (smoothed[i].X <= 4 && smoothed[i + 1].X >= 4 || smoothed[i].X >= 4 && smoothed[i + 1].X <= 4)
            {
                Assert.True(smoothed[i].Y == 4 || smoothed[i + 1].Y == 4,
                    "a segment crossing the wall column must touch the open gap row");
            }
        }
    }
}
