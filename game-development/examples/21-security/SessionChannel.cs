using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Course.Security;

/// <summary>One end of an authenticated message stream. The tag (HMAC-SHA256 under the session
/// key) covers the sequence number and the payload, so editing either is detected. Sequence
/// numbers must strictly increase, so a recorded message cannot be sent again. This provides
/// integrity and freshness, not secrecy: confidentiality comes from TLS underneath.</summary>
public sealed class SessionChannel
{
    private readonly byte[] _key;
    private long _nextSendSeq = 1;
    private long _lastReceivedSeq;

    public SessionChannel(byte[] sessionKey) => _key = sessionKey;

    public SealedMessage Seal(byte[] payload)
    {
        var seq = _nextSendSeq++;
        return new SealedMessage(seq, payload, Tag(seq, payload));
    }

    public OpenResult Open(SealedMessage message, out byte[] payload)
    {
        payload = [];
        // 1. Authenticate first, so an attacker without the key learns nothing from the replay check.
        if (!CryptographicOperations.FixedTimeEquals(Tag(message.Seq, message.Payload), message.Tag))
            return OpenResult.Tampered;
        // 2. Then freshness: anything not newer than the last accepted message is a replay.
        if (message.Seq <= _lastReceivedSeq) return OpenResult.Replayed;

        _lastReceivedSeq = message.Seq;
        payload = message.Payload;
        return OpenResult.Ok;
    }

    private byte[] Tag(long seq, byte[] payload)
    {
        var data = new byte[8 + payload.Length];
        BinaryPrimitives.WriteInt64BigEndian(data, seq);
        payload.CopyTo(data, 8);
        return HMACSHA256.HashData(_key, data);
    }
}
