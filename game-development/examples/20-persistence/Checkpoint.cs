namespace Course.Persistence;

public readonly record struct Checkpoint(string PlayerId, long Tick, CheckpointReason Reason);
