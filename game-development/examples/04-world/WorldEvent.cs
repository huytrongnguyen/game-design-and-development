namespace Course.World;

/// <summary>Something that happened to an entity's zone membership. Systems and the network layer react to these.</summary>
public readonly record struct WorldEvent(WorldEventKind Kind, EntityId Entity, ZoneId Zone);
