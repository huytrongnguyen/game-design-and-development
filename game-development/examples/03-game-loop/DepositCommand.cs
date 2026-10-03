namespace Course.GameLoop;

/// <summary>A deterministic command: credits a fixed amount to the zone's balance.</summary>
public sealed record DepositCommand(long Amount) : ICommand
{
    public void Apply(ZoneState state, SeededRng rng, TickTimerWheel timers, long currentTick)
        => state.Balance += Amount;
}
