using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Course.Security;

/// <summary>Time-based one-time passwords (RFC 6238) on top of HOTP (RFC 4226).
/// Time is passed in as Unix seconds so the code is deterministic and testable.</summary>
public static class Totp
{
    public static string Generate(ReadOnlySpan<byte> secret, long unixSeconds, int digits = 6, int stepSeconds = 30)
        => ForCounter(secret, unixSeconds / stepSeconds, digits);

    public static long CounterFor(long unixSeconds, int stepSeconds = 30) => unixSeconds / stepSeconds;

    public static string ForCounter(ReadOnlySpan<byte> secret, long counter, int digits = 6)
    {
        Span<byte> message = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(message, counter);          // 8-byte big-endian counter

        Span<byte> mac = stackalloc byte[20];
        HMACSHA1.HashData(secret, message, mac);                         // RFC 6238's reference hash

        var offset = mac[^1] & 0x0F;                                     // "dynamic truncation"
        var binary = ((mac[offset] & 0x7F) << 24) | (mac[offset + 1] << 16)
                   | (mac[offset + 2] << 8) | mac[offset + 3];

        var modulus = (int)Math.Pow(10, digits);
        return (binary % modulus).ToString().PadLeft(digits, '0');
    }
}
