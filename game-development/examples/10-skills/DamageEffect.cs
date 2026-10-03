namespace Course.Skills;

/// <summary>
/// Direct damage. Damage = attack * Power% * (100 + level * PerLevelPercent) / 100.
/// Both percentages are data, so a different game only changes the numbers.
/// Percentages are stored as integers (250 = 2.5x) so the math stays integral.
/// </summary>
public sealed record DamageEffect(int PowerPercent, int PerLevelPercent = 10) : ISkillEffect
{
    public static int Compute(int atk, int powerPercent, int perLevelPercent, int skillLevel) =>
        atk * powerPercent * (100 + skillLevel * perLevelPercent) / 10_000;

    public void Apply(Combatant caster, int skillLevel, Combatant target, int now) =>
        target.TakeDamage(Compute(caster.Stat(Stat.Atk), PowerPercent, PerLevelPercent, skillLevel));
}
