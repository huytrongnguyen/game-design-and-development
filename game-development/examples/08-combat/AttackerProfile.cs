namespace Course.Combat;

/// <summary>Everything the resolver needs to know about the attacker, already derived and frozen for this swing.</summary>
public sealed record AttackerProfile
{
    public required int AttackRank { get; init; }
    public required int Atk { get; init; }
    /// <summary>Hit rate 0..100+. In this model it sets the floor of the damage roll, not a chance to miss.</summary>
    public int HitRate { get; init; } = 100;
    /// <summary>Skill power relative to a plain swing (1.0 = plain swing).</summary>
    public double SkillRatio { get; init; } = 1.0;
    public WeaponGroup WeaponGroup { get; init; } = WeaponGroup.Pierce;
    public ElementalAttack? Elemental { get; init; }
    /// <summary>Crit chance in percent; 0 means the attacker cannot crit.</summary>
    public int CritRate { get; init; }
    public int CritPower { get; init; }
}
