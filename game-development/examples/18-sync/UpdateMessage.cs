namespace Course.Sync;

/// <summary>A delta: only the fields named in <see cref="Mask"/> are meaningful and cost bytes.</summary>
public sealed record UpdateMessage(EntityId Id, FieldMask Mask, float X, float Y, int Hp, int Appearance)
    : SyncMessage(Id);
