namespace M12.Ai;

public readonly record struct AllyDecision(AllyAction Action, int? TargetId, double Score);
