namespace Course.MiniEngine;

/// <summary>One self-contained step of simulation logic, run once per tick, in a fixed order decided by the engine's caller.</summary>
public interface ISystem
{
    void Step(EntityStore store, EventQueue events, long tick);
}
