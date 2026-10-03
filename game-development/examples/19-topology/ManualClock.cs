namespace Course.Topology;

/// <summary>A clock that only moves when told to, so expiry is testable without waiting.</summary>
public sealed class ManualClock : IClock
{
    public long NowSeconds { get; private set; }

    public ManualClock(long startSeconds = 0) => NowSeconds = startSeconds;

    public void Advance(long seconds) => NowSeconds += seconds;
}
