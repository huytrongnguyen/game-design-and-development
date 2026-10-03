namespace Course.Skills;

public class StanceTests
{
    private static readonly SkillDefinition Slash = TestKit.Skill("slash", spCost: 10, cooldown: 0, effects: new DamageEffect(100));
    private static readonly SkillDefinition Shot = TestKit.Skill("shot", spCost: 10, cooldown: 0, effects: new DamageEffect(100));

    private static readonly Stance Melee = new("melee", ["slash"],
        [new StatModifier(Stat.Atk, ModifierOp.PercentAdd, 20)], SpPerInterval: 5);

    private static readonly Stance Ranged = new("ranged", ["shot"], [], SpPerInterval: -8);

    private static Combatant HeroWithBothSkills()
    {
        var hero = TestKit.Unit("hero", atk: 100);
        hero.Caster.Learn(Slash, 1);
        hero.Caster.Learn(Shot, 1);
        return hero;
    }

    [Fact]
    public void TryBegin_SkillOutsideTheActiveStance_IsRejected()
    {
        var hero = HeroWithBothSkills();
        var dummy = TestKit.Unit("dummy", x: 10);
        hero.SwitchStance(Melee);

        Assert.Equal(CastStatus.NotInStance, hero.Caster.TryBegin("shot", dummy, [hero, dummy], 0));
        Assert.Equal(CastStatus.Started, hero.Caster.TryBegin("slash", dummy, [hero, dummy], 0));
    }

    [Fact]
    public void SwitchStance_SwapsTheUsableSkillsAndTheStanceModifiers()
    {
        var hero = HeroWithBothSkills();
        var dummy = TestKit.Unit("dummy", x: 10);

        hero.SwitchStance(Melee);
        Assert.Equal(120, hero.Stat(Stat.Atk));

        hero.SwitchStance(Ranged);
        Assert.Equal(100, hero.Stat(Stat.Atk));
        Assert.Equal(CastStatus.Started, hero.Caster.TryBegin("shot", dummy, [hero, dummy], 0));
    }

    [Fact]
    public void SwitchStance_DuringACast_CancelsIt()
    {
        var bolt = TestKit.Skill("slash", cast: 30, effects: new DamageEffect(100));
        var hero = TestKit.Unit("hero");
        hero.Caster.Learn(bolt, 1);
        var dummy = TestKit.Unit("dummy", x: 10);
        hero.SwitchStance(Melee);
        hero.Caster.TryBegin("slash", dummy, [hero, dummy], 0);

        hero.SwitchStance(Ranged);
        hero.Tick(30);

        Assert.False(hero.Caster.IsCasting);
        Assert.Equal(1000, dummy.Hp);
    }

    [Fact]
    public void Tick_RegenStance_AddsSpEveryInterval_DrainStanceRemovesIt()
    {
        var hero = HeroWithBothSkills();
        hero.Caster.AddSp(-50); // 50 SP

        hero.SwitchStance(Melee);
        for (var t = 1; t <= 120; t++) hero.Tick(t);   // intervals at 60 and 120
        Assert.Equal(60, hero.Caster.Sp);

        hero.SwitchStance(Ranged);
        for (var t = 121; t <= 180; t++) hero.Tick(t); // interval at 180
        Assert.Equal(52, hero.Caster.Sp);
    }
}
