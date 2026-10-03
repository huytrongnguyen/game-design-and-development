using System.Security.Cryptography;
using System.Text;

namespace Course.Topology;

/// <summary>
/// Issues and checks short-lived, signed session tokens: <c>base64url(payload).base64url(HMAC-SHA256)</c>.
/// Any service holding the key can validate a token without calling the login service.
/// </summary>
public sealed class SessionTokenService
{
    private readonly byte[] _key;
    private readonly IClock _clock;

    public SessionTokenService(byte[] key, IClock clock)
    {
        if (key.Length < 16) throw new ArgumentException("Key must be at least 16 bytes.", nameof(key));
        _key = key;
        _clock = clock;
    }

    public string Issue(string accountId, long ttlSeconds)
    {
        if (accountId.Contains('|')) throw new ArgumentException("Account id may not contain '|'.", nameof(accountId));
        var payload = $"{accountId}|{_clock.NowSeconds + ttlSeconds}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        return $"{Base64Url(payloadBytes)}.{Base64Url(Sign(payloadBytes))}";
    }

    public TokenValidation Validate(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2) return Fail(TokenStatus.Malformed);

        byte[] payloadBytes, signature;
        try
        {
            payloadBytes = FromBase64Url(parts[0]);
            signature = FromBase64Url(parts[1]);
        }
        catch (FormatException)
        {
            return Fail(TokenStatus.Malformed);
        }

        // Constant-time comparison: do not leak how many bytes matched.
        if (!CryptographicOperations.FixedTimeEquals(Sign(payloadBytes), signature))
            return Fail(TokenStatus.BadSignature);

        var fields = Encoding.UTF8.GetString(payloadBytes).Split('|');
        if (fields.Length != 2 || !long.TryParse(fields[1], out var expiresAt))
            return Fail(TokenStatus.Malformed);

        if (_clock.NowSeconds >= expiresAt) return Fail(TokenStatus.Expired);
        return new TokenValidation(TokenStatus.Valid, new TokenClaims(fields[0], expiresAt));
    }

    private static TokenValidation Fail(TokenStatus status) => new(status, null);

    private byte[] Sign(byte[] payload) => HMACSHA256.HashData(_key, payload);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string text)
    {
        var s = text.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
