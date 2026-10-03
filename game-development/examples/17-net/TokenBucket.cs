namespace M17.Net;

/// <summary>
/// Classic token-bucket rate limiter: a burst allowance that refills at a steady rate.
/// The clock is injected so tests do not sleep.
/// </summary>
public sealed class TokenBucket(int capacity, double refillPerSecond, Func<long> nowMilliseconds)
{
    private double _tokens = capacity;
    private long _last = nowMilliseconds();

    public bool TryTake()
    {
        var now = nowMilliseconds();
        _tokens = Math.Min(capacity, _tokens + (now - _last) * refillPerSecond / 1000.0);
        _last = now;
        if (_tokens < 1)
        {
            return false;
        }

        _tokens -= 1;
        return true;
    }
}
