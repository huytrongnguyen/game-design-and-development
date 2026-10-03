namespace Course.Sync;

/// <summary>
/// Uniform spatial hash for area-of-interest queries. A radius query touches only the few cells that
/// overlap the query box instead of scanning every entity in the zone.
/// </summary>
public sealed class AoiGrid
{
    private readonly float _cellSize;
    private readonly Dictionary<(int, int), List<EntityId>> _cells = new();
    private readonly Dictionary<EntityId, (float X, float Y, (int, int) Cell)> _slots = new();

    public AoiGrid(float cellSize)
    {
        if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
        _cellSize = cellSize;
    }

    public int Count => _slots.Count;

    public void Insert(EntityId id, float x, float y)
    {
        var cell = CellOf(x, y);
        _slots.Add(id, (x, y, cell));
        CellList(cell).Add(id);
    }

    public void Remove(EntityId id)
    {
        if (!_slots.Remove(id, out var slot)) return;
        _cells[slot.Cell].Remove(id);
    }

    /// <summary>Moves an entity; the cell lists are only touched when it crosses a cell border.</summary>
    public void Move(EntityId id, float x, float y)
    {
        var slot = _slots[id];
        var cell = CellOf(x, y);
        if (cell != slot.Cell)
        {
            _cells[slot.Cell].Remove(id);
            CellList(cell).Add(id);
        }
        _slots[id] = (x, y, cell);
    }

    /// <summary>Appends every entity within <paramref name="radius"/> of the point (circle test).</summary>
    public void Query(float x, float y, float radius, List<EntityId> results)
    {
        var (minX, minY) = CellOf(x - radius, y - radius);
        var (maxX, maxY) = CellOf(x + radius, y + radius);
        var r2 = radius * radius;
        for (var cx = minX; cx <= maxX; cx++)
        for (var cy = minY; cy <= maxY; cy++)
        {
            if (!_cells.TryGetValue((cx, cy), out var list)) continue;
            foreach (var id in list)
            {
                var s = _slots[id];
                var dx = s.X - x;
                var dy = s.Y - y;
                if (dx * dx + dy * dy <= r2) results.Add(id);
            }
        }
    }

    private (int, int) CellOf(float x, float y) =>
        ((int)MathF.Floor(x / _cellSize), (int)MathF.Floor(y / _cellSize));

    private List<EntityId> CellList((int, int) cell)
    {
        if (!_cells.TryGetValue(cell, out var list)) _cells[cell] = list = new List<EntityId>();
        return list;
    }
}
