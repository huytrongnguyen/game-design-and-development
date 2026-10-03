namespace Course.World;

/// <summary>
/// A stable handle to one entity: a slot <see cref="Index"/> plus a <see cref="Generation"/>.
/// When a slot is reused the generation goes up, so an old handle to a despawned entity
/// never matches the new occupant.
/// </summary>
public readonly record struct EntityId(int Index, int Generation)
{
    public override string ToString() => $"Entity#{Index}.{Generation}";
}
