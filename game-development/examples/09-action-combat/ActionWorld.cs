namespace Course.ActionCombat;

public enum HitOutcome { Hit, Dodged }

public sealed record HitEvent(int Tick, string AttackerId, string TargetId, string SkillId, int Damage, HitOutcome Outcome);

/// <summary>
/// A tiny authoritative action-combat simulation. Order inside <see cref="Step"/>:
/// movement from skills, body separation, record positions, hit tests (with rewind), then advance the tick.
/// </summary>
public sealed class ActionWorld(ActionRules rules)
{
    private readonly List<Combatant> _combatants = [];
    private readonly Dictionary<string, PositionHistory> _history = [];

    public int Tick { get; private set; }
    public List<HitEvent> Events { get; } = [];

    public Combatant Add(string id, int team, Vec2 position, double radius, int hp)
    {
        var c = new Combatant(id, team, position, radius, hp);
        _combatants.Add(c);
        _history[id] = new PositionHistory(rules.HistoryCapacity);
        return c;
    }

    /// <summary>Starts a skill if the combatant is idle or inside the running skill's cancel window.</summary>
    public bool TryStart(Combatant c, SkillDef skill)
    {
        if (c.Action is not null && !c.Action.CanCancelAt(Tick - c.ActionStartTick)) return false;
        c.Action = skill;
        c.ActionStartTick = Tick;
        return true;
    }

    public bool IsInvulnerable(Combatant c) =>
        c.Action is not null && c.Action.IsInvulnerableAt(Tick - c.ActionStartTick);

    public void Step()
    {
        // 1. Movement driven by skill timelines.
        foreach (var c in _combatants.Where(c => c.Action is not null))
        {
            int offset = Tick - c.ActionStartTick;
            foreach (var m in c.Action!.Moves)
                if (offset >= m.FromTick && offset < m.ToTick)
                    c.Position += Vec2.FromAngle(c.FacingDeg) * m.DistancePerTick;
        }

        // 2. Body collision: push overlapping circles apart, half each.
        for (int i = 0; i < _combatants.Count; i++)
            for (int j = i + 1; j < _combatants.Count; j++)
                Separate(_combatants[i], _combatants[j]);

        // 3. Record this tick's positions for later rewinds.
        foreach (var c in _combatants) _history[c.Id].Record(Tick, c.Position);

        // 4. Hit tests.
        foreach (var attacker in _combatants.Where(c => c.Action is not null))
        {
            var skill = attacker.Action!;
            if (skill.Shape is null || Array.IndexOf(skill.HitTicks, Tick - attacker.ActionStartTick) < 0) continue;
            int viewTick = Tick - Math.Min(attacker.ViewLagTicks, rules.MaxRewindTicks);
            foreach (var target in _combatants.Where(t => t.Team != attacker.Team && t.Hp > 0))
            {
                // Rewound position for the geometry test; fall back to the current one if history is gone.
                Vec2 seen = _history[target.Id].TryGet(viewTick, out var old) ? old : target.Position;
                if (!ShapeOverlap.Hits(skill.Shape, attacker.Position, attacker.FacingDeg, seen, target.Radius)) continue;

                // Invulnerability is judged at server time, not at the rewound time.
                if (IsInvulnerable(target))
                {
                    Events.Add(new(Tick, attacker.Id, target.Id, skill.Id, 0, HitOutcome.Dodged));
                    continue;
                }
                target.Hp -= skill.Damage;
                Events.Add(new(Tick, attacker.Id, target.Id, skill.Id, skill.Damage, HitOutcome.Hit));
            }
        }

        // 5. Finish skills whose timeline is over, then advance.
        foreach (var c in _combatants.Where(c => c.Action is not null))
            if (Tick + 1 - c.ActionStartTick >= c.Action!.TotalTicks) c.Action = null;
        Tick++;
    }

    /// <summary>Test and tool hook: place a combatant (for example to script a target's path).</summary>
    public void SetPosition(Combatant c, Vec2 p) => c.Position = p;

    private static void Separate(Combatant a, Combatant b)
    {
        Vec2 d = b.Position - a.Position;
        double dist = d.Length, min = a.Radius + b.Radius;
        if (dist >= min) return;
        Vec2 n = dist < 1e-9 ? new Vec2(1, 0) : d * (1.0 / dist);
        double push = (min - dist) / 2;
        a.Position -= n * push;
        b.Position += n * push;
    }
}
