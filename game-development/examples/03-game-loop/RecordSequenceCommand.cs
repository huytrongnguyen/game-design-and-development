namespace Course.GameLoop;

/// <summary>
/// A diagnostic command that just appends a number to <see cref="ZoneState.AppliedOrder"/>.
/// Used to prove ordering: pair it with <see cref="CommandQueue.Submit"/>'s sequence-stamping
/// overload to record the exact order commands were applied in, regardless of which producer
/// thread submitted which command and in what real-time order (see <c>CommandQueueTests</c>).
/// </summary>
public sealed record RecordSequenceCommand(long Sequence) : ICommand
{
    public void Apply(ZoneState state, SeededRng rng, TickTimerWheel timers, long currentTick)
        => state.AppliedOrder.Add(Sequence);
}
