namespace Course.Navigation.Tests;

/// <summary>
/// A micro-benchmark-style test, not a timing benchmark: it asserts that a 256x256 query expands
/// a bounded number of nodes, standing in for "fits comfortably inside a server tick budget"
/// without depending on the test machine's speed.
/// </summary>
public class BenchmarkTests
{
    [Fact]
    public void FindPath_256x256Grid_FindsAPathWithinAnExpandedNodeBudget()
    {
        // With this seed and a 20% obstacle density, the search actually expands 20,313 of the
        // grid's 65,536 cells (about 31%) to find a 340-waypoint path — comfortably inside a
        // 50,000-node budget, which stands in for "fits a server tick".
        var grid = TestMaps.CreateBenchmarkGrid(size: 256, obstacleProbability: 0.2, seed: 20261002);

        var result = AStarPathfinder.FindPath(grid, new GridCell(0, 0), new GridCell(255, 255), maxExpandedNodes: 50_000);

        Assert.Equal(PathStatus.Found, result.Status);
        Assert.Equal(20_313, result.ExpandedNodeCount);
        Assert.Equal(340, result.Waypoints.Count);
        Assert.True(result.ExpandedNodeCount < 50_000,
            $"expected the search to stay within budget, expanded {result.ExpandedNodeCount} nodes");
    }
}
