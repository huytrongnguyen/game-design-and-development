namespace Course.GachaOdds;

/// <summary>Distribution of the number of pulls needed, with summary figures.</summary>
public sealed record Distribution(double[] Pmf)
{
    public double Mean => Enumerable.Range(1, Pmf.Length - 1).Sum(n => n * Pmf[n]);
    public double Mass => Pmf.Sum();

    /// <summary>Smallest pull count n such that at least <paramref name="p"/> of players are done by n pulls.</summary>
    public int Percentile(double p)
    {
        double cum = 0;
        for (int n = 1; n < Pmf.Length; n++)
        {
            cum += Pmf[n];
            if (cum >= p) return n;
        }
        return -1;   // beyond the horizon
    }

    /// <summary>Chance that a player needs more than <paramref name="n"/> pulls.</summary>
    public double ProbMoreThan(int n) => 1 - Enumerable.Range(1, Math.Min(n, Pmf.Length - 1)).Sum(k => Pmf[k]);

    /// <summary>Largest pull count with a non-zero chance (the worst case), or -1 if it runs past the horizon.</summary>
    public int WorstCase
    {
        get
        {
            for (int n = Pmf.Length - 1; n >= 1; n--) if (Pmf[n] > 1e-15) return n < Pmf.Length - 1 ? n : -1;
            return -1;
        }
    }
}

/// <summary>Exact distributions by dynamic programming, no randomness.</summary>
public static class Odds
{
    /// <summary>Pulls until the next top-rarity result: P(first success at pull n) = h(n) x product of (1 - h(k)) for k below n.</summary>
    public static Distribution TopResult(Model m, int horizon)
    {
        var pmf = new double[horizon + 1];
        double alive = 1;
        for (int n = 1; n <= horizon && alive > 0; n++)
        {
            double h = m.Rate(n);
            pmf[n] = alive * h;
            alive *= 1 - h;
        }
        return new Distribution(pmf);
    }

    /// <summary>Pulls until the featured item. With a guarantee: at most two top-rarity results. Without: a geometric number of them.</summary>
    public static Distribution Featured(Model m, int horizon)
    {
        var p = TopResult(m, horizon).Pmf;
        double q = m.FeaturedChance;
        var f = new double[horizon + 1];
        for (int n = 1; n <= horizon; n++)
        {
            double firstWin = q * p[n];
            double afterMiss = 0;
            for (int a = 1; a < n; a++)
            {
                // After a miss: guaranteed win next time, or another independent 50/50 (renewal) when there is no guarantee.
                double next = m.GuaranteeAfterLoss ? p[n - a] : f[n - a];
                afterMiss += p[a] * next;
            }
            f[n] = firstWin + (1 - q) * afterMiss;
        }
        return new Distribution(f);
    }

    /// <summary>Dollars for a given number of pulls.</summary>
    public static double Spend(GachaData d, double pulls) => pulls * d.UsdPerPull;
}
