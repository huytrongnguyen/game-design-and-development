namespace M12.Ai.Tests;

public class AllyAiTests
{
    private static (Unit self, Unit leader, Unit tank, Unit enemy) Scene(int tankHp, double leaderX = 3)
    {
        var self = new Unit(1, new Vec2(0, 0), 100);
        var leader = new Unit(2, new Vec2(leaderX, 0), 100);
        var tank = new Unit(3, new Vec2(4, 0), 100);
        tank.SetHp(tankHp);
        var enemy = new Unit(9, new Vec2(8, 0), 100);
        return (self, leader, tank, enemy);
    }

    [Fact]
    public void Decide_HealerWithAllyAt40Percent_HealsWithScore06()
    {
        var (s, l, t, e) = Scene(40);
        var d = AllyAi.Decide(AllyPreset.Healer, s, l, [s, l, t], [e]);
        Assert.Equal(AllyAction.Heal, d.Action);
        Assert.Equal(3, d.TargetId);
        Assert.Equal(0.6, d.Score, 6);
    }

    [Fact]
    public void Decide_AttackerWithSameScene_Attacks()
    {
        var (s, l, t, e) = Scene(40);
        var d = AllyAi.Decide(AllyPreset.Attacker, s, l, [s, l, t], [e]);
        Assert.Equal(AllyAction.Attack, d.Action);
        Assert.Equal(9, d.TargetId);
        Assert.Equal(0.6, d.Score, 6);
    }

    [Fact]
    public void Decide_HealerAllAboveThreshold_FallsBackToAttackWithScore012()
    {
        var (s, l, t, e) = Scene(80);
        var d = AllyAi.Decide(AllyPreset.Healer, s, l, [s, l, t], [e]);
        Assert.Equal(AllyAction.Attack, d.Action);
        Assert.Equal(0.12, d.Score, 6);
    }

    [Fact]
    public void Decide_LeaderFarAndAllyAt40Percent_FollowBeatsHeal()
    {
        var (s, l, t, e) = Scene(40, leaderX: 20); // follow = 0.8 * min(1, 14/6) = 0.8 > 0.6
        var d = AllyAi.Decide(AllyPreset.Healer, s, l, [s, t], [e]);
        Assert.Equal(AllyAction.Follow, d.Action);
    }

    [Fact]
    public void Decide_LeaderFarAndAllyAt10Percent_HealBeatsFollow()
    {
        var (s, l, t, e) = Scene(10, leaderX: 20); // heal 0.9 > follow 0.8
        var d = AllyAi.Decide(AllyPreset.Healer, s, l, [s, t], [e]);
        Assert.Equal(AllyAction.Heal, d.Action);
        Assert.Equal(0.9, d.Score, 6);
    }

    [Fact]
    public void Decide_NothingToDo_Idles()
    {
        var self = new Unit(1, new Vec2(0, 0), 100);
        var leader = new Unit(2, new Vec2(2, 0), 100);
        var d = AllyAi.Decide(AllyPreset.Healer, self, leader, [self, leader], []);
        Assert.Equal(AllyAction.Idle, d.Action);
    }
}
