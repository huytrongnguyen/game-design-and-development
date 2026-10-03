namespace M12.Ai;

/// <summary>Tuning for one monster type. Distances are world units, times are ticks.</summary>
public sealed record MonsterConfig
{
    public double AggroRadius { get; init; } = 10;
    public double AttackRange { get; init; } = 2;
    /// <summary>Maximum distance from home before the monster gives up and walks back.</summary>
    public double LeashDistance { get; init; } = 30;
    public double SpeedPerTick { get; init; } = 1;
    public int AttackDamage { get; init; } = 5;
    public int AttackCooldownTicks { get; init; } = 10;
    public int MaxHp { get; init; } = 100;
    /// <summary>The monster decides every N ticks; movement still runs every tick.</summary>
    public int ThinkIntervalTicks { get; init; } = 5;
    public double InitialHate { get; init; } = 20;
    public double HateDecayPerThink { get; init; } = 0.02;
}
