namespace Course.Persistence;

/// <summary>A store that behaves like a transactional database: changes are staged in a
/// private workspace, validated, and only then swapped in together with the operation id
/// and the log entry. Anything that throws before the swap leaves no trace.</summary>
public sealed class InMemoryGameStore : IGameStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, PlayerRecord> _players = new();
    private readonly Dictionary<string, PlayerSnapshot> _snapshots = new();
    private readonly Dictionary<string, SagaRecord> _sagas = new();
    private readonly HashSet<string> _applied = new();
    private readonly List<LogEntry> _log = new();

    /// <summary>Test hook: called for each staged change, before anything is committed.
    /// Throwing here simulates a crash in the middle of a transaction.</summary>
    public Action<int>? BeforeCommit { get; set; }

    /// <summary>How many snapshot writes reached "the database" (to show write-behind coalescing).</summary>
    public int SnapshotWrites { get; private set; }

    public IReadOnlyList<LogEntry> Log
    {
        get { lock (_gate) return _log.ToArray(); }
    }

    public PlayerRecord? Find(string playerId)
    {
        lock (_gate) return _players.GetValueOrDefault(playerId);
    }

    public bool CreatePlayer(PlayerRecord player)
    {
        lock (_gate) return _players.TryAdd(player.Id, player);
    }

    public ApplyResult Apply(string opId, string kind, IReadOnlyList<Change> changes)
    {
        lock (_gate)
        {
            if (_applied.Contains(opId)) return new(ApplyOutcome.Duplicate);   // idempotency
            if (changes.Count == 0) return new(ApplyOutcome.Rejected, "no changes");

            var staged = new Dictionary<string, PlayerRecord>();
            for (var i = 0; i < changes.Count; i++)
            {
                var c = changes[i];
                if (!staged.TryGetValue(c.PlayerId, out var p))
                {
                    p = _players.GetValueOrDefault(c.PlayerId);
                    if (p is null) return new(ApplyOutcome.Rejected, $"unknown player {c.PlayerId}");
                }
                p = p.Apply(c);
                if (p.Gold < 0) return new(ApplyOutcome.Rejected, $"insufficient gold: {c.PlayerId}");
                if (c.ItemId is not null && p.Count(c.ItemId) < 0)
                    return new(ApplyOutcome.Rejected, $"insufficient {c.ItemId}: {c.PlayerId}");
                staged[c.PlayerId] = p;
                BeforeCommit?.Invoke(i);
            }

            // Commit point: from here on nothing can fail.
            foreach (var (id, record) in staged) _players[id] = record;
            _applied.Add(opId);
            _log.Add(new LogEntry(_log.Count + 1, opId, kind, changes.ToArray()));
            return new(ApplyOutcome.Applied);
        }
    }

    public void SaveSnapshot(PlayerSnapshot snapshot)
    {
        lock (_gate) { _snapshots[snapshot.PlayerId] = snapshot; SnapshotWrites++; }
    }

    public PlayerSnapshot? LoadSnapshot(string playerId)
    {
        lock (_gate) return _snapshots.GetValueOrDefault(playerId);
    }

    public SagaRecord? FindSaga(string sagaId)
    {
        lock (_gate) return _sagas.GetValueOrDefault(sagaId);
    }

    public void SaveSaga(SagaRecord saga)
    {
        lock (_gate) _sagas[saga.Id] = saga;
    }
}
