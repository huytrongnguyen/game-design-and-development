namespace M12.Ai;

/// <summary>
/// Idle, Chase, Attack, Return finite state machine with a hate table.
/// Decisions happen on think ticks; movement integrates on every tick.
/// </summary>
public sealed class Monster
{
    private readonly MonsterConfig _cfg;
    private readonly int _thinkOffset;
    private Unit? _target;
    private long _nextAttackTick;

    public Monster(int id, Vec2 home, MonsterConfig cfg, int thinkOffset = 0)
    {
        Id = id;
        Home = home;
        Position = home;
        _cfg = cfg;
        _thinkOffset = thinkOffset;
        Hp = cfg.MaxHp;
    }

    public int Id { get; }
    public Vec2 Home { get; }
    public Vec2 Position { get; private set; }
    public int Hp { get; private set; }
    public MonsterState State { get; private set; } = MonsterState.Idle;
    public HateTable Hate { get; } = new();
    public int? TargetId => _target?.Id;
    public int ThinkCount { get; private set; }

    public void Tick(long tick, IReadOnlyList<Unit> units)
    {
        // The offset staggers monsters so their think work spreads over the interval.
        if ((tick + _thinkOffset) % _cfg.ThinkIntervalTicks == 0)
            Think(tick, units);
        Move();
    }

    /// <summary>A unit hit this monster. Ignored while returning (the monster is evading).</summary>
    public void OnDamaged(int attackerId, int damage)
    {
        if (State == MonsterState.Return) return;
        Hp = Math.Max(0, Hp - damage);
        Hate.AddDamage(attackerId, damage);
    }

    /// <summary>A unit healed someone this monster is fighting.</summary>
    public void OnAllyHealed(int healerId, int healed)
    {
        if (State == MonsterState.Return) return;
        Hate.AddHeal(healerId, healed);
    }

    private void Think(long tick, IReadOnlyList<Unit> units)
    {
        ThinkCount++;
        switch (State)
        {
            case MonsterState.Idle:
                ThinkIdle(units);
                break;
            case MonsterState.Chase:
            case MonsterState.Attack:
                ThinkEngaged(tick, units);
                break;
            case MonsterState.Return:
                break; // walking home: ignores everything
        }
    }

    private void ThinkIdle(IReadOnlyList<Unit> units)
    {
        // Hit while idle counts as aggro even from outside the radius.
        if (Hate.Count == 0)
        {
            Unit? nearest = null;
            var best = _cfg.AggroRadius;
            foreach (var u in units)
            {
                if (!u.IsAlive) continue;
                var d = Position.DistanceTo(u.Position);
                if (d <= best) { best = d; nearest = u; }
            }
            if (nearest is null) return;
            Hate.Add(nearest.Id, _cfg.InitialHate);
        }
        State = MonsterState.Chase;
    }

    private void ThinkEngaged(long tick, IReadOnlyList<Unit> units)
    {
        if (Position.DistanceTo(Home) > _cfg.LeashDistance)
        {
            EnterReturn();
            return;
        }

        foreach (var u in units)
            if (!u.IsAlive) Hate.Remove(u.Id);
        Hate.Decay(_cfg.HateDecayPerThink);

        var targetId = Hate.SelectTarget(tick, TargetId);
        _target = null;
        if (targetId is int id)
            foreach (var u in units)
                if (u.Id == id) { _target = u; break; }
        if (_target is null)
        {
            EnterReturn();
            return;
        }

        var dist = Position.DistanceTo(_target.Position);
        State = dist <= _cfg.AttackRange ? MonsterState.Attack : MonsterState.Chase;
        if (State == MonsterState.Attack && tick >= _nextAttackTick)
        {
            _target.TakeDamage(_cfg.AttackDamage);
            _nextAttackTick = tick + _cfg.AttackCooldownTicks;
        }
    }

    private void EnterReturn()
    {
        State = MonsterState.Return;
        Hate.Clear();
        _target = null;
    }

    private void Move()
    {
        if (State == MonsterState.Chase && _target is not null)
        {
            var gap = Position.DistanceTo(_target.Position) - _cfg.AttackRange;
            if (gap > 0)
                Position = Position.MoveToward(_target.Position, Math.Min(_cfg.SpeedPerTick, gap));
        }
        else if (State == MonsterState.Return)
        {
            Position = Position.MoveToward(Home, _cfg.SpeedPerTick);
            if (Position == Home)
            {
                State = MonsterState.Idle;
                Hp = _cfg.MaxHp; // a full reset stops leash-and-heal exploits being free
            }
        }
    }
}
