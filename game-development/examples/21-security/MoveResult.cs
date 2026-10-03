namespace Course.Security;

/// <summary>The server's answer to a claimed position. <see cref="Position"/> is always the
/// authoritative one: the claim if accepted, the last accepted position if not.</summary>
public readonly record struct MoveResult(bool Accepted, Point2 Position, double Claimed, double Allowed);
