namespace Course.Balance;

/// <summary>The spreadsheet formulas. A Path is optional: null means the base class kit.</summary>
public static class Model
{
    /// <summary>Expected damage per second against one benchmark fight. Module 06's formula
    /// (crit multiplies the raw hit, then ratio defence K / (K + defence)), times the Path's damage multiplier,
    /// times an area bonus: share x Path area multiplier x (extra targets the class can reach).</summary>
    public static double Dps(BalanceData d, HeroClass c, Fight f, PathMod? p = null)
    {
        double reduction = d.DefenceConstant / (d.DefenceConstant + f.EnemyDefence);
        double perSecond = c.DamagePerHit * (1 - c.CritChance + c.CritChance * c.CritMultiplier) * reduction / c.AttackInterval;
        double extraTargets = Math.Clamp(f.Targets, 1, c.MaxTargets) - 1;
        return perSecond * (p?.DamageMult ?? 1) * (1 + c.AreaShare * (p?.AreaMult ?? 1) * extraTargets);
    }

    /// <summary>Effective HP: health divided by the share of damage that gets through defence and the kit's mitigation.
    /// With ratio defence the defence stat multiplies health by (K + defence) / K.</summary>
    public static double Ehp(BalanceData d, HeroClass c, PathMod? p = null) =>
        c.Hp * (p?.EhpMult ?? 1) * (d.DefenceConstant + c.Defence) / d.DefenceConstant / (1 - c.Mitigation);

    public static double Utility(HeroClass c, PathMod? p = null) => c.Utility + (p?.UtilityDelta ?? 0);

    /// <summary>The roster mean of each component in one fight: the 1.0 every class is measured against.</summary>
    public static (double Dps, double Ehp, double Utility) Means(BalanceData d, Fight f) =>
        (d.Classes.Average(c => Dps(d, c, f)), d.Classes.Average(c => Ehp(d, c)), d.Classes.Average(c => Utility(c)));

    /// <summary>Normalised score in one fight: the weighted sum of (value / roster mean). 1.0 = exactly average.</summary>
    public static double Score(BalanceData d, HeroClass c, Fight f, PathMod? p = null)
    {
        var m = Means(d, f);
        var w = f.Weights;
        return w.Dps * Dps(d, c, f, p) / m.Dps + w.Survivability * Ehp(d, c, p) / m.Ehp + w.Utility * Utility(c, p) / m.Utility;
    }
}
