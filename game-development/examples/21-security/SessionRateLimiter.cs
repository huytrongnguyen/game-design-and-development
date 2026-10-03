namespace Course.Security;

/// <summary>A token bucket per session: a burst allowance that refills steadily. Messages over
/// budget are dropped (throttled); a session that keeps hammering is disconnected. Time is the tick.</summary>
public sealed class SessionRateLimiter
{
    private sealed class Bucket { public double Tokens; public long LastTick; public int Strikes; }

    private readonly double _capacity;
    private readonly double _refillPerTick;
    private readonly int _maxStrikes;
    private readonly Dictionary<string, Bucket> _buckets = new();

    public SessionRateLimiter(double capacity, double refillPerTick, int maxStrikes)
    {
        _capacity = capacity;
        _refillPerTick = refillPerTick;
        _maxStrikes = maxStrikes;
    }

    public RateVerdict Allow(string sessionId, long tick, double cost = 1)
    {
        if (!_buckets.TryGetValue(sessionId, out var b))
            _buckets[sessionId] = b = new Bucket { Tokens = _capacity, LastTick = tick };

        b.Tokens = Math.Min(_capacity, b.Tokens + (tick - b.LastTick) * _refillPerTick);
        b.LastTick = tick;

        if (b.Tokens >= cost)
        {
            b.Tokens -= cost;
            b.Strikes = 0;
            return RateVerdict.Allowed;
        }
        return ++b.Strikes >= _maxStrikes ? RateVerdict.Disconnect : RateVerdict.Throttled;
    }
}
