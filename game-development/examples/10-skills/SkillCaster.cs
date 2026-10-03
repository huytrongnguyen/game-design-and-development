namespace Course.Skills;

/// <summary>
/// Owns a unit's SP, known skills and cooldowns, and runs the cast lifecycle:
/// request -> validate -> (cast time) -> apply effects -> cooldown. All time is in ticks.
/// </summary>
public sealed class SkillCaster
{
    private sealed record Known(SkillDefinition Definition, int Level);

    private sealed record PendingCast(Known Skill, Combatant? Target, IReadOnlyList<Combatant> World, int FinishAt);

    private readonly Combatant _owner;
    private readonly Dictionary<string, Known> _known = [];
    private readonly Dictionary<string, int> _readyAt = [];
    private PendingCast? _pending;

    public SkillCaster(Combatant owner, int maxSp)
    {
        _owner = owner;
        MaxSp = maxSp;
        Sp = maxSp;
    }

    public int Sp { get; private set; }
    public int MaxSp { get; }
    public bool IsCasting => _pending is not null;

    public void Learn(SkillDefinition skill, int level) => _known[skill.Id] = new Known(skill, level);

    public void AddSp(int delta) => Sp = Math.Clamp(Sp + delta, 0, MaxSp);

    public int CooldownRemaining(string skillId, int now) =>
        _readyAt.TryGetValue(skillId, out var ready) ? Math.Max(0, ready - now) : 0;

    /// <summary>
    /// Validates the request. If it passes, pays SP, starts the cooldown, and either applies the effects
    /// at once (cast time 0) or starts the cast bar.
    /// </summary>
    public CastStatus TryBegin(string skillId, Combatant? target, IReadOnlyList<Combatant> world, int now)
    {
        if (!_known.TryGetValue(skillId, out var skill)) return CastStatus.UnknownSkill;
        var def = skill.Definition;
        if (_owner.Stance is not null && !_owner.Stance.SkillIds.Contains(skillId)) return CastStatus.NotInStance;
        if (_pending is not null) return CastStatus.AlreadyCasting;
        if (CooldownRemaining(skillId, now) > 0) return CastStatus.OnCooldown;
        if (Sp < def.SpCost) return CastStatus.NotEnoughSp;
        if (def.Shape != TargetShape.Self)
        {
            if (target is null) return CastStatus.NoTarget;
            var dx = (long)(target.X - _owner.X);
            var dy = (long)(target.Y - _owner.Y);
            if (dx * dx + dy * dy > (long)def.Range * def.Range) return CastStatus.OutOfRange;
        }

        Sp -= def.SpCost;
        _readyAt[skillId] = now + def.CooldownTicks;
        if (def.CastTicks == 0) Resolve(skill, target, world, now);
        else _pending = new PendingCast(skill, target, world, now + def.CastTicks);
        return CastStatus.Started;
    }

    /// <summary>Cancels a cast in progress. SP and cooldown are not refunded.</summary>
    public void Interrupt() => _pending = null;

    public void Tick(int now)
    {
        if (_pending is { } p && now >= p.FinishAt)
        {
            _pending = null;
            Resolve(p.Skill, p.Target, p.World, now);
        }
    }

    private void Resolve(Known skill, Combatant? target, IReadOnlyList<Combatant> world, int now)
    {
        var def = skill.Definition;
        foreach (var hit in SelectTargets(def, target, world))
            foreach (var effect in def.Effects)
                effect.Apply(_owner, skill.Level, hit, now);
    }

    private IEnumerable<Combatant> SelectTargets(SkillDefinition def, Combatant? target, IReadOnlyList<Combatant> world)
    {
        switch (def.Shape)
        {
            case TargetShape.Self:
                yield return _owner;
                break;
            case TargetShape.Single:
                yield return target!;
                break;
            case TargetShape.Circle:
                foreach (var c in world)
                {
                    var dx = (long)(c.X - target!.X);
                    var dy = (long)(c.Y - target.Y);
                    if (dx * dx + dy * dy <= (long)def.Radius * def.Radius) yield return c;
                }
                break;
        }
    }
}
