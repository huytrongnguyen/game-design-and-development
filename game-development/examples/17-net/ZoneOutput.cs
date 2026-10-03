namespace M17.Net;

/// <summary>A result the zone wants delivered to every connected player.</summary>
public sealed record ZoneOutput(string Op, object Payload);
