namespace M24.Pipeline;

/// <summary>An item row as authored. <c>Skill</c> (optional) is a reference to a skill id.</summary>
public sealed record ItemDef(string Id, string NameKey, string? Skill = null);
