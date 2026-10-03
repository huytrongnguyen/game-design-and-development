namespace Course.Quests;

/// <summary>One line of the quest log: "Kill 3 wolf", "Talk to elder", "Reach cave".</summary>
public sealed record ObjectiveDef(string Id, ObjectiveKind Kind, string Target, int Count = 1);
