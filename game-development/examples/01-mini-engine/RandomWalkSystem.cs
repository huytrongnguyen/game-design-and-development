namespace Course.MiniEngine;

/// <summary>
/// Moves every <see cref="RandomWalker"/> by one random step per axis (-1, 0 or +1) each tick, using an
/// RNG injected at construction — never <c>Random.Shared</c> — so the same seed always replays the exact
/// same path (module 01, section 5).
/// </summary>
public sealed class RandomWalkSystem : ISystem
{
    private readonly Random _rng;

    public RandomWalkSystem(Random rng) => _rng = rng;

    public void Step(EntityStore store, EventQueue events, long tick)
    {
        foreach (var id in store.EntitiesWith<RandomWalker>())
        {
            if (!store.TryGet<Position>(id, out var position))
                continue;

            var dx = _rng.Next(-1, 2);
            var dy = _rng.Next(-1, 2);
            store.Set(id, new Position(position.X + dx, position.Y + dy));
        }
    }
}
