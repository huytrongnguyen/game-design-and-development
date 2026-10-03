namespace Course.Skills;

/// <summary>
/// The buffs on one unit: applies them under their stacking policy, ticks periodic effects,
/// expires them, dispels them, and exposes their stat modifiers.
/// </summary>
public sealed class BuffContainer
{
    private readonly List<BuffInstance> _active = [];

    public IReadOnlyList<BuffInstance> Active => _active;

    public bool Has(string buffId) => _active.Exists(b => b.Definition.Id == buffId);

    public int StacksOf(string buffId)
    {
        var total = 0;
        foreach (var b in _active)
            if (b.Definition.Id == buffId) total += b.Stacks;
        return total;
    }

    public void Apply(BuffDefinition def, int now)
    {
        var existing = _active.Find(b => b.Definition.Id == def.Id);
        switch (def.Policy)
        {
            case StackingPolicy.Refresh:
                if (existing is null) _active.Add(new BuffInstance(def, now));
                else existing.ExpiresAt = now + def.DurationTicks;
                break;

            case StackingPolicy.StackToMax:
                if (existing is null) _active.Add(new BuffInstance(def, now));
                else
                {
                    existing.Stacks = Math.Min(def.MaxStacks, existing.Stacks + 1);
                    existing.ExpiresAt = now + def.DurationTicks;
                }
                break;

            case StackingPolicy.Independent:
                var sameId = _active.FindAll(b => b.Definition.Id == def.Id);
                if (sameId.Count >= def.MaxStacks)
                    _active.Remove(sameId.MinBy(b => b.ExpiresAt)!);
                _active.Add(new BuffInstance(def, now));
                break;
        }
    }

    /// <summary>Advances to <paramref name="now"/>: fires due periodic effects, then removes expired buffs. Returns periodic damage dealt.</summary>
    public int Tick(int now)
    {
        var damage = 0;
        foreach (var b in _active)
        {
            var period = b.Definition.PeriodTicks;
            if (period <= 0) continue;
            while (b.NextPeriodAt <= now && b.NextPeriodAt <= b.ExpiresAt)
            {
                damage += b.Definition.PeriodicDamage * b.Stacks;
                b.NextPeriodAt += period;
            }
        }
        _active.RemoveAll(b => now >= b.ExpiresAt);
        return damage;
    }

    /// <summary>Removes up to <paramref name="maxCount"/> dispellable buffs of the requested kind, oldest first. Returns how many were removed.</summary>
    public int Dispel(bool debuffs, int maxCount)
    {
        var removed = 0;
        for (var i = 0; i < _active.Count && removed < maxCount;)
        {
            var d = _active[i].Definition;
            if (d.Dispellable && d.IsDebuff == debuffs)
            {
                _active.RemoveAt(i);
                removed++;
            }
            else i++;
        }
        return removed;
    }

    /// <summary>Appends every active modifier (repeated once per stack) to <paramref name="into"/>.</summary>
    public void CollectModifiers(List<StatModifier> into)
    {
        foreach (var b in _active)
            for (var s = 0; s < b.Stacks; s++)
                into.AddRange(b.Definition.Modifiers);
    }
}
