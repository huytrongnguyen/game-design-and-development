namespace Course.Sync;

/// <summary>"Disappear": the observer must forget the entity.</summary>
public sealed record LeaveMessage(EntityId Id) : SyncMessage(Id);
