namespace Course.Quests;

public readonly record struct MissionResult(string MissionId, MissionOutcome Outcome, int Score, char Grade);
