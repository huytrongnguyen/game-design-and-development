namespace M24.Pipeline;

/// <summary>A skill row as authored. <c>NameKey</c> points into the string table.</summary>
public sealed record SkillDef(string Id, string NameKey, int Power);
