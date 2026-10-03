namespace Course.Navigation;

/// <summary>
/// The whole server-side query pipeline this module's PoC needs to prove viable: pick (or build and
/// cache) the ground grid already inflated for the requesting agent's radius , run budgeted
/// A* over it, then smooth the result. One shared ground, several agent sizes, a bounded
/// per-query search effort.
///
/// Caching one inflated grid per radius is closer in spirit to the "one baked mesh per agent type" model of
/// the major engines than to a single shared mesh with a per-shape expansion layer; a production version would share more of the inflation work across
/// radii (e.g. one distance field, thresholded per radius) instead of rebuilding it from scratch.
/// </summary>
public sealed class PathfindingService
{
    private readonly Grid _ground;
    private readonly int _cellSizeUnits;
    private readonly Dictionary<int, Grid> _groundByAgentRadius = new();

    public PathfindingService(Grid ground, int cellSizeUnits)
    {
        _ground = ground;
        _cellSizeUnits = cellSizeUnits;
    }

    public PathResult FindPath(GridCell start, GridCell goal, int agentRadiusUnits, int? maxExpandedNodes = null)
    {
        var grid = GroundFor(agentRadiusUnits);
        var raw = AStarPathfinder.FindPath(grid, start, goal, maxExpandedNodes);
        if (raw.Status != PathStatus.Found)
            return raw;

        var smoothed = StringPuller.Smooth(grid, raw.Waypoints);
        return PathResult.Found(smoothed, raw.ExpandedNodeCount);
    }

    private Grid GroundFor(int agentRadiusUnits)
    {
        if (!_groundByAgentRadius.TryGetValue(agentRadiusUnits, out var grid))
        {
            int radiusCells = AgentSizes.ToCells(agentRadiusUnits, _cellSizeUnits);
            grid = _ground.InflateObstacles(radiusCells);
            _groundByAgentRadius[agentRadiusUnits] = grid;
        }
        return grid;
    }
}
