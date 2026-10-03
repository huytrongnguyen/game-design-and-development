namespace Course.Combat;

/// <summary>What the server tells everyone happened. Misses and blocks carry zero damage.</summary>
public readonly record struct AttackResult(AttackOutcome Outcome, int Damage);
