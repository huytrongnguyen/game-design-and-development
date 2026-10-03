namespace M24.Pipeline;

/// <summary>A monster row as authored. <c>Drops</c> are references to item ids.</summary>
public sealed record MonsterDef(string Id, string NameKey, string[] Drops);
