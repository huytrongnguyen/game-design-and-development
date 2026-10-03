namespace Course.Skills;

/// <summary>Small builders shared by the tests.</summary>
internal static class TestKit
{
    public static Combatant Unit(string id, int x = 0, int y = 0, int atk = 100, int maxHp = 1000, int maxSp = 100) =>
        new(id, x, y, maxHp, maxSp, new Dictionary<Stat, int> { [Stat.Atk] = atk, [Stat.Def] = 10, [Stat.Speed] = 100 });

    public static SkillDefinition Skill(
        string id, int spCost = 40, int cooldown = 50, int cast = 0, int range = 500,
        TargetShape shape = TargetShape.Single, int radius = 0, params ISkillEffect[] effects) =>
        new(id, spCost, cooldown, cast, range, shape, radius, effects);

    public static BuffDefinition Buff(
        string id, int duration, StackingPolicy policy, int maxStacks = 1, bool debuff = false, bool dispellable = true,
        int period = 0, int periodicDamage = 0, params StatModifier[] modifiers) =>
        new(id, duration, policy, maxStacks, debuff, dispellable, modifiers, period, periodicDamage);
}
