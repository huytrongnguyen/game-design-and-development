namespace Course.Progression;

/// <summary>Why an action was refused. <see cref="None"/> means it succeeded.</summary>
public enum Denial
{
    None, UnknownClass, UnknownSkill, MaxCircles, TotalCircleCap, LevelTooLow, MissingPrerequisite, RankFull,
    SkillLocked, NoPoints, SkillMaxed, StatCap, NotEnoughGold,
}

/// <summary>
/// All progression rules in one place: experience and levels, advancing in the class tree, free-point and
/// skill-point spending, derived stats and respecs. Every limit comes from <see cref="ProgressionRules"/>.
/// </summary>
public sealed class ProgressionService(ClassTree tree)
{
    private readonly ExpCurve _curve = new(tree.Rules);
    private ProgressionRules Rules => tree.Rules;

    // ---- Levels ----

    public void AddExp(Character c, long amount)
    {
        c.Exp += amount;
        c.Level = _curve.LevelFor(c.Exp);
    }

    // ---- Advancement ----

    public Denial CanAdvance(Character c, string classId)
    {
        var cls = tree.Find(classId);
        if (cls is null) return Denial.UnknownClass;
        c.Circles.TryGetValue(classId, out int have);
        if (have >= cls.MaxCircles) return Denial.MaxCircles;
        if (c.TotalCircles >= Rules.MaxTotalCircles) return Denial.TotalCircleCap;
        if (c.TotalCircles + 1 > c.Level / Rules.LevelsPerCircle) return Denial.LevelTooLow;
        foreach (var p in cls.Requires)
            if (!c.Circles.TryGetValue(p.Class, out int got) || got < p.MinCircles) return Denial.MissingPrerequisite;
        if (have == 0)   // opening a new class uses up a slot of its rank
        {
            int slots = Rules.ClassesPerRank.GetValueOrDefault(cls.Rank, 1);
            int used = c.Circles.Keys.Count(id => tree.Find(id)?.Rank == cls.Rank);
            if (used >= slots) return Denial.RankFull;
        }
        return Denial.None;
    }

    public Denial Advance(Character c, string classId)
    {
        var denial = CanAdvance(c, classId);
        if (denial == Denial.None) c.Circles[classId] = c.Circles.GetValueOrDefault(classId) + 1;
        return denial;
    }

    // ---- Stats ----

    public int FreePointsAvailable(Character c) => Rules.FreePointsPerLevel * (c.Level - 1) - c.FreeAllocated.Total;

    public Denial SpendFreePoint(Character c, Stat stat, int points = 1)
    {
        if (points > FreePointsAvailable(c)) return Denial.NoPoints;
        if (c.FreeAllocated.Get(stat) + points > Rules.MaxFreePointsPerStat) return Denial.StatCap;
        c.FreeAllocated = c.FreeAllocated.With(stat, c.FreeAllocated.Get(stat) + points);
        return Denial.None;
    }

    /// <summary>Base stats + growth of every circle taken + the player's free points.</summary>
    public StatBlock TotalStats(Character c)
    {
        var total = c.BaseStats + c.FreeAllocated;
        foreach (var (id, circles) in c.Circles) total += tree.Find(id)!.Growth * circles;
        return total;
    }

    // ---- Skills ----

    public int SkillPointsEarned(Character c) => c.Circles.Sum(kv => tree.Find(kv.Key)!.SkillPointsPerCircle * kv.Value);
    public int SkillPointsAvailable(Character c) => SkillPointsEarned(c) - c.SkillLevels.Values.Sum();

    public Denial SpendSkillPoint(Character c, string skillId)
    {
        if (tree.FindSkill(skillId) is not var (cls, skill)) return Denial.UnknownSkill;
        if (c.Circles.GetValueOrDefault(cls.Id) < skill.UnlockCircle) return Denial.SkillLocked;
        int level = c.SkillLevels.GetValueOrDefault(skillId);
        if (level >= skill.MaxLevel) return Denial.SkillMaxed;
        if (SkillPointsAvailable(c) < 1) return Denial.NoPoints;
        c.SkillLevels[skillId] = level + 1;
        return Denial.None;
    }

    // ---- Respec ----

    /// <summary>Takes back all free points for gold per point. Fails without changing anything if gold is short.</summary>
    public Denial RespecStats(Character c)
    {
        long cost = (long)c.FreeAllocated.Total * Rules.StatRespecGoldPerPoint;
        if (c.Gold < cost) return Denial.NotEnoughGold;
        c.Gold -= cost;
        c.FreeAllocated = default;
        return Denial.None;
    }

    /// <summary>Free: skill points go back to the pool.</summary>
    public void RespecSkills(Character c) => c.SkillLevels.Clear();

    /// <summary>Gives up every circle (and so every skill) for gold per circle.</summary>
    public Denial ResetClasses(Character c)
    {
        long cost = (long)c.TotalCircles * Rules.ClassResetGoldPerCircle;
        if (c.Gold < cost) return Denial.NotEnoughGold;
        c.Gold -= cost;
        c.Circles.Clear();
        c.SkillLevels.Clear();
        return Denial.None;
    }
}
