namespace Course.MiniEngine;

/// <summary>How far an entity moves per tick, read by <see cref="MovementSystem"/>.</summary>
public readonly record struct Velocity(float Dx, float Dy);
