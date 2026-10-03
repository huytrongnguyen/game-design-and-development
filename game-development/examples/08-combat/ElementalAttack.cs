namespace Course.Combat;

/// <summary>Flat damage of one element added on top of the physical hit.</summary>
public readonly record struct ElementalAttack(Element Element, int Amount);
