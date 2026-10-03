namespace Course.Economy;

/// <summary>
/// A tiny seeded random generator (SplitMix64). It is written out here, instead of using
/// <see cref="Random"/>, so that the same seed gives the same numbers on every machine and
/// runtime version: a drop that happened can be replayed exactly.
/// </summary>
public sealed class SplitMixRng(ulong seed)
{
    private ulong _state = seed;

    public ulong NextUInt64()
    {
        var z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>An integer in [0, <paramref name="bound"/>). The modulo bias is far below 1e-15 for small bounds.</summary>
    public int NextInt(int bound)
    {
        if (bound < 1) throw new ArgumentOutOfRangeException(nameof(bound));
        return (int)(NextUInt64() % (ulong)bound);
    }
}
