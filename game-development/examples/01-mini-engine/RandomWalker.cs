namespace Course.MiniEngine;

/// <summary>
/// Marker component (no data): entities with this move by one seeded-random step per axis each tick.
/// See <see cref="RandomWalkSystem"/>.
/// </summary>
public readonly record struct RandomWalker;
