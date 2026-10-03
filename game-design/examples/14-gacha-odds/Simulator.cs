namespace Course.GachaOdds;

/// <summary>SplitMix64: a tiny seeded generator, so a run is identical on every machine.</summary>
public sealed class Rng(ulong seed)
{
    private ulong _state = seed;

    public double NextDouble()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 11) / (double)(1UL << 53);
    }
}

public sealed record SimResult(double Mean, int Median, int P90, int P99, int Max, double ShareOverHardCase);

public static class Simulator
{
    /// <summary>Plays <paramref name="trials"/> players who pull until they own the featured item.</summary>
    public static SimResult Run(Model m, int trials, ulong seed, int worstCaseToCheck = 180)
    {
        var rng = new Rng(seed);
        var pulls = new int[trials];
        for (int i = 0; i < trials; i++) pulls[i] = OnePlayer(m, rng);
        Array.Sort(pulls);
        int over = pulls.Count(x => x > worstCaseToCheck);
        return new SimResult(pulls.Average(x => (double)x), pulls[trials / 2], pulls[(int)(0.9 * (trials - 1))],
                             pulls[(int)(0.99 * (trials - 1))], pulls[^1], over / (double)trials);
    }

    private static int OnePlayer(Model m, Rng rng)
    {
        int total = 0, sinceTop = 0;
        bool guaranteed = false;
        while (true)
        {
            total++; sinceTop++;
            if (rng.NextDouble() >= m.Rate(sinceTop)) continue;
            sinceTop = 0;                                            // a top-rarity result: the pity counter resets
            if (guaranteed || rng.NextDouble() < m.FeaturedChance) return total;
            guaranteed = m.GuaranteeAfterLoss;
        }
    }
}
