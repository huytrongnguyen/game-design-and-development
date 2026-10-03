namespace Course.Security;

/// <summary>What travels on the wire: sequence number, payload, and an authentication tag over both.</summary>
public sealed record SealedMessage(long Seq, byte[] Payload, byte[] Tag);
