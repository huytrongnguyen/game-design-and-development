namespace Course.Sync;

/// <summary>"Appear": the full state of an entity the observer did not know yet.</summary>
public sealed record EnterMessage(EntityId Id, float X, float Y, int Hp, int Appearance) : SyncMessage(Id);
