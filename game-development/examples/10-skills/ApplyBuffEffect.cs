namespace Course.Skills;

/// <summary>Applies a buff or debuff to each target, under that buff's own stacking policy.</summary>
public sealed record ApplyBuffEffect(BuffDefinition Buff) : ISkillEffect
{
    public void Apply(Combatant caster, int skillLevel, Combatant target, int now) =>
        target.Buffs.Apply(Buff, now);
}
