namespace Course.Topology;

/// <summary>
/// Routes characters to zone hosts and is the single place that knows where every character is.
/// Presence lives here (not in the zones) so that "never in two zones" is one invariant in one class.
/// </summary>
public sealed class ZoneDirectory
{
    private sealed record PendingTransfer(CharacterState Snapshot, string ToZone, long ExpiresAtSeconds);

    private readonly Dictionary<string, IZoneHost> _hosts = new();
    private readonly Dictionary<int, string> _inZone = new();
    private readonly Dictionary<string, PendingTransfer> _pending = new();
    private readonly CharacterStore _store;
    private readonly IClock _clock;
    private readonly Random _rng; // seeded in tests; use RandomNumberGenerator for real ticket ids
    private readonly long _ticketTtlSeconds;

    public ZoneDirectory(CharacterStore store, IClock clock, Random rng, long ticketTtlSeconds)
    {
        _store = store;
        _clock = clock;
        _rng = rng;
        _ticketTtlSeconds = ticketTtlSeconds;
    }

    public void Register(IZoneHost host) => _hosts[host.Name] = host;

    /// <summary>The zone a character is in, or null if offline or in transit.</summary>
    public string? LocationOf(int characterId) => _inZone.GetValueOrDefault(characterId);

    public bool IsInTransit(int characterId) =>
        _pending.Values.Any(p => p.Snapshot.Id == characterId);

    /// <summary>Put a stored character into the zone saved in its record.</summary>
    public EnterResult Enter(int characterId)
    {
        var record = _store.Get(characterId);
        if (record is null) return new EnterResult(EnterStatus.UnknownCharacter, null);
        if (_inZone.ContainsKey(characterId) || IsInTransit(characterId))
            return new EnterResult(EnterStatus.AlreadyOnline, null);
        if (!_hosts.TryGetValue(record.Zone, out var host))
            return new EnterResult(EnterStatus.UnknownZone, null);
        if (host.IsFull) return new EnterResult(EnterStatus.ZoneFull, null);

        host.Admit(record);
        _inZone[characterId] = host.Name;
        return new EnterResult(EnterStatus.Ok, host.Name);
    }

    /// <summary>Leave the world: checkpoint the live state to the store and clear presence.</summary>
    public bool Logout(int characterId)
    {
        if (!_inZone.Remove(characterId, out var zone)) return false;
        var state = _hosts[zone].Remove(characterId)!;
        _store.Save(state);
        return true;
    }

    /// <summary>
    /// Step 1 of a zone change: detach the character from its zone, checkpoint it, and hold the
    /// snapshot under a one-time ticket. Until the ticket is redeemed the character is in no zone.
    /// </summary>
    public TransferResult BeginTransfer(int characterId, string toZone)
    {
        if (!_inZone.TryGetValue(characterId, out var from)) return Fail(TransferStatus.NotInZone);
        if (!_hosts.TryGetValue(toZone, out var dest)) return Fail(TransferStatus.UnknownZone);
        if (from == toZone) return Fail(TransferStatus.SameZone);
        if (dest.IsFull) return Fail(TransferStatus.ZoneFull);

        var snapshot = _hosts[from].Remove(characterId)!;
        _inZone.Remove(characterId);
        _store.Save(snapshot);

        var ticket = new TransferTicket(NewTicketId());
        _pending[ticket.Id] = new PendingTransfer(snapshot, toZone, _clock.NowSeconds + _ticketTtlSeconds);
        return new TransferResult(TransferStatus.Ok, ticket);
    }

    /// <summary>Step 2: redeem the ticket at the destination. A ticket works once.</summary>
    public TransferResult CompleteTransfer(TransferTicket ticket)
    {
        if (!_pending.TryGetValue(ticket.Id, out var pending)) return Fail(TransferStatus.InvalidTicket);

        if (_clock.NowSeconds >= pending.ExpiresAtSeconds)
        {
            _pending.Remove(ticket.Id); // character stays checkpointed in its source zone, offline
            return Fail(TransferStatus.Expired);
        }

        var dest = _hosts[pending.ToZone];
        if (dest.IsFull) return Fail(TransferStatus.ZoneFull); // ticket stays valid for a retry

        _pending.Remove(ticket.Id); // consumed
        var arriving = pending.Snapshot with { Zone = dest.Name, X = dest.Spawn.X, Y = dest.Spawn.Y };
        dest.Admit(arriving);
        _inZone[arriving.Id] = dest.Name;
        _store.Save(arriving);
        return new TransferResult(TransferStatus.Ok, null);
    }

    /// <summary>Drop every ticket past its deadline. Returns how many were dropped.</summary>
    public int ExpireTransfers()
    {
        var dead = _pending.Where(p => _clock.NowSeconds >= p.Value.ExpiresAtSeconds).Select(p => p.Key).ToList();
        foreach (var id in dead) _pending.Remove(id);
        return dead.Count;
    }

    private static TransferResult Fail(TransferStatus status) => new(status, null);

    private string NewTicketId()
    {
        var bytes = new byte[16];
        _rng.NextBytes(bytes);
        return Convert.ToHexString(bytes);
    }
}
