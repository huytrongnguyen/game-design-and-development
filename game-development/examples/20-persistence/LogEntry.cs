namespace Course.Persistence;

/// <summary>One immutable fact in the append-only audit log.</summary>
public sealed record LogEntry(long Seq, string OpId, string Kind, IReadOnlyList<Change> Changes);
