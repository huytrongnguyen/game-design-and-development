namespace Course.PartyControl.Tests;

public class PartyHostTests
{
    [Fact]
    public void Entities_TwoPlayers_FourUnitsEachAllTaggedWithTheirOwner()
    {
        var host = new PartyHost();
        host.Add(TestParties.Create(ownerId: 1));
        host.Add(TestParties.Create(ownerId: 2));

        var entities = host.Entities().ToList();

        Assert.Equal(8, entities.Count);
        Assert.Equal(4, entities.Count(u => u.OwnerId == 1));
        Assert.Equal(1, entities.Count(u => u.OwnerId == 2 && u.Kind == UnitKind.Pet));
    }

    [Fact]
    public void Apply_MoveFromPlayerOne_NeverTouchesPlayerTwosUnits()
    {
        var host = new PartyHost();
        var one = TestParties.Create(ownerId: 1);
        var two = TestParties.Create(ownerId: 2);
        host.Add(one);
        host.Add(two);

        var result = host.Apply(1, PartyCommand.MoveTo(new Vec2(9, 9)));

        Assert.Equal(CommandResult.Ok, result);
        Assert.Equal(new Vec2(9, 9), one.Leader.Destination);
        Assert.All(two.Units, u => Assert.Null(u.Destination));
    }

    [Fact]
    public void Apply_UnknownPlayer_IsRejected()
    {
        var host = new PartyHost();

        Assert.Equal(CommandResult.UnknownPlayer, host.Apply(99, PartyCommand.MoveTo(new Vec2(1, 1))));
    }

    [Fact]
    public void Apply_SwitchToBadIndex_ReturnsRejected()
    {
        var host = new PartyHost();
        host.Add(TestParties.Create());

        Assert.Equal(CommandResult.Rejected, host.Apply(1, PartyCommand.SwitchLeader(7)));
    }

    [Fact]
    public void Apply_KeyPress_QueuesACastForTheSimulation()
    {
        var host = new PartyHost();
        host.Add(TestParties.Create());

        host.Apply(1, PartyCommand.PressKey('X'));

        Assert.Equal([new SkillCast(2, "Z2")], host.PendingCasts);
    }

    [Fact]
    public void Apply_SaveThenRecallSquad_RoundTripsThroughTheCommandPath()
    {
        var host = new PartyHost();
        var party = TestParties.Create();
        host.Add(party);

        host.Apply(1, PartyCommand.SaveSquad(1));
        host.Apply(1, PartyCommand.SwitchLeader(2));
        var result = host.Apply(1, PartyCommand.RecallSquad(1));

        Assert.Equal(CommandResult.Ok, result);
        Assert.Equal(0, party.LeaderIndex);
    }

    [Fact]
    public void Tick_TwoParties_AdvanceIndependently()
    {
        var host = new PartyHost();
        var one = TestParties.Create(ownerId: 1);
        var two = TestParties.Create(ownerId: 2);
        host.Add(one);
        host.Add(two);
        host.Apply(1, PartyCommand.MoveTo(new Vec2(10, 0)));

        host.Tick(1.0);

        Assert.Equal(new Vec2(5, 0), one.Leader.Position);
        Assert.Equal(new Vec2(0, 0), two.Leader.Position);
    }
}
