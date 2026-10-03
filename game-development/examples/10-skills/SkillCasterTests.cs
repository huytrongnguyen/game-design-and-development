namespace Course.Skills;

public class SkillCasterTests
{
    private static readonly SkillDefinition Strike = TestKit.Skill("strike", spCost: 40, cooldown: 50,
        effects: new DamageEffect(200, 15));

    private static (Combatant Caster, Combatant Target, List<Combatant> World) Setup(SkillDefinition skill, int level = 1)
    {
        var caster = TestKit.Unit("hero", atk: 100);
        var target = TestKit.Unit("dummy", x: 100);
        caster.Caster.Learn(skill, level);
        return (caster, target, [caster, target]);
    }

    [Fact]
    public void TryBegin_BeforeCooldownEnds_IsRejectedUntilTheExactTick()
    {
        var (caster, target, world) = Setup(Strike);
        Assert.Equal(CastStatus.Started, caster.Caster.TryBegin("strike", target, world, now: 100));
        caster.Caster.AddSp(100);

        Assert.Equal(CastStatus.OnCooldown, caster.Caster.TryBegin("strike", target, world, now: 149));
        Assert.Equal(1, caster.Caster.CooldownRemaining("strike", 149));
        Assert.Equal(CastStatus.Started, caster.Caster.TryBegin("strike", target, world, now: 150));
    }

    [Fact]
    public void TryBegin_NotEnoughSp_IsRejectedAndSpUnchanged()
    {
        var (caster, target, world) = Setup(Strike);
        caster.Caster.AddSp(-70); // 30 left, skill costs 40

        Assert.Equal(CastStatus.NotEnoughSp, caster.Caster.TryBegin("strike", target, world, 0));
        Assert.Equal(30, caster.Caster.Sp);
        Assert.Equal(0, caster.Caster.CooldownRemaining("strike", 0));
    }

    [Fact]
    public void TryBegin_ExactlyEnoughSp_SpendsItAll()
    {
        var (caster, target, world) = Setup(Strike);
        caster.Caster.AddSp(-60); // exactly 40

        Assert.Equal(CastStatus.Started, caster.Caster.TryBegin("strike", target, world, 0));
        Assert.Equal(0, caster.Caster.Sp);
    }

    [Fact]
    public void TryBegin_TargetBeyondRange_IsRejectedWithoutCost()
    {
        var skill = TestKit.Skill("short", range: 99, effects: new DamageEffect(100));
        var (caster, target, world) = Setup(skill); // target is 100 away

        Assert.Equal(CastStatus.OutOfRange, caster.Caster.TryBegin("short", target, world, 0));
        Assert.Equal(100, caster.Caster.Sp);
    }

    [Theory]
    [InlineData(1, 230)]   // 100 atk * 2.0 * (100 + 1*15)/100
    [InlineData(11, 530)]  // 100 atk * 2.0 * (100 + 11*15)/100
    public void DamageEffect_ScalesWithSkillLevel(int level, int expectedDamage)
    {
        var (caster, target, world) = Setup(Strike, level);

        caster.Caster.TryBegin("strike", target, world, 0);

        Assert.Equal(1000 - expectedDamage, target.Hp);
    }

    [Fact]
    public void TryBegin_WithCastTime_AppliesEffectsOnlyWhenTheCastCompletes()
    {
        var skill = TestKit.Skill("bolt", cast: 20, effects: new DamageEffect(200));
        var (caster, target, world) = Setup(skill);

        Assert.Equal(CastStatus.Started, caster.Caster.TryBegin("bolt", target, world, now: 10));
        caster.Tick(29);
        Assert.Equal(1000, target.Hp);
        Assert.True(caster.Caster.IsCasting);
        caster.Tick(30);

        Assert.Equal(1000 - 220, target.Hp); // 100 * 2.0 * 1.1
        Assert.False(caster.Caster.IsCasting);
    }

    [Fact]
    public void Interrupt_DuringCast_CancelsEffectsAndKeepsTheSpentSp()
    {
        var skill = TestKit.Skill("bolt", cast: 20, effects: new DamageEffect(200));
        var (caster, target, world) = Setup(skill);
        caster.Caster.TryBegin("bolt", target, world, 0);

        caster.Caster.Interrupt();
        caster.Tick(20);

        Assert.Equal(1000, target.Hp);
        Assert.Equal(60, caster.Caster.Sp);
    }

    [Fact]
    public void TryBegin_CircleShape_HitsOnlyUnitsWithinRadiusOfTheTarget()
    {
        var skill = TestKit.Skill("blast", range: 2000, shape: TargetShape.Circle, radius: 50, effects: new DamageEffect(100));
        var caster = TestKit.Unit("hero", atk: 100, x: -1000);
        var a = TestKit.Unit("a", x: 100);
        var b = TestKit.Unit("b", x: 150);   // 50 from a: inside
        var c = TestKit.Unit("c", x: 151);   // 51 from a: outside
        caster.Caster.Learn(skill, 1);

        caster.Caster.TryBegin("blast", a, [caster, a, b, c], 0);

        Assert.Equal(1000 - 110, a.Hp);
        Assert.Equal(1000 - 110, b.Hp);
        Assert.Equal(1000, c.Hp);
        Assert.Equal(1000, caster.Hp);
    }

    [Fact]
    public void TryBegin_SkillWithBuffEffect_AppliesTheBuffToTheTarget()
    {
        var weaken = TestKit.Buff("weaken", 100, StackingPolicy.Refresh, debuff: true,
            modifiers: new StatModifier(Stat.Def, ModifierOp.PercentAdd, -50));
        var skill = TestKit.Skill("hex", effects: new ApplyBuffEffect(weaken));
        var (caster, target, world) = Setup(skill);

        caster.Caster.TryBegin("hex", target, world, 0);

        Assert.True(target.Buffs.Has("weaken"));
        Assert.Equal(5, target.Stat(Stat.Def)); // 10 * 50%
    }
}
