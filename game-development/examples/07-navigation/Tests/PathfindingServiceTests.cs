namespace Course.Navigation.Tests;

public class PathfindingServiceTests
{
    // GapMap's gap is 3 cells wide (x = 3..5). After inflation the open centre column's
    // distance-to-nearest-obstacle is 2 cells, so radius classes that inflate by 1 cell
    // (AgentSizes.Small = 10) still fit through, but ones that inflate by 2 cells or more
    // (Medium = 20, Large = 40) do not. With 10 world units per cell, Small/Medium/Large
    // convert to exactly 1/2/4 cells.
    private const int CellSizeUnits = 10;

    [Fact]
    public void FindPath_SmallAgent_FitsThroughTheGap()
    {
        var ground = Grid.FromAscii(TestMaps.GapMap);
        var service = new PathfindingService(ground, CellSizeUnits);

        var result = service.FindPath(new GridCell(4, 0), new GridCell(4, 10), AgentSizes.Small);

        // The gap's centre column (x = 4) stays walkable end to end after a 1-cell inflation,
        // so the smoothed path is the straight vertical line straight through row y = 5's gap.
        Assert.Equal(PathStatus.Found, result.Status);
        Assert.All(result.Waypoints, cell => Assert.Equal(4, cell.X));
    }

    [Fact]
    public void FindPath_LargeAgent_CannotFitThroughTheSameGap()
    {
        var ground = Grid.FromAscii(TestMaps.GapMap);
        var service = new PathfindingService(ground, CellSizeUnits);

        var result = service.FindPath(new GridCell(4, 0), new GridCell(4, 10), AgentSizes.Large);

        Assert.Equal(PathStatus.NotFound, result.Status);
    }

    [Fact]
    public void FindPath_MediumAgent_AlsoCannotFitThroughThisParticularGap()
    {
        // AgentSizes.Medium = 20 -> 2 cells of inflation; the gap's centre column distance
        // to the nearest obstacle is exactly 2, which the inflation rule (<=radius blocked)
        // closes off too. Demonstrates the gap is sized to separate exactly "S fits, M/L don't".
        var ground = Grid.FromAscii(TestMaps.GapMap);
        var service = new PathfindingService(ground, CellSizeUnits);

        var result = service.FindPath(new GridCell(4, 0), new GridCell(4, 10), AgentSizes.Medium);

        Assert.Equal(PathStatus.NotFound, result.Status);
    }

    [Fact]
    public void FindPath_CachesTheInflatedGroundPerAgentRadius()
    {
        var ground = Grid.FromAscii(TestMaps.GapMap);
        var service = new PathfindingService(ground, CellSizeUnits);

        // Calling twice for the same radius must not throw and must return the same result —
        // exercising the per-radius cache in GroundFor without reaching into its internals.
        var first = service.FindPath(new GridCell(4, 0), new GridCell(4, 10), AgentSizes.Small);
        var second = service.FindPath(new GridCell(4, 0), new GridCell(4, 10), AgentSizes.Small);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.Waypoints, second.Waypoints);
    }
}
