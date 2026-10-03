namespace Course.Combat;

public sealed record DefenderProfile
{
    public required int DefenseRank { get; init; }
    public required int Def { get; init; }
    public int Block { get; init; }
    public int Parry { get; init; }
    /// <summary>Flat chance (percent) to dodge outright, rolled before anything else.</summary>
    public int Evasion { get; init; }
    public ArmorType ArmorType { get; init; } = ArmorType.Medium;
    public ElementResists Resists { get; init; }
    public int CritDefense { get; init; }
}
