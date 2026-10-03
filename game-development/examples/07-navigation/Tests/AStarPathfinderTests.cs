namespace Course.Navigation.Tests;

public class AStarPathfinderTests
{
    [Fact]
    public void FindPath_DetourMap_FindsPathAroundTheWallThroughTheGapRow()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);

        var result = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0));

        Assert.Equal(PathStatus.Found, result.Status);
        Assert.Equal(new GridCell(0, 0), result.Waypoints[0]);
        Assert.Equal(new GridCell(9, 0), result.Waypoints[^1]);
        // Every waypoint must sit on an actually-walkable cell (the detour never cuts through
        // the wall column except at the open row y = 4).
        Assert.All(result.Waypoints, cell => Assert.True(grid.IsWalkable(cell)));
        Assert.Contains(result.Waypoints, cell => cell.Y == 4 && cell.X == 4);
    }

    [Fact]
    public void FindPath_StartEqualsGoal_ReturnsSingleWaypointAndNoExpansion()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);

        var result = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(0, 0));

        Assert.Equal(PathStatus.Found, result.Status);
        Assert.Equal(new[] { new GridCell(0, 0) }, result.Waypoints);
        Assert.Equal(0, result.ExpandedNodeCount);
    }

    [Fact]
    public void FindPath_GoalInsideAnObstacle_ReturnsNotFound()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);

        var result = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(4, 0));

        Assert.Equal(PathStatus.NotFound, result.Status);
        Assert.Empty(result.Waypoints);
    }

    [Fact]
    public void FindPath_BudgetSmallerThanNeeded_ReturnsBudgetExceededNotNotFound()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);

        // The detour needs far more than a handful of expansions; a budget of 3 is nowhere
        // near enough, but a path does exist, so the query must report BudgetExceeded, not
        // NotFound. The search aborts right after its 4th node expansion exceeds the budget.
        var result = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0), maxExpandedNodes: 3);

        Assert.Equal(PathStatus.BudgetExceeded, result.Status);
        Assert.Equal(4, result.ExpandedNodeCount);
        Assert.Empty(result.Waypoints);
    }

    [Fact]
    public void FindPath_DetourMap_PathLengthMatchesTheExpectedDetourCost()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);

        var result = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0));

        // The raw path takes 10 steps: 7 diagonal (cost sqrt(2) each) and 3 straight (cost 1
        // each) down to the gap row and back up — 7*sqrt(2) + 3 = 12.899... An 8-directional
        // straight-line (Chebyshev) detour of the same shape without the no-corner-cutting
        // rule would be shorter; this is the cost *with* that rule enforced (Grid.Neighbors).
        double length = PathLength(result.Waypoints);

        Assert.Equal(11, result.Waypoints.Count);
        Assert.Equal(21, result.ExpandedNodeCount);
        Assert.Equal(7 * Math.Sqrt(2) + 3, length, precision: 10);
        Assert.Equal(12.8994952, length, precision: 6);
    }

    private static double PathLength(IReadOnlyList<GridCell> waypoints)
    {
        double total = 0;
        for (int i = 1; i < waypoints.Count; i++)
        {
            double dx = waypoints[i].X - waypoints[i - 1].X;
            double dy = waypoints[i].Y - waypoints[i - 1].Y;
            total += Math.Sqrt(dx * dx + dy * dy);
        }
        return total;
    }

    [Fact]
    public void FindPath_BudgetGenerous_StillFindsTheSamePathAsUnbudgeted()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);

        var unbudgeted = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0));
        var budgeted = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(9, 0), maxExpandedNodes: 1000);

        Assert.Equal(PathStatus.Found, budgeted.Status);
        Assert.Equal(unbudgeted.Waypoints, budgeted.Waypoints);
    }
}
