namespace Course.Navigation;

/// <summary>
/// A* search over a <see cref="Grid"/> with an optional expanded-node budget. A budget turns "too expensive"
/// into a clean, directly observable failure instead of a stalled tick.
/// </summary>
public static class AStarPathfinder
{
    public static PathResult FindPath(Grid grid, GridCell start, GridCell goal, int? maxExpandedNodes = null)
    {
        if (!grid.IsWalkable(start) || !grid.IsWalkable(goal))
            return PathResult.NotFound(0);

        if (start == goal)
            return PathResult.Found(new[] { start }, 0);

        var open = new PriorityQueue<GridCell, double>();
        var gScore = new Dictionary<GridCell, double> { [start] = 0 };
        var cameFrom = new Dictionary<GridCell, GridCell>();
        var closed = new HashSet<GridCell>();

        open.Enqueue(start, Octile.Heuristic(start, goal));
        int expandedNodeCount = 0;

        while (open.TryDequeue(out var current, out _))
        {
            if (!closed.Add(current))
                continue; // a cheaper route to this node was already finalized

            if (current == goal)
                return PathResult.Found(ReconstructPath(cameFrom, start, goal), expandedNodeCount);

            expandedNodeCount++;
            if (maxExpandedNodes is int budget && expandedNodeCount > budget)
                return PathResult.BudgetExceeded(expandedNodeCount);

            double currentG = gScore[current];
            foreach (var (neighbor, stepCost) in grid.Neighbors(current))
            {
                if (closed.Contains(neighbor))
                    continue;

                double tentativeG = currentG + stepCost;
                if (gScore.TryGetValue(neighbor, out double existingG) && tentativeG >= existingG)
                    continue;

                gScore[neighbor] = tentativeG;
                cameFrom[neighbor] = current;
                open.Enqueue(neighbor, tentativeG + Octile.Heuristic(neighbor, goal));
            }
        }

        return PathResult.NotFound(expandedNodeCount);
    }

    private static List<GridCell> ReconstructPath(
        Dictionary<GridCell, GridCell> cameFrom, GridCell start, GridCell goal)
    {
        var path = new List<GridCell> { goal };
        var current = goal;
        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
