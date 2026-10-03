using System.Text.Json;

namespace M17.Net;

/// <summary>
/// Every message in both directions has the same outer shape: what it is (Op),
/// which request it answers (Seq) and its body (Payload).
/// </summary>
public sealed record Envelope(string Op, int Seq, JsonElement Payload);
