namespace Course.Skills;

/// <summary>One thing a skill does to each unit it hits. A skill is a list of these, so new skills are data.</summary>
public interface ISkillEffect
{
    void Apply(Combatant caster, int skillLevel, Combatant target, int now);
}
