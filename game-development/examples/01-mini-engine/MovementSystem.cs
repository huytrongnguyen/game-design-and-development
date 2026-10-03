namespace Course.MiniEngine;

/// <summary>Adds each entity's Velocity to its Position, once per tick. 1 tick = 1 unit of time, so Velocity is "units moved per tick".</summary>
public sealed class MovementSystem : ISystem
{
    public void Step(EntityStore store, EventQueue events, long tick)
    {
        foreach (var id in store.EntitiesWith<Velocity>())
        {
            if (!store.TryGet<Velocity>(id, out var velocity) || !store.TryGet<Position>(id, out var position))
                continue;

            store.Set(id, new Position(position.X + velocity.Dx, position.Y + velocity.Dy));
        }
    }
}
