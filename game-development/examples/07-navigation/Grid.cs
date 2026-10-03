namespace Course.Navigation;

/// <summary>
/// A walkability grid: the simplest of the three navigation representations in the lesson
/// (grid / waypoint graph / navmesh). Each cell is walkable or not; cell
/// (0, 0) is the top-left of the parsed ASCII map.
/// </summary>
public sealed class Grid
{
    private readonly bool[,] _walkable;

    public int Width { get; }
    public int Height { get; }

    public Grid(bool[,] walkable)
    {
        _walkable = walkable;
        Width = walkable.GetLength(0);
        Height = walkable.GetLength(1);
    }

    public bool InBounds(GridCell cell) =>
        cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

    public bool IsWalkable(GridCell cell) =>
        InBounds(cell) && _walkable[cell.X, cell.Y];

    /// <summary>
    /// Parses an ASCII map: '#' is an obstacle, anything else (conventionally '.') is walkable.
    /// <paramref name="rows"/>[0] is y = 0; all rows must have the same length.
    /// </summary>
    public static Grid FromAscii(IReadOnlyList<string> rows)
    {
        int height = rows.Count;
        int width = rows[0].Length;
        var walkable = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            string row = rows[y];
            for (int x = 0; x < width; x++)
                walkable[x, y] = row[x] != '#';
        }
        return new Grid(walkable);
    }

    /// <summary>
    /// Approximates agent-radius erosion: every cell within
    /// <paramref name="radiusCells"/> (Chebyshev / king-move distance) of an existing obstacle
    /// becomes blocked too, computed by a multi-source breadth-first search seeded from every
    /// obstacle cell. This is a square approximation of the circular Minkowski-sum expansion both
    /// navmesh erosion and exact obstacle expansion compute precisely — good enough to
    /// teach the idea and to prove a size-class gap test, not a production-quality distance field.
    /// </summary>
    public Grid InflateObstacles(int radiusCells)
    {
        if (radiusCells <= 0)
            return this;

        var distance = new int[Width, Height];
        var queue = new Queue<GridCell>();
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var cell = new GridCell(x, y);
                if (IsWalkable(cell))
                {
                    distance[x, y] = int.MaxValue;
                }
                else
                {
                    distance[x, y] = 0;
                    queue.Enqueue(cell);
                }
            }
        }

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            int cellDistance = distance[cell.X, cell.Y];
            if (cellDistance >= radiusCells)
                continue;

            foreach (var neighbor in EightNeighbors(cell))
            {
                if (!InBounds(neighbor))
                    continue;
                if (distance[neighbor.X, neighbor.Y] <= cellDistance + 1)
                    continue;

                distance[neighbor.X, neighbor.Y] = cellDistance + 1;
                queue.Enqueue(neighbor);
            }
        }

        var inflated = new bool[Width, Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                inflated[x, y] = distance[x, y] > radiusCells;

        return new Grid(inflated);
    }

    /// <summary>
    /// 8-directional neighbours with octile movement cost (1.0 straight, sqrt(2) diagonal),
    /// forbidding diagonal "corner cutting": a diagonal step is only offered if both orthogonal
    /// cells beside it are also walkable, so an agent can never clip through a wall corner.
    /// </summary>
    public IEnumerable<(GridCell Cell, double Cost)> Neighbors(GridCell cell)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var next = new GridCell(cell.X + dx, cell.Y + dy);
                if (!IsWalkable(next))
                    continue;

                bool diagonal = dx != 0 && dy != 0;
                if (diagonal)
                {
                    var sideA = new GridCell(cell.X + dx, cell.Y);
                    var sideB = new GridCell(cell.X, cell.Y + dy);
                    if (!IsWalkable(sideA) || !IsWalkable(sideB))
                        continue;
                }

                yield return (next, diagonal ? Octile.DiagonalCost : 1.0);
            }
        }
    }

    private static IEnumerable<GridCell> EightNeighbors(GridCell cell)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0)
                    yield return new GridCell(cell.X + dx, cell.Y + dy);
    }
}
