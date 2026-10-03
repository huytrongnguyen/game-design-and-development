namespace Course.MiniEngine;

/// <summary>Where an entity is, in arbitrary world units. 1 tick of Velocity = 1 unit moved (no delta-time).</summary>
public readonly record struct Position(float X, float Y);
