namespace Course.MiniEngine;

/// <summary>
/// Holds the events published during the current tick only. The engine clears it at the start of every
/// tick and drains (reads + clears) it at the end, so events never carry over and never arrive "early"
/// from a future tick — a system can only see events published earlier in the *same* tick's system order.
/// </summary>
public sealed class EventQueue
{
    private readonly List<GameEvent> _tickEvents = [];

    public void BeginTick() => _tickEvents.Clear();

    public void Publish(GameEvent gameEvent) => _tickEvents.Add(gameEvent);

    public List<GameEvent> DrainTick() => new(_tickEvents);
}
