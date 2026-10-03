namespace Course.Skills;

/// <summary>Static data for one skill: cost, timing, reach, target shape and effects.</summary>
public sealed record SkillDefinition(
    string Id,
    int SpCost,
    int CooldownTicks,
    int CastTicks,
    int Range,
    TargetShape Shape,
    int Radius,
    IReadOnlyList<ISkillEffect> Effects);
