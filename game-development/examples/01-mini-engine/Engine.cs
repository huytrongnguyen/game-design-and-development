namespace Course.MiniEngine;

/// <summary>
/// The fixed-tick loop. One <see cref="Step"/> call = one tick: run every system once, in the order given
/// at construction, then drain and return the events systems published this tick. There is no wall-clock
/// time anywhere in this type — "when" is the tick number, nothing else — so the same system list, the
/// same starting data and the same number of Step() calls always produce the same result.
/// </summary>
public sealed class Engine
{
    private readonly EntityStore _store;
    private readonly EventQueue _events;
    private readonly List<ISystem> _systems;

    public Engine(EntityStore store, EventQueue events, IReadOnlyList<ISystem> systems)
    {
        _store = store;
        _events = events;
        _systems = [.. systems];
    }

    public long Tick { get; private set; }

    public EntityStore Store => _store;

    public IReadOnlyList<GameEvent> Step()
    {
        Tick++;
        _events.BeginTick();

        foreach (var system in _systems)
            system.Step(_store, _events, Tick);

        return _events.DrainTick();
    }
}
