namespace Course.Navigation;

/// <summary>
/// Greedy "pull to the furthest visible point" smoothing of a raw A* grid path — the grid analogue
/// of the funnel/string-pulling step a navmesh path needs. A navmesh's
/// funnel tightens a corridor of *polygons* against shared portal edges; a grid path has no
/// polygon corridor to work with, so this instead walks forward from the current anchor to the
/// farthest waypoint still in a straight, fully walkable line of sight, and repeats.
/// </summary>
public static class StringPuller
{
    public static List<GridCell> Smooth(Grid grid, IReadOnlyList<GridCell> rawPath)
    {
        if (rawPath.Count <= 2)
            return rawPath.ToList();

        var smoothed = new List<GridCell> { rawPath[0] };
        int anchor = 0;

        while (anchor < rawPath.Count - 1)
        {
            int farthest = anchor + 1;
            for (int candidate = rawPath.Count - 1; candidate > anchor + 1; candidate--)
            {
                if (HasLineOfSight(grid, rawPath[anchor], rawPath[candidate]))
                {
                    farthest = candidate;
                    break;
                }
            }

            smoothed.Add(rawPath[farthest]);
            anchor = farthest;
        }

        return smoothed;
    }

    /// <summary>
    /// True if every grid cell a straight line between <paramref name="from"/> and
    /// <paramref name="to"/> passes through (a Bresenham walk) is walkable.
    /// </summary>
    private static bool HasLineOfSight(Grid grid, GridCell from, GridCell to)
    {
        int x = from.X, y = from.Y;
        int x1 = to.X, y1 = to.Y;
        int dx = Math.Abs(x1 - x), dy = Math.Abs(y1 - y);
        int stepX = x1 > x ? 1 : -1;
        int stepY = y1 > y ? 1 : -1;
        int error = dx - dy;

        while (true)
        {
            if (!grid.IsWalkable(new GridCell(x, y)))
                return false;

            if (x == x1 && y == y1)
                return true;

            int error2 = error * 2;
            if (error2 > -dy)
            {
                error -= dy;
                x += stepX;
            }
            if (error2 < dx)
            {
                error += dx;
                y += stepY;
            }
        }
    }
}
