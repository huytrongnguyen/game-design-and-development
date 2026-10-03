namespace Course.Sync;

/// <summary>Everything the replication pass produced for one tick.</summary>
public sealed class TickResult
{
    public TickResult(long tick, IReadOnlyDictionary<EntityId, List<SyncMessage>> byViewer)
    {
        Tick = tick;
        ByViewer = byViewer;
        foreach (var list in byViewer.Values)
            foreach (var m in list)
                TotalBytes += WireSize.Of(m);
    }

    public long Tick { get; }
    public IReadOnlyDictionary<EntityId, List<SyncMessage>> ByViewer { get; }
    public int TotalBytes { get; }

    public IReadOnlyList<SyncMessage> For(EntityId viewer) => ByViewer[viewer];
}
