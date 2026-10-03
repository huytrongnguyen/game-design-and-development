namespace Course.Quests;

/// <summary>What turning the quest in grants. <see cref="UnlockCharacter"/> is the
/// "story gates a recruitable character" hook: the quest system names it, the roster grants it.</summary>
public sealed record RewardDef(int Exp = 0, int Gold = 0, IReadOnlyList<string>? Items = null, string? UnlockCharacter = null);
