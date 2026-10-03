namespace M17.Net;

/// <summary>Full = every player in the zone. Otherwise only the players that changed this tick.</summary>
public sealed record StatePayload(long Tick, bool Full, IReadOnlyList<PlayerView> Players);
