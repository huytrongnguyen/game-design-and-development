namespace Course.Persistence;

/// <summary>What the game needs from a database, and nothing more. The in-memory version
/// below is for tests; a PostgreSQL version implements the same members (see schema.sql).</summary>
public interface IGameStore
{
    PlayerRecord? Find(string playerId);

    /// <summary>Returns false if the player already exists.</summary>
    bool CreatePlayer(PlayerRecord player);

    /// <summary>All-or-nothing, and at most once per <paramref name="opId"/>.
    /// Also appends one audit-log entry in the same commit.</summary>
    ApplyResult Apply(string opId, string kind, IReadOnlyList<Change> changes);

    IReadOnlyList<LogEntry> Log { get; }

    void SaveSnapshot(PlayerSnapshot snapshot);
    PlayerSnapshot? LoadSnapshot(string playerId);

    SagaRecord? FindSaga(string sagaId);
    void SaveSaga(SagaRecord saga);
}
