namespace Course.Skills;

public class BuffContainerTests
{
    private static readonly StatModifier AtkUp10 = new(Stat.Atk, ModifierOp.PercentAdd, 10);

    [Fact]
    public void Apply_StackToMax_CapsAtMaxStacksAndScalesTheModifier()
    {
        var rage = TestKit.Buff("rage", 100, StackingPolicy.StackToMax, maxStacks: 3, modifiers: AtkUp10);
        var unit = TestKit.Unit("hero", atk: 100);

        for (var i = 0; i < 5; i++) unit.Buffs.Apply(rage, now: i);

        Assert.Equal(3, unit.Buffs.StacksOf("rage"));
        Assert.Equal(130, unit.Stat(Stat.Atk)); // 100 * (100 + 3*10) / 100
    }

    [Fact]
    public void Apply_Refresh_ResetsDurationWithoutAddingStacks()
    {
        var guard = TestKit.Buff("guard", 100, StackingPolicy.Refresh);
        var unit = TestKit.Unit("hero");
        unit.Buffs.Apply(guard, now: 0);

        unit.Buffs.Apply(guard, now: 60);   // new expiry: 160
        unit.Buffs.Tick(159);
        Assert.True(unit.Buffs.Has("guard"));
        Assert.Equal(1, unit.Buffs.StacksOf("guard"));

        unit.Buffs.Tick(160);
        Assert.False(unit.Buffs.Has("guard"));
    }

    [Fact]
    public void Apply_Independent_KeepsSeparateTimersAndReplacesTheOldestWhenFull()
    {
        var regen = TestKit.Buff("regen", 100, StackingPolicy.Independent, maxStacks: 2);
        var unit = TestKit.Unit("hero");

        unit.Buffs.Apply(regen, 0);    // expires 100
        unit.Buffs.Apply(regen, 50);   // expires 150
        unit.Buffs.Tick(100);
        Assert.Equal(1, unit.Buffs.StacksOf("regen"));

        unit.Buffs.Apply(regen, 100);  // expires 200; count back to 2
        unit.Buffs.Apply(regen, 120);  // full: replaces the one expiring at 150
        Assert.Equal(2, unit.Buffs.StacksOf("regen"));
        unit.Buffs.Tick(150);
        Assert.Equal(2, unit.Buffs.StacksOf("regen"));
    }

    [Fact]
    public void Tick_Dot_DealsTheExactTotalOverItsDuration()
    {
        var poison = TestKit.Buff("poison", 100, StackingPolicy.Refresh, debuff: true, period: 20, periodicDamage: 10);
        var victim = TestKit.Unit("dummy", maxHp: 200);
        victim.Buffs.Apply(poison, now: 0);

        for (var t = 1; t <= 200; t++) victim.Tick(t);

        Assert.Equal(200 - 50, victim.Hp); // ticks at 20, 40, 60, 80, 100
        Assert.False(victim.Buffs.Has("poison"));
    }

    [Fact]
    public void Tick_StackedDot_DamagePerPeriodScalesWithStacks()
    {
        var bleed = TestKit.Buff("bleed", 40, StackingPolicy.StackToMax, maxStacks: 5, debuff: true,
            period: 20, periodicDamage: 10);
        var victim = TestKit.Unit("dummy", maxHp: 500);
        victim.Buffs.Apply(bleed, 0);
        victim.Buffs.Apply(bleed, 0);   // 2 stacks: 20 per period

        for (var t = 1; t <= 40; t++) victim.Tick(t);

        Assert.Equal(500 - 40, victim.Hp); // ticks at 20 and 40, 20 each
    }

    [Fact]
    public void Tick_AtExpiry_RemovesTheModifierOnThatExactTick()
    {
        var might = TestKit.Buff("might", 30, StackingPolicy.Refresh,
            modifiers: new StatModifier(Stat.Atk, ModifierOp.Add, 50));
        var unit = TestKit.Unit("hero", atk: 100);
        unit.Buffs.Apply(might, now: 0);
        Assert.Equal(150, unit.Stat(Stat.Atk));

        unit.Tick(29);
        Assert.Equal(150, unit.Stat(Stat.Atk));
        unit.Tick(30);
        Assert.Equal(100, unit.Stat(Stat.Atk));
    }

    [Fact]
    public void Dispel_Debuffs_RemovesOnlyDispellableDebuffsUpToTheLimit()
    {
        var unit = TestKit.Unit("hero");
        unit.Buffs.Apply(TestKit.Buff("slow", 100, StackingPolicy.Refresh, debuff: true), 0);
        unit.Buffs.Apply(TestKit.Buff("curse", 100, StackingPolicy.Refresh, debuff: true, dispellable: false), 0);
        unit.Buffs.Apply(TestKit.Buff("fear", 100, StackingPolicy.Refresh, debuff: true), 0);
        unit.Buffs.Apply(TestKit.Buff("haste", 100, StackingPolicy.Refresh), 0);

        var removed = unit.Buffs.Dispel(debuffs: true, maxCount: 1);

        Assert.Equal(1, removed);
        Assert.False(unit.Buffs.Has("slow"));   // oldest dispellable debuff goes first
        Assert.True(unit.Buffs.Has("curse"));   // not dispellable
        Assert.True(unit.Buffs.Has("fear"));    // beyond the limit
        Assert.True(unit.Buffs.Has("haste"));   // wrong kind
    }

    [Fact]
    public void Stat_PercentAndFlatModifiers_CombineAsFlatThenPercent()
    {
        var unit = TestKit.Unit("hero", atk: 100);
        unit.Buffs.Apply(TestKit.Buff("a", 50, StackingPolicy.Refresh, modifiers: new StatModifier(Stat.Atk, ModifierOp.Add, 20)), 0);
        unit.Buffs.Apply(TestKit.Buff("b", 50, StackingPolicy.Refresh, modifiers: AtkUp10), 0);
        unit.Buffs.Apply(TestKit.Buff("c", 50, StackingPolicy.Refresh, modifiers: AtkUp10), 0);

        Assert.Equal(144, unit.Stat(Stat.Atk)); // (100 + 20) * (100 + 10 + 10) / 100
    }
}
