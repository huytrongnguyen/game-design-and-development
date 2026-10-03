namespace Course.Economy;

/// <summary>
/// Hands out unique ids for item instances. In production this is a database sequence;
/// the id must never be reused, or two items could be confused.
/// </summary>
public sealed class InstanceIdGenerator(long start = 1)
{
    private long _next = start;

    public long Next() => _next++;
}
