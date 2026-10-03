namespace Course.Economy;

/// <summary>
/// An experience table: <c>nextXp[i]</c> is what it costs to go from level i+1 to level i+2.
/// Everything else (cumulative totals, the level for a total) is derived from it with integer math,
/// so thresholds are exact.
/// </summary>
public sealed class ExpCurve
{
    private readonly long[] _totalToReach; // _totalToReach[i] = cumulative xp needed to be at level i+1

    public ExpCurve(IReadOnlyList<long> nextXp)
    {
        if (nextXp.Count == 0 || nextXp.Any(x => x < 1))
            throw new ArgumentException("Every level step must cost at least 1 xp.");
        _totalToReach = new long[nextXp.Count + 1];
        for (var i = 0; i < nextXp.Count; i++)
            _totalToReach[i + 1] = _totalToReach[i] + nextXp[i];
    }

    public int MaxLevel => _totalToReach.Length;

    /// <summary>Builds a smooth curve: each step costs <paramref name="ratioPermille"/>/1000 times the previous one.</summary>
    public static ExpCurve Geometric(long firstStep, int ratioPermille, int steps)
    {
        var list = new List<long>();
        var step = firstStep;
        for (var i = 0; i < steps; i++)
        {
            list.Add(step);
            step = step * ratioPermille / 1000;
        }
        return new ExpCurve(list);
    }

    public long TotalXpToReach(int level) => _totalToReach[level - 1];

    public long XpToNext(int level) => level >= MaxLevel ? 0 : _totalToReach[level] - _totalToReach[level - 1];

    /// <summary>The level a character with this much total experience has. Stays at the cap beyond the last threshold.</summary>
    public int LevelFor(long totalXp)
    {
        var level = 1;
        while (level < MaxLevel && totalXp >= _totalToReach[level]) level++;
        return level;
    }
}
