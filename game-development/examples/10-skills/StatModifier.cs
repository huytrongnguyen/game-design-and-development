namespace Course.Skills;

/// <summary>One declared effect on a stat. Buffs and stances carry these; the stat code never asks "which buff".</summary>
public readonly record struct StatModifier(Stat Stat, ModifierOp Op, int Value);
