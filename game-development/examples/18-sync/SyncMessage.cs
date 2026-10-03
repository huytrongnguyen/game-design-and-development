namespace Course.Sync;

/// <summary>Base of everything the server sends about an entity.</summary>
public abstract record SyncMessage(EntityId Id);
