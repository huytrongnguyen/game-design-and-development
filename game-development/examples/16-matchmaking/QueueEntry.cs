namespace Course.Matchmaking;

/// <summary>One player waiting in the queue. Immutable; the wait is derived from the tick it was enqueued.</summary>
public sealed record QueueEntry(string PlayerId, Role Role, int Level, string Shard, long EnqueuedTick);
