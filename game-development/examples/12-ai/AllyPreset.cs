namespace M12.Ai;

/// <summary>A role preset: the weights and ranges that turn one scoring function into a healer, an attacker, and so on.</summary>
public sealed record AllyPreset(
    string Name,
    double HealWeight,
    double AttackWeight,
    double FollowWeight,
    double HealThreshold,
    double HealRange,
    double AttackRange,
    double FollowDistance)
{
    public static AllyPreset Healer { get; } = new("Healer", 1.0, 0.2, 0.8, 0.7, 15, 12, 6);
    public static AllyPreset Attacker { get; } = new("Attacker", 0.0, 1.0, 0.8, 0.7, 15, 12, 6);
}
