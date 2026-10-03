namespace Course.Quests;

/// <summary>Static rules of an instanced mission. Difficulty tiers are separate rows here
/// (same shape, different numbers) rather than a script per tier.</summary>
public sealed record MissionDef(
    string Id,
    string Tier,
    int MinPlayers,
    int MaxPlayers,
    int MinLevel,
    long TimeLimitTicks,
    int PointsPerKill,
    int GradeS,
    int GradeA);
