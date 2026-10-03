namespace Course.Scripting;

/// <summary>A quest row from data. <see cref="CompleteHook"/> is a hook name; null means "no extra logic".</summary>
public sealed record QuestDef(string Id, string CompleteHook, string? GoldFormula = null);
