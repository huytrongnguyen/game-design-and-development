namespace Course.Curves;

/// <summary>The formulas of module 14: XP curve, income, hours per level, rested XP and the power ratio.</summary>
public static class Curves
{
    /// <summary>XP to go from level L to L+1.</summary>
    public static long XpToNext(CurveData d, int level) => (long)Math.Round(d.Xp.Kind switch
    {
        "linear" => d.Xp.Base + d.Xp.Slope * level,
        "polynomial" => d.Xp.Base * Math.Pow(level, d.Xp.Exponent),
        "exponential" => d.Xp.Base * Math.Pow(d.Xp.Growth, level - 1),
        _ => throw new ArgumentException($"Unknown curve kind '{d.Xp.Kind}'"),
    });

    public static long KillXp(CurveData d, int level) =>
        (long)Math.Round(d.Income.KillXpBase * Math.Pow(level, d.Income.KillXpExponent));

    public static long QuestXp(CurveData d, int level) => (long)(d.Income.QuestKillEquivalent * KillXp(d, level));

    /// <summary>XP per hour of play at this level, for a player doing the region's usual mix. xpMultiplier models bonuses (1 = solo baseline).</summary>
    public static double XpPerHour(CurveData d, int level, double xpMultiplier = 1.0)
    {
        var r = d.RegionOf(level);
        return xpMultiplier * (r.KillsPerHour * KillXp(d, level) + r.QuestsPerHour * QuestXp(d, level));
    }

    /// <summary>Hours to go from level L to L+1: the XP bar divided by XP per hour.</summary>
    public static double HoursForLevel(CurveData d, int level, double xpMultiplier = 1.0) =>
        XpToNext(d, level) / XpPerHour(d, level, xpMultiplier);

    /// <summary>Hours of play from level 1 until the hero reaches the given level.</summary>
    public static double HoursToReach(CurveData d, int level, double xpMultiplier = 1.0) =>
        Enumerable.Range(1, level - 1).Sum(l => HoursForLevel(d, l, xpMultiplier));

    public static double TotalHours(CurveData d, double xpMultiplier = 1.0) => HoursToReach(d, d.MaxLevel, xpMultiplier);

    /// <summary>Hours spent on the levels played inside one region.</summary>
    public static double RegionHours(CurveData d, Region r, double xpMultiplier = 1.0) =>
        Enumerable.Range(r.FromLevel, r.ToLevel - r.FromLevel + 1).Sum(l => HoursForLevel(d, l, xpMultiplier));

    /// <summary>The other direction: from a time budget per level to the XP table (rounded to the nearest XP).</summary>
    public static long[] DeriveXpTable(CurveData d, Func<int, double> hoursPerLevel) =>
        Enumerable.Range(1, d.MaxLevel - 1).Select(l => (long)Math.Round(hoursPerLevel(l) * XpPerHour(d, l))).ToArray();

    /// <summary>Extra XP a returning player gets: bonus x the XP earned, paid from the pool, which fills with offline hours.</summary>
    public static double RestedExtraXp(CurveData d, int level, double offlineHours, double baseXpEarned)
    {
        double bars = Math.Min(d.Rested.PoolCapBars, d.Rested.PoolPerOfflineHour * offlineHours);
        return Math.Min(bars * XpToNext(d, level), d.Rested.Bonus * baseXpEarned);
    }

    // ---- Power curve against content ----

    /// <summary>Level power: 1.0 at level 1, growing by StatGrowth per level.</summary>
    public static double LevelPower(CurveData d, int level) => 1 + d.Power.StatGrowth * (level - 1);

    /// <summary>The level of the quest hub the hero is working in (hubs sit every LevelsPerHub levels).</summary>
    public static int HubLevel(CurveData d, int level) => 1 + d.Power.LevelsPerHub * ((level - 1) / d.Power.LevelsPerHub);

    /// <summary>Gear factor, linearly interpolated between the anchors in the data file.</summary>
    public static double GearFactor(CurveData d, int level)
    {
        var g = d.Power.Gear;
        var hi = g.First(a => a.Level >= level);
        var lo = g.Last(a => a.Level <= level);
        return hi.Level == lo.Level ? lo.Factor : lo.Factor + (hi.Factor - lo.Factor) * (level - lo.Level) / (hi.Level - lo.Level);
    }

    /// <summary>Hero power divided by the power of the content at the hero's hub. 1.0 means an even fight.</summary>
    public static double PowerRatio(CurveData d, int level) =>
        GearFactor(d, level) * LevelPower(d, level) / LevelPower(d, HubLevel(d, level));
}
