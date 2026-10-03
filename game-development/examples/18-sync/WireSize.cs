namespace Course.Sync;

/// <summary>
/// Byte-count estimate of a message on the wire: 1 byte type tag, 4 byte id, then the payload.
/// Position is two 4-byte floats, hp and appearance are 4-byte ints, an update adds a 1-byte field mask.
/// Real protocols quantise harder (16-bit positions, varints); the ratios are what matter.
/// </summary>
public static class WireSize
{
    public const int Header = 1 + 4;

    public static int Of(SyncMessage message) => message switch
    {
        EnterMessage => Header + 8 + 4 + 4,
        UpdateMessage u => Header + 1 + PayloadOf(u.Mask),
        LeaveMessage => Header,
        _ => throw new ArgumentOutOfRangeException(nameof(message)),
    };

    public static int PayloadOf(FieldMask mask)
    {
        var bytes = 0;
        if ((mask & FieldMask.Position) != 0) bytes += 8;
        if ((mask & FieldMask.Hp) != 0) bytes += 4;
        if ((mask & FieldMask.Appearance) != 0) bytes += 4;
        return bytes;
    }
}
