namespace Course.Quests;

/// <summary>One player's quest state: a state per quest plus per-objective counters.
/// This is the only mutable quest data; definitions stay in the catalog.</summary>
public sealed class QuestLog
{
    private readonly QuestCatalog _catalog;
    private readonly Dictionary<string, QuestState> _state = new();
    private readonly Dictionary<string, int[]> _progress = new();
    private readonly List<string> _triggers = new();

    public int Level { get; private set; }

    public QuestLog(QuestCatalog catalog, int level)
    {
        _catalog = catalog;
        Level = level;
        foreach (var q in catalog.All) _state[q.Id] = QuestState.Locked;
        Refresh();
    }

    public QuestState StateOf(string questId) => _state[questId];

    public int ProgressOf(string questId, string objectiveId)
    {
        var def = _catalog.Get(questId);
        var index = def.Objectives.ToList().FindIndex(o => o.Id == objectiveId);
        return _progress.TryGetValue(questId, out var p) ? p[index] : 0;
    }

    public void SetLevel(int level)
    {
        Level = level;
        Refresh();
    }

    /// <summary>Locked becomes Available when every prerequisite is Rewarded and the level is met.
    /// Called after anything that could change the answer (reward, level-up).</summary>
    public void Refresh()
    {
        foreach (var q in _catalog.All)
        {
            if (_state[q.Id] != QuestState.Locked) continue;
            var open = Level >= q.MinLevel
                && (q.Prerequisites ?? []).All(p => _state[p] == QuestState.Rewarded);
            if (open) _state[q.Id] = QuestState.Available;
        }
    }

    public bool TryAccept(string questId)
    {
        if (_state[questId] != QuestState.Available) return false;
        var def = _catalog.Get(questId);
        _state[questId] = QuestState.Active;
        _progress[questId] = new int[def.Objectives.Count];
        if (def.OnAcceptDialogue is not null) _triggers.Add($"dialogue:{def.OnAcceptDialogue}");
        return true;
    }

    /// <summary>Feeds one game event to every active quest. Only objectives with the same
    /// kind and target move; a quest completes when every objective reaches its count.</summary>
    public void Apply(GameEvent e)
    {
        foreach (var def in _catalog.All)
        {
            if (_state[def.Id] != QuestState.Active) continue;
            var counters = _progress[def.Id];
            for (var i = 0; i < def.Objectives.Count; i++)
            {
                var o = def.Objectives[i];
                if (o.Kind == e.Kind && o.Target == e.Target)
                    counters[i] = Math.Min(o.Count, counters[i] + e.Amount);
            }

            var done = true;
            for (var i = 0; i < counters.Length; i++)
                done &= counters[i] >= def.Objectives[i].Count;
            if (!done) continue;

            _state[def.Id] = QuestState.Completed;
            if (def.OnCompleteCutscene is not null) _triggers.Add($"cutscene:{def.OnCompleteCutscene}");
        }
    }

    /// <summary>Turn-in. Returns the reward exactly once; a second call returns null.</summary>
    public RewardDef? TryClaim(string questId)
    {
        if (_state[questId] != QuestState.Completed) return null;
        _state[questId] = QuestState.Rewarded;
        Refresh();
        return _catalog.Get(questId).Reward;
    }

    /// <summary>Hands dialogue and cutscene ids to the presentation layer and clears the list.</summary>
    public IReadOnlyList<string> DrainTriggers()
    {
        var copy = _triggers.ToList();
        _triggers.Clear();
        return copy;
    }
}
