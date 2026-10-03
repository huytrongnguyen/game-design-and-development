namespace Course.ActionCombat;

/// <summary>
/// A fixed-size ring buffer of one entity's position per tick. The server keeps it so it can ask
/// "where was this entity N ticks ago?" when it resolves a hit from a lagging client.
/// </summary>
public sealed class PositionHistory(int capacity)
{
    private readonly Vec2[] _positions = new Vec2[capacity];
    private readonly int[] _ticks = Enumerable.Repeat(-1, capacity).ToArray();

    public void Record(int tick, Vec2 position)
    {
        int i = tick % capacity;
        _positions[i] = position;
        _ticks[i] = tick;
    }

    /// <summary>False when the tick was never recorded or has been overwritten.</summary>
    public bool TryGet(int tick, out Vec2 position)
    {
        int i = tick % capacity;
        position = _positions[i];
        return tick >= 0 && _ticks[i] == tick;
    }
}
