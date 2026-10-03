namespace Course.PartyControl.Tests;

public class LeaderTests
{
    [Fact]
    public void Move_AfterSwitchingLeaderToSecondCharacter_OnlyTheNewLeaderReceivesIt()
    {
        var party = TestParties.Create();

        Assert.True(party.TrySwitchLeader(1));
        party.Move(new Vec2(20, 0));

        Assert.Equal(new Vec2(20, 0), party.Characters[1].Destination);
        Assert.Null(party.Characters[0].Destination);
        Assert.Null(party.Characters[2].Destination);
    }

    [Fact]
    public void Move_WithDefaultLeader_GoesToCharacterZero()
    {
        var party = TestParties.Create();

        party.Move(new Vec2(7, 7));

        Assert.Equal(new Vec2(7, 7), party.Characters[0].Destination);
        Assert.Null(party.Characters[1].Destination);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void TrySwitchLeader_IndexOutOfRange_ReturnsFalseAndKeepsLeader(int index)
    {
        var party = TestParties.Create();

        Assert.False(party.TrySwitchLeader(index));
        Assert.Equal(0, party.LeaderIndex);
    }

    [Fact]
    public void TrySwitchLeader_InactiveCharacter_IsRefused()
    {
        var party = TestParties.Create();
        party.Characters[2].Active = false;

        Assert.False(party.TrySwitchLeader(2));
        Assert.Equal(0, party.LeaderIndex);
    }

    [Fact]
    public void TrySwitchLeader_PendingOrder_IsCancelled()
    {
        var party = TestParties.Create();
        party.Move(new Vec2(50, 0));

        party.TrySwitchLeader(2);

        Assert.Null(party.Characters[0].Destination);
    }
}
