namespace Course.Ttk;

/// <summary>The spreadsheet view: closed-form expected values, no randomness.</summary>
public static class Formulas
{
    /// <summary>Damage after defence. Subtractive: hit - defence, never below MinFraction of the hit.
    /// Ratio: hit * K / (K + defence), so defence is a percentage reduction that never reaches 100%.</summary>
    public static double ApplyDefence(DefenceRule rule, double hit, double defence) => rule.Model switch
    {
        DefenceModel.Subtractive => Math.Max(rule.MinFraction * hit, hit - defence),
        _ => hit * rule.RatioConstant / (rule.RatioConstant + defence),
    };

    /// <summary>Average damage of one swing: crit and non-crit outcomes weighted by crit chance.
    /// The crit multiplier applies to the raw hit, before defence.</summary>
    public static double ExpectedHit(DefenceRule rule, HeroClass c, double defence) =>
        (1 - c.CritChance) * ApplyDefence(rule, c.DamagePerHit, defence)
        + c.CritChance * ApplyDefence(rule, c.DamagePerHit * c.CritMultiplier, defence);

    public static double Dps(DefenceRule rule, HeroClass c, double defence) =>
        ExpectedHit(rule, c, defence) / c.AttackInterval;

    /// <summary>Enemy health divided by the team's combined damage per second.</summary>
    public static double ExpectedTtk(DefenceRule rule, IEnumerable<HeroClass> team, double enemyHp, double enemyDef) =>
        enemyHp / team.Sum(c => Dps(rule, c, enemyDef));

    /// <summary>Seconds a hero survives if every enemy attack lands: no dodging, no healing, no kills thinning the pack.</summary>
    public static double TimeToDie(DefenceRule rule, HeroClass hero, Enemy enemy) =>
        hero.Hp / (enemy.Attackers * ApplyDefence(rule, enemy.DamagePerHit, hero.Defence) / enemy.AttackInterval);

    /// <summary>Share of enemy damage the player must avoid (dodge, block, kite) to win: 0 = none, 1 = all of it.</summary>
    public static double RequiredAvoidance(double ttk, double ttd) => Math.Clamp(1 - ttd / ttk, 0, 1);
}
