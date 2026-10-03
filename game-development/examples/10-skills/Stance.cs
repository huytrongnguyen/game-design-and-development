namespace Course.Skills;

/// <summary>
/// A stance is a loadout: which skills are usable, which modifiers are always on,
/// and how SP flows (positive regenerates, negative drains) every regen interval.
/// </summary>
public sealed record Stance(
    string Id,
    IReadOnlyList<string> SkillIds,
    IReadOnlyList<StatModifier> Modifiers,
    int SpPerInterval);
