namespace Course.Ttk;

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

public sealed record SimResult(double Mean, double P90);

/// <summary>Plays the fight many times with real variance and crits. A swing starts at t = 0 and lands
/// at the end of its attack interval, so simulated times run about half an interval above the formula.</summary>
public static class Simulator
{
    public static SimResult Run(DefenceRule rule, IReadOnlyList<HeroClass> team,
                                double enemyHp, double enemyDef, int trials, ulong seed)
    {
        var rng = new Rng(seed);
        var times = new double[trials];
        for (int i = 0; i < trials; i++) times[i] = OneFight(rule, team, enemyHp, enemyDef, rng);
        Array.Sort(times);
        return new SimResult(times.Average(), times[(int)(0.9 * (trials - 1))]);
    }

    private static double OneFight(DefenceRule rule, IReadOnlyList<HeroClass> team,
                                   double hp, double def, Rng rng)
    {
        var next = team.Select(c => c.AttackInterval).ToArray();   // when each hero's next swing lands
        while (true)
        {
            int who = Array.IndexOf(next, next.Min());
            var c = team[who];
            double hit = c.DamagePerHit * (1 + c.Variance * (2 * rng.NextDouble() - 1));
            if (rng.NextDouble() < c.CritChance) hit *= c.CritMultiplier;
            hp -= Formulas.ApplyDefence(rule, hit, def);
            if (hp <= 0) return next[who];
            next[who] += c.AttackInterval;
        }
    }
}
