namespace Course.MiniEngine;

/// <summary>Something a system announced during a tick, for other systems (or, in a real engine, the network layer) to react to.</summary>
public readonly record struct GameEvent(string Name, long Tick, EntityId Source);
