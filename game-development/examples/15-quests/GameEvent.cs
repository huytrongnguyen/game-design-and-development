namespace Course.Quests;

/// <summary>A fact the simulation publishes ("a wolf died", "player entered the cave").
/// The quest system only listens; it never reaches into combat or movement code.</summary>
public readonly record struct GameEvent(ObjectiveKind Kind, string Target, int Amount = 1);
