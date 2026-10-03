namespace Course.Economy;

/// <summary>
/// Picks one entry with probability weight / total weight. Integer weights keep the
/// result exact and easy to audit: weights 70, 25, 5 mean 70%, 25% and 5%.
/// </summary>
public sealed class WeightedTable<T>
{
    private readonly (T Value, int Weight)[] _entries;
    private readonly int _total;

    public WeightedTable(IEnumerable<(T Value, int Weight)> entries)
    {
        _entries = entries.ToArray();
        if (_entries.Length == 0 || _entries.Any(e => e.Weight < 1))
            throw new ArgumentException("A weighted table needs at least one entry and every weight must be positive.");
        _total = _entries.Sum(e => e.Weight);
    }

    public T Pick(SplitMixRng rng)
    {
        var roll = rng.NextInt(_total);
        foreach (var (value, weight) in _entries)
        {
            if (roll < weight) return value;
            roll -= weight;
        }
        throw new InvalidOperationException("Unreachable: roll is always below the total weight.");
    }
}
