namespace M17.Net;

/// <summary>Fields are nullable so a missing field is detected instead of silently becoming 0.</summary>
public sealed record HelloPayload(string? Name, int? Protocol);
