namespace Course.Skills;

/// <summary>A unit in the fight: HP, position, base stats, a stance, buffs and a skill caster.</summary>
public sealed class Combatant
{
    /// <summary>Ticks between SP regen/drain steps from the active stance.</summary>
    public const int RegenIntervalTicks = 60;

    private readonly Dictionary<Stat, int> _baseStats;
    private readonly List<StatModifier> _scratch = [];

    public Combatant(string id, int x, int y, int maxHp, int maxSp, IReadOnlyDictionary<Stat, int> baseStats)
    {
        Id = id;
        X = x;
        Y = y;
        MaxHp = maxHp;
        Hp = maxHp;
        _baseStats = new Dictionary<Stat, int>(baseStats);
        Caster = new SkillCaster(this, maxSp);
    }

    public string Id { get; }
    public int X { get; }
    public int Y { get; }
    public int Hp { get; private set; }
    public int MaxHp { get; }
    public Stance? Stance { get; private set; }
    public BuffContainer Buffs { get; } = new();
    public SkillCaster Caster { get; }

    public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);

    /// <summary>Switching stance changes the usable skills and cancels a cast in progress.</summary>
    public void SwitchStance(Stance stance)
    {
        Stance = stance;
        Caster.Interrupt();
    }

    /// <summary>The final value of a stat: base, then stance and buff modifiers. No code here knows a buff's name.</summary>
    public int Stat(Stat stat)
    {
        _scratch.Clear();
        if (Stance is not null) _scratch.AddRange(Stance.Modifiers);
        Buffs.CollectModifiers(_scratch);
        _baseStats.TryGetValue(stat, out var baseValue);
        return StatResolver.Resolve(baseValue, stat, _scratch);
    }

    /// <summary>One simulation step: periodic buff effects, stance SP flow, then cast completion.</summary>
    public void Tick(int now)
    {
        TakeDamage(Buffs.Tick(now));
        if (Stance is not null && now > 0 && now % RegenIntervalTicks == 0)
            Caster.AddSp(Stance.SpPerInterval);
        Caster.Tick(now);
    }
}
