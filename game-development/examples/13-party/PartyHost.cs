namespace Course.PartyControl;

/// <summary>
/// The server side of one zone: one Party per connected player. Commands are looked up by the
/// authenticated player id, then validated by the Party's own rules.
/// </summary>
public sealed class PartyHost
{
    private readonly Dictionary<int, Party> _parties = [];
    private readonly List<SkillCast> _casts = [];

    public IReadOnlyList<SkillCast> PendingCasts => _casts;

    public void Add(Party party) => _parties[party.OwnerId] = party;

    /// <summary>Every entity the zone simulates and broadcasts: one per character plus the pet, per player.</summary>
    public IEnumerable<Unit> Entities() => _parties.Values.SelectMany(f => f.Units);

    public CommandResult Apply(int playerId, PartyCommand command)
    {
        if (!_parties.TryGetValue(playerId, out var party))
        {
            return CommandResult.UnknownPlayer;
        }
        bool ok = true;
        switch (command.Kind)
        {
            case CommandKind.Move:
                party.Move(command.Point);
                break;
            case CommandKind.SwitchLeader:
                ok = party.TrySwitchLeader(command.Number);
                break;
            case CommandKind.SkillKey:
                ok = party.TryUseSkill(command.Key, out var cast);
                if (ok)
                {
                    _casts.Add(cast);
                }
                break;
            case CommandKind.SaveSquad:
                ok = party.TrySaveSquad(command.Number);
                break;
            case CommandKind.RecallSquad:
                ok = party.TryRecallSquad(command.Number);
                break;
            case CommandKind.Regroup:
                party.Regroup();
                break;
        }
        return ok ? CommandResult.Ok : CommandResult.Rejected;
    }

    public void Tick(double dt)
    {
        foreach (var party in _parties.Values)
        {
            party.Tick(dt);
        }
    }
}
