namespace Course.Sync;

/// <summary>
/// A replicated entity. Instead of a boolean dirty flag that must be cleared (and can only serve one
/// reader), every field remembers the tick of its last change. Any observer can then ask "what changed
/// since the last tick I sent you?", so slow observers simply accumulate changes.
/// </summary>
public sealed class Entity
{
    private long _positionTick, _hpTick, _appearanceTick;

    public Entity(EntityId id, float x, float y, int hp, int appearance, long tick)
    {
        Id = id;
        X = x;
        Y = y;
        Hp = hp;
        Appearance = appearance;
        _positionTick = _hpTick = _appearanceTick = tick;
    }

    public EntityId Id { get; }
    public float X { get; private set; }
    public float Y { get; private set; }
    public int Hp { get; private set; }
    public int Appearance { get; private set; }

    /// <summary>Setting an identical value is not a change: it must not mark the field dirty.</summary>
    public void SetPosition(float x, float y, long tick)
    {
        if (X == x && Y == y) return;
        X = x;
        Y = y;
        _positionTick = tick;
    }

    public void SetHp(int hp, long tick)
    {
        if (Hp == hp) return;
        Hp = hp;
        _hpTick = tick;
    }

    public void SetAppearance(int appearance, long tick)
    {
        if (Appearance == appearance) return;
        Appearance = appearance;
        _appearanceTick = tick;
    }

    /// <summary>Fields changed strictly after <paramref name="tick"/>.</summary>
    public FieldMask ChangedSince(long tick)
    {
        var mask = FieldMask.None;
        if (_positionTick > tick) mask |= FieldMask.Position;
        if (_hpTick > tick) mask |= FieldMask.Hp;
        if (_appearanceTick > tick) mask |= FieldMask.Appearance;
        return mask;
    }

    /// <summary>The classic per-tick dirty flag: did anything change during exactly this tick?</summary>
    public bool IsDirtyAt(long tick) =>
        _positionTick == tick || _hpTick == tick || _appearanceTick == tick;
}
