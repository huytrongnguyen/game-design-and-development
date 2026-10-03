namespace M24.Pipeline;

/// <summary>All authored tables, as loaded from the authoring format.</summary>
public sealed record ContentSet(SkillDef[] Skills, ItemDef[] Items, MonsterDef[] Monsters);
