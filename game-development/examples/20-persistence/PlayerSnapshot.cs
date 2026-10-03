namespace Course.Persistence;

/// <summary>The cheap-to-lose part of a player (where they stand, experience). It is saved
/// by checkpoints, not by transactions.</summary>
public sealed record PlayerSnapshot(string PlayerId, string Zone, int X, int Y, long Exp);
