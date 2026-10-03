namespace Course.Matchmaking;

/// <summary>The queue service. Everything it needs to decide is passed in (rules, the current tick),
/// so a test can replay any scenario tick by tick.</summary>
public sealed class Matchmaker
{
    private readonly QueueRules _rules;
    private readonly InstanceAllocator _instances;
    private readonly LeaverTracker _leavers;
    private readonly List<QueueEntry> _queue = new();
    private readonly List<Party> _awaitingInstance = new();
    private readonly List<Backfill> _backfills = new();
    private readonly Dictionary<string, Party> _partyOf = new();
    private int _nextParty = 1;

    private sealed record Backfill(Party Party, Role Role, long RequestedTick);

    public Matchmaker(QueueRules rules, InstanceAllocator instances)
    {
        _rules = rules;
        _instances = instances;
        _leavers = new LeaverTracker(rules.LeaverCooldownTicks);
    }

    public int QueueLength => _queue.Count;
    public int OpenBackfills => _backfills.Count;
    public LeaverTracker Leavers => _leavers;

    public EnqueueResult Enqueue(QueueEntry e)
    {
        if (!_leavers.CanQueue(e.PlayerId, e.EnqueuedTick)) return EnqueueResult.LockedOut;
        if (_queue.Any(q => q.PlayerId == e.PlayerId) || _partyOf.ContainsKey(e.PlayerId)) return EnqueueResult.AlreadyQueued;
        _queue.Add(e);
        return EnqueueResult.Queued;
    }

    public bool Cancel(string playerId) => _queue.RemoveAll(q => q.PlayerId == playerId) > 0;

    /// <summary>One matchmaking pass. Returns the parties formed this tick.</summary>
    public List<Party> Tick(long now)
    {
        var formed = new List<Party>();

        // 1. Parties still waiting for an instance slot try again.
        foreach (var p in _awaitingInstance.ToList())
            if (_instances.TryAllocate() is { } id) { p.InstanceId = id; _awaitingInstance.Remove(p); }

        // 2. Backfills come first: a half-played run is worth more than a new one.
        foreach (var bf in _backfills.ToList())
        {
            var anchorLevel = (int)Math.Round(bf.Party.Members.Average(m => m.Level));
            var bracket = _rules.BracketAt(now - bf.RequestedTick);
            var pick = _queue.Where(q => q.Role == bf.Role && Math.Abs(q.Level - anchorLevel) <= bracket
                                          && ShardOk(bf.Party.Members[0], q, now - q.EnqueuedTick))
                             .OrderBy(q => q.EnqueuedTick).ThenBy(q => q.PlayerId).FirstOrDefault();
            if (pick == null) continue;
            _queue.Remove(pick);
            bf.Party.Members.Add(pick);
            _partyOf[pick.PlayerId] = bf.Party;
            _backfills.Remove(bf);
        }

        // 3. New parties, oldest waiter first.
        bool progress;
        do
        {
            progress = false;
            foreach (var anchor in _queue.OrderBy(q => q.EnqueuedTick).ThenBy(q => q.PlayerId))
            {
                var members = TryForm(anchor, now);
                if (members == null) continue;
                foreach (var m in members) _queue.Remove(m);
                var party = new Party($"party-{_nextParty++}", members);
                foreach (var m in members) _partyOf[m.PlayerId] = party;
                if (_instances.TryAllocate() is { } id) party.InstanceId = id; else _awaitingInstance.Add(party);
                formed.Add(party);
                progress = true;
                break;
            }
        } while (progress);

        return formed;
    }

    /// <summary>A member leaves. After the run started this is a strike plus a backfill request;
    /// before it started the rest of the party simply goes back to the queue with their old wait.</summary>
    public void Leave(string playerId, long now)
    {
        if (!_partyOf.Remove(playerId, out var party)) return;
        var me = party.Members.First(m => m.PlayerId == playerId);
        party.Members.Remove(me);

        if (party.Started)
        {
            _leavers.Strike(playerId, now);
            if (party.Members.Count == 0) Disband(party);
            else _backfills.Add(new Backfill(party, me.Role, now));
        }
        else
        {
            _awaitingInstance.Remove(party);
            foreach (var m in party.Members) { _partyOf.Remove(m.PlayerId); _queue.Add(m); }
            party.Members.Clear();
        }
    }

    /// <summary>The run ended normally: free the slot and forget the party.</summary>
    public void Finish(Party party)
    {
        Disband(party);
    }

    private void Disband(Party party)
    {
        foreach (var m in party.Members) _partyOf.Remove(m.PlayerId);
        party.Members.Clear();
        _backfills.RemoveAll(b => b.Party == party);
        if (party.InstanceId != null) { _instances.Release(party.InstanceId); party.InstanceId = null; }
    }

    private bool ShardOk(QueueEntry anchor, QueueEntry other, long otherWait) =>
        anchor.Shard == other.Shard || _rules.CrossShardAllowed(otherWait);

    private List<QueueEntry>? TryForm(QueueEntry anchor, long now)
    {
        var wait = now - anchor.EnqueuedTick;
        var bracket = _rules.BracketAt(wait);
        var pool = _queue.Where(q => Math.Abs(q.Level - anchor.Level) <= bracket
                                     && (q == anchor || ShardOk(anchor, q, wait)))
                         .OrderBy(q => q.EnqueuedTick).ThenBy(q => q.PlayerId).ToList();
        var chosen = new List<QueueEntry>();
        foreach (var role in Enum.GetValues<Role>())
        {
            var need = _rules.Need(role);
            var ofRole = pool.Where(q => q.Role == role).ToList();
            if (anchor.Role == role) { ofRole.Remove(anchor); ofRole.Insert(0, anchor); }
            if (ofRole.Count < need) return null;
            chosen.AddRange(ofRole.Take(need));
        }
        return chosen;
    }
}
