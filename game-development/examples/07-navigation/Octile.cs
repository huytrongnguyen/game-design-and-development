namespace Course.Navigation;

/// <summary>
/// The octile-distance heuristic for 8-directional grid movement with diagonal cost sqrt(2).
/// It never overestimates the true remaining cost on this grid, so
/// it is admissible and A* over the grid still finds the optimal path.
/// </summary>
internal static class Octile
{
    public const double DiagonalCost = 1.4142135623730951; // sqrt(2)

    public static double Heuristic(GridCell a, GridCell b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);
        int straightSteps = Math.Abs(dx - dy);
        int diagonalSteps = Math.Min(dx, dy);
        return straightSteps + diagonalSteps * DiagonalCost;
    }
}
