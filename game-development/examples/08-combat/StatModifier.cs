namespace Course.Combat;

/// <summary>One contribution to a stat. <c>Source</c> lets us remove everything an item or buff added in one call.</summary>
public readonly record struct StatModifier(string Source, ModifierLayer Layer, double Value);
