namespace Course.Sync;

/// <summary>Stable identity of anything that can be replicated.</summary>
public readonly record struct EntityId(int Value) : IComparable<EntityId>
{
    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);
}
