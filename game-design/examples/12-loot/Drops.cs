namespace Course.Loot;

/// <summary>Runs needed to see one specific piece from a dungeon chest, with and without bad-luck protection.</summary>
public static class Drops
{
    public static double RarityChance(LootData d, string source, string rarity)
    {
        var w = d.Source(source).Weights;
        return w[rarity] / w.Values.Sum();
    }

    /// <summary>Chance that one chest roll is the target: the rarity, then one of the pieces of that rarity.</summary>
    public static double ChancePerRoll(LootData d) =>
        d.Source(d.Dungeon.Source).DropChance * RarityChance(d, d.Dungeon.Source, d.Dungeon.TargetRarity) / d.Dungeon.Pieces;

    /// <summary>Chance that at least one of the run's rolls is the target (before any protection).</summary>
    public static double ChancePerRun(LootData d) => 1 - Math.Pow(1 - ChancePerRoll(d), d.Dungeon.RollsPerRun);

    /// <summary>Chance on the nth run since the last target. Runs before the soft start use the base chance.</summary>
    public static double ChanceOnRun(LootData d, int n, bool protectedRuns)
    {
        double p = ChancePerRun(d);
        if (!protectedRuns) return p;
        var pity = d.Dungeon.Pity;
        if (n >= pity.HardAtRuns) return 1.0;
        return Math.Min(1.0, p + pity.SoftBonusPerRun * Math.Max(0, n - pity.SoftStartRuns));
    }

    /// <summary>Exact result: the survival chance (no target yet after n runs) is multiplied run by run.
    /// Without protection this is the geometric distribution: mean 1/p, quantile ln(1-q)/ln(1-p).</summary>
    public static Spread Exact(LootData d, bool protectedRuns)
    {
        double survive = 1, mean = 0;
        int n = 0, p50 = 0, p90 = 0, p99 = 0;
        while (survive > 1e-12)
        {
            n++;
            mean += survive;                                   // P(more than n-1 runs are needed)
            survive *= 1 - ChanceOnRun(d, n, protectedRuns);
            if (p50 == 0 && 1 - survive >= 0.50) p50 = n;
            if (p90 == 0 && 1 - survive >= 0.90) p90 = n;
            if (p99 == 0 && 1 - survive >= 0.99) p99 = n;
        }
        return new Spread(mean, p50, p90, p99);
    }

    public static int RunsToTarget(LootData d, bool protectedRuns, Rng rng)
    {
        for (int n = 1; ; n++)
            if (rng.NextDouble() < ChanceOnRun(d, n, protectedRuns)) return n;
    }

    public static Spread Simulate(LootData d, bool protectedRuns, int trials, ulong seed)
    {
        var rng = new Rng(seed);
        return Spread.Of(Enumerable.Range(0, trials).Select(_ => (double)RunsToTarget(d, protectedRuns, rng)).ToArray());
    }
}
