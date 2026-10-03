namespace Course.Quests;

/// <summary>One private run of a mission: open, entered, running, ended, destroyed.
/// All time is measured in simulation ticks supplied by the caller.</summary>
public sealed class MissionInstance
{
    public enum Phase { Open, Running, Ended, Destroyed }

    private readonly List<PartyMember> _members = new();
    private int _kills;

    public MissionDef Def { get; }
    public Phase State { get; private set; } = Phase.Open;
    public MissionOutcome Outcome { get; private set; } = MissionOutcome.None;
    public long DeadlineTick { get; private set; }
    public int Score { get; private set; }
    public IReadOnlyList<PartyMember> Members => _members;

    public MissionInstance(MissionDef def) => Def = def;

    /// <summary>Entry rules are checked before any state changes, so a rejected party leaves no trace.
    /// A successful entry starts the clock.</summary>
    public EnterResult TryEnter(IReadOnlyList<PartyMember> party, long nowTick)
    {
        if (State != Phase.Open) return EnterResult.NotOpen;
        if (party.Count < Def.MinPlayers) return EnterResult.PartyTooSmall;
        if (party.Count > Def.MaxPlayers) return EnterResult.PartyTooLarge;
        if (party.Any(m => m.Level < Def.MinLevel)) return EnterResult.LevelTooLow;

        _members.AddRange(party);
        DeadlineTick = nowTick + Def.TimeLimitTicks;
        State = Phase.Running;
        return EnterResult.Entered;
    }

    public void RecordKill()
    {
        if (State == Phase.Running) _kills++;
    }

    /// <summary>The encounter was won. Faster clears score higher: one bonus point per 10 ticks left.</summary>
    public MissionResult? Complete(long nowTick)
    {
        if (State != Phase.Running) return null;
        var bonus = (int)Math.Max(0, DeadlineTick - nowTick) / 10;
        return End(MissionOutcome.Cleared, _kills * Def.PointsPerKill + bonus);
    }

    /// <summary>Called every tick by the manager. The mission ends on the first tick that
    /// reaches the deadline, not one later.</summary>
    public MissionResult? Tick(long nowTick)
    {
        if (State != Phase.Running || nowTick < DeadlineTick) return null;
        return End(MissionOutcome.TimedOut, _kills * Def.PointsPerKill);
    }

    /// <summary>Releases everything the run held. After this the instance can never be entered again.</summary>
    public void Destroy()
    {
        _members.Clear();
        State = Phase.Destroyed;
    }

    private MissionResult End(MissionOutcome outcome, int score)
    {
        State = Phase.Ended;
        Outcome = outcome;
        Score = score;
        var grade = score >= Def.GradeS ? 'S' : score >= Def.GradeA ? 'A' : 'B';
        return new MissionResult(Def.Id, outcome, score, grade);
    }
}
