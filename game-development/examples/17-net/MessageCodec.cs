using System.Text.Json;

namespace M17.Net;

/// <summary>Text encoding of the protocol: one JSON envelope per WebSocket message.</summary>
public static class MessageCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static bool TryParse(string text, out Envelope? envelope)
    {
        envelope = null;
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("op", out var op) || op.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var seq = root.TryGetProperty("seq", out var s) && s.TryGetInt32(out var n) ? n : 0;
            var payload = root.TryGetProperty("payload", out var p) ? p.Clone() : default;
            envelope = new Envelope(op.GetString()!, seq, payload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryReadPayload<T>(Envelope envelope, out T? payload) where T : class
    {
        payload = null;
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        try
        {
            payload = envelope.Payload.Deserialize<T>(Options);
            return payload is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string Serialize(string op, int seq, object? payload = null) =>
        JsonSerializer.Serialize(new { op, seq, payload }, Options);

    public static string Error(int seq, string code, string message) =>
        Serialize(Ops.Error, seq, new { code, message });
}
