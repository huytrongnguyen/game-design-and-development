namespace Course.Combat;

/// <summary>
/// The server-side attack pipeline. Order matters: cheap early-outs first, then mitigation,
/// then multipliers, then one final floor. All randomness comes from the injected generator.
/// </summary>
public sealed class AttackResolver(CombatRng rng, CombatRules rules)
{
    private readonly CombatFormulas _f = new(rules);

    public AttackResult Resolve(AttackerProfile a, DefenderProfile d)
    {
        // 1. Flat evasion: a straight miss chance, independent of every other stat.
        if (d.Evasion > rng.Next(1, 100))
            return new(AttackOutcome.Miss, 0);

        // 2. Block, then parry. Attacker rank advantage lowers both chances.
        if (rng.Next(1, 100) < _f.EffectiveAvoid(d.Block, a.AttackRank, d.DefenseRank))
            return new(AttackOutcome.Block, 0);
        if (rng.Next(1, 100) < _f.EffectiveAvoid(d.Parry, a.AttackRank, d.DefenseRank))
            return new(AttackOutcome.Block, 0);

        // 3. Base roll: hit rate sets the floor of the range, never a miss.
        double damage = a.HitRate >= 100 ? a.Atk : rng.Next(a.Atk * a.HitRate / 100, a.Atk);

        // 4. Defense removes a percentage of the physical part.
        damage *= (100 - _f.DefensePercent(d.Def, a.AttackRank)) / 100;

        // 5. Flat elemental damage, reduced by the matching resistance.
        if (a.Elemental is { } e)
            damage += e.Amount * (100 - d.Resists.Get(e.Element)) / 100.0;

        // 6. Chip-damage floor, then the level-gap multiplier.
        damage = Math.Max(damage, rules.MinDamage);
        damage *= _f.GradeFix(a.AttackRank, d.DefenseRank);

        // 7. Multipliers: skill power, armor-type matchup, and (if rolled) a critical.
        damage *= a.SkillRatio * _f.ArmorTypeFix(a.WeaponGroup, d.ArmorType);

        bool crit = a.CritRate > 0 && rng.Next(1, 100) <= Math.Min(100, a.CritRate);
        if (crit)
            damage *= _f.CritMultiplier(a.CritPower, d.CritDefense);

        // 8. One floor at the end; never round between steps.
        int final = (int)Math.Floor(damage);
        return new(crit ? AttackOutcome.Crit : AttackOutcome.Hit, final);
    }
}
