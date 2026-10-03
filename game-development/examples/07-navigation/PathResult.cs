namespace Course.Navigation;

/// <summary>The result of an <see cref="AStarPathfinder"/> or <see cref="PathfindingService"/> query.</summary>
public sealed record PathResult(PathStatus Status, IReadOnlyList<GridCell> Waypoints, int ExpandedNodeCount)
{
    public static PathResult Found(IReadOnlyList<GridCell> waypoints, int expandedNodeCount) =>
        new(PathStatus.Found, waypoints, expandedNodeCount);

    public static PathResult NotFound(int expandedNodeCount) =>
        new(PathStatus.NotFound, Array.Empty<GridCell>(), expandedNodeCount);

    public static PathResult BudgetExceeded(int expandedNodeCount) =>
        new(PathStatus.BudgetExceeded, Array.Empty<GridCell>(), expandedNodeCount);
}
