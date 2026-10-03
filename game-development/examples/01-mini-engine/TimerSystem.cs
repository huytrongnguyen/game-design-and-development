namespace Course.MiniEngine;

/// <summary>Counts every Timer component down by one tick; at zero, publishes its event and removes the Timer so it never fires twice.</summary>
public sealed class TimerSystem : ISystem
{
    public void Step(EntityStore store, EventQueue events, long tick)
    {
        foreach (var id in store.EntitiesWith<Timer>())
        {
            if (!store.TryGet<Timer>(id, out var timer))
                continue;

            var remaining = timer.TicksRemaining - 1;
            if (remaining <= 0)
            {
                events.Publish(new GameEvent(timer.EventName, tick, id));
                store.Remove<Timer>(id);
            }
            else
            {
                store.Set(id, timer with { TicksRemaining = remaining });
            }
        }
    }
}
