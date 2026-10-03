namespace Course.Skills;

/// <summary>Turns a base value plus a pile of modifiers into the final stat: (base + sum(Add)) * (100 + sum(PercentAdd)) / 100.</summary>
public static class StatResolver
{
    public static int Resolve(int baseValue, Stat stat, IReadOnlyList<StatModifier> modifiers)
    {
        var flat = 0;
        var percent = 0;
        foreach (var m in modifiers)
        {
            if (m.Stat != stat) continue;
            if (m.Op == ModifierOp.Add) flat += m.Value;
            else percent += m.Value;
        }
        return (baseValue + flat) * (100 + percent) / 100;
    }
}
