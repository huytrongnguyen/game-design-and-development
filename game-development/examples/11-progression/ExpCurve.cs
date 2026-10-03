namespace Course.Progression;

/// <summary>Experience needed per level, as a power curve from <see cref="ProgressionRules"/>.</summary>
public sealed class ExpCurve(ProgressionRules rules)
{
    /// <summary>Exp needed to go from <paramref name="level"/> to the next level.</summary>
    public long ExpToNext(int level) => (long)Math.Round(rules.ExpBase * Math.Pow(level, rules.ExpPower), MidpointRounding.AwayFromZero);

    /// <summary>Total exp needed to reach <paramref name="level"/> from level 1.</summary>
    public long ExpToReach(int level)
    {
        long total = 0;
        for (int l = 1; l < level; l++) total += ExpToNext(l);
        return total;
    }

    /// <summary>The level a character has, given total exp, capped at the maximum.</summary>
    public int LevelFor(long totalExp)
    {
        int level = 1;
        long needed = 0;
        while (level < rules.MaxLevel && totalExp >= needed + ExpToNext(level))
        {
            needed += ExpToNext(level);
            level++;
        }
        return level;
    }
}
