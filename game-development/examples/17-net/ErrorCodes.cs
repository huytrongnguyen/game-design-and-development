namespace M17.Net;

/// <summary>Machine-readable error codes sent in an <c>error</c> message.</summary>
public static class ErrorCodes
{
    public const string BadJson = "bad_json";
    public const string BadPayload = "bad_payload";
    public const string UnknownOp = "unknown_op";
    public const string RateLimited = "rate_limited";
    public const string HandshakeRequired = "handshake_required";
    public const string AlreadyHandshaken = "already_handshaken";
    public const string VersionMismatch = "version_mismatch";
}
