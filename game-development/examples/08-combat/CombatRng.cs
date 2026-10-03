namespace Course.Combat;

/// <summary>
/// SplitMix64: a tiny seeded generator whose output is fixed by the algorithm itself, not by the
/// runtime version, so a recorded fight replays identically on any machine. Never share one
/// instance between zones; give each simulation its own.
/// </summary>
public sealed class CombatRng(ulong seed)
{
    private ulong _state = seed;

    /// <summary>Integer in [minInclusive, maxInclusive].</summary>
    public int Next(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxInclusive));

        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        ulong range = (ulong)((long)maxInclusive - minInclusive + 1);
        return (int)(minInclusive + (long)(z % range));
    }
}
