namespace Course.MiniEngine;

/// <summary>A stable identifier for one entity, assigned in creation order starting at 0.</summary>
public readonly record struct EntityId(int Value)
{
    public override string ToString() => $"Entity#{Value}";
}
