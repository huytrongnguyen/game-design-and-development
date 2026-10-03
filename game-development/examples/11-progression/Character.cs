namespace Course.Progression;

/// <summary>The mutable progression state of one character. All rules live in <see cref="ProgressionService"/>.</summary>
public sealed class Character
{
    public int Level { get; internal set; } = 1;
    public long Exp { get; internal set; }
    public long Gold { get; set; }
    public StatBlock BaseStats { get; init; } = new(5, 5, 5, 5);
    /// <summary>Circles taken per class id.</summary>
    public Dictionary<string, int> Circles { get; } = [];
    /// <summary>Free points the player has allocated, per stat.</summary>
    public StatBlock FreeAllocated { get; internal set; }
    /// <summary>Skill level per skill id.</summary>
    public Dictionary<string, int> SkillLevels { get; } = [];

    public int TotalCircles => Circles.Values.Sum();
}
