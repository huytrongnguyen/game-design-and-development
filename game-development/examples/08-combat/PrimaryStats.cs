namespace Course.Combat;

/// <summary>The four base attributes of the sample ruleset. Everything else (HP, hit rate, attack interval...) is derived from them.</summary>
public readonly record struct PrimaryStats(int Strength, int Agility, int Vitality, int Intellect)
{
    public static PrimaryStats operator +(PrimaryStats a, PrimaryStats b) =>
        new(a.Strength + b.Strength, a.Agility + b.Agility, a.Vitality + b.Vitality, a.Intellect + b.Intellect);
}
