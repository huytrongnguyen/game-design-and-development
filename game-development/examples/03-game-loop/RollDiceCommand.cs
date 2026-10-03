namespace Course.GameLoop;

/// <summary>
/// A command whose effect depends on randomness. Because it only ever draws from the
/// <see cref="SeededRng"/> it is given (never <c>Random.Shared</c>), replaying the same
/// seed and the same command log reproduces the same rolls (see <c>ReplayTests</c>).
/// </summary>
public sealed record RollDiceCommand(int Sides) : ICommand
{
    public void Apply(ZoneState state, SeededRng rng, TickTimerWheel timers, long currentTick)
        => state.RollTotal += rng.RollDie(Sides);
}
