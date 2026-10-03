namespace Course.Skills;

/// <summary>Who a skill hits, relative to the caster and the chosen target.</summary>
public enum TargetShape
{
    Self,
    Single,

    /// <summary>Everyone within the skill's radius of the chosen target (the target included).</summary>
    Circle,
}
