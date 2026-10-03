namespace Course.World;

/// <summary>
/// One map copy with a uniform grid for spatial queries. The world is cut into square cells; each
/// entity sits in exactly one cell, and a radius query only visits the cells the circle's bounding box touches.
/// Positions are read from the <see cref="EntityStore"/>, which stays the single source of truth.
/// </summary>
public sealed class Zone
{
    private readonly EntityStore _store;
    private readonly List<EntityId>[] _cells;
    private readonly Dictionary<EntityId, int> _cellOf = [];

    public Zone(ZoneId id, EntityStore store, float width, float height, float cellSize)
    {
        Id = id;
        _store = store;
        Width = width;
        Height = height;
        CellSize = cellSize;
        Columns = (int)MathF.Ceiling(width / cellSize);
        Rows = (int)MathF.Ceiling(height / cellSize);
        _cells = new List<EntityId>[Columns * Rows];
        for (var i = 0; i < _cells.Length; i++)
            _cells[i] = [];
    }

    public ZoneId Id { get; }
    public float Width { get; }
    public float Height { get; }
    public float CellSize { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int Count => _cellOf.Count;

    public bool Contains(EntityId id) => _cellOf.ContainsKey(id);

    public bool InBounds(Position p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

    /// <summary>Puts an already-positioned entity into the grid. Fails if it is already here, stale, or outside the zone.</summary>
    public bool Add(EntityId id)
    {
        if (_cellOf.ContainsKey(id) || !_store.TryGet(id, out Position p) || !InBounds(p))
            return false;

        var cell = CellIndex(p);
        _cells[cell].Add(id);
        _cellOf[id] = cell;
        return true;
    }

    public bool Remove(EntityId id)
    {
        if (!_cellOf.Remove(id, out var cell))
            return false;
        _cells[cell].Remove(id);
        return true;
    }

    /// <summary>
    /// Call after the entity's <see cref="Position"/> changed. Re-files it only when it crossed into a different cell.
    /// Returns true if the cell changed.
    /// </summary>
    public bool Update(EntityId id)
    {
        if (!_cellOf.TryGetValue(id, out var oldCell) || !_store.TryGet(id, out Position p))
            return false;

        var newCell = CellIndex(p);
        if (newCell == oldCell)
            return false;

        _cells[oldCell].Remove(id);
        _cells[newCell].Add(id);
        _cellOf[id] = newCell;
        return true;
    }

    /// <summary>The (column, row) of the cell holding the entity, or null if it is not in this zone.</summary>
    public (int Column, int Row)? CellOf(EntityId id) =>
        _cellOf.TryGetValue(id, out var cell) ? (cell % Columns, cell / Columns) : null;

    public int CountInCell(int column, int row) =>
        column < 0 || row < 0 || column >= Columns || row >= Rows ? 0 : _cells[row * Columns + column].Count;

    /// <summary>
    /// Fills <paramref name="results"/> with every entity within <paramref name="radius"/> of
    /// <paramref name="center"/> (distance exactly equal to the radius counts), sorted by slot index so the answer is deterministic.
    /// The caller owns the list so a hot loop can reuse it.
    /// </summary>
    public void Query(Position center, float radius, List<EntityId> results)
    {
        results.Clear();
        var minC = Math.Max(0, (int)MathF.Floor((center.X - radius) / CellSize));
        var maxC = Math.Min(Columns - 1, (int)MathF.Floor((center.X + radius) / CellSize));
        var minR = Math.Max(0, (int)MathF.Floor((center.Y - radius) / CellSize));
        var maxR = Math.Min(Rows - 1, (int)MathF.Floor((center.Y + radius) / CellSize));
        var r2 = radius * radius;

        for (var row = minR; row <= maxR; row++)
        {
            for (var col = minC; col <= maxC; col++)
            {
                foreach (var id in _cells[row * Columns + col])
                {
                    if (!_store.TryGet(id, out Position p))
                        continue;
                    var dx = p.X - center.X;
                    var dy = p.Y - center.Y;
                    if (dx * dx + dy * dy <= r2)
                        results.Add(id);
                }
            }
        }

        results.Sort((a, b) => a.Index.CompareTo(b.Index));
    }

    private int CellIndex(Position p) => (int)(p.Y / CellSize) * Columns + (int)(p.X / CellSize);
}
