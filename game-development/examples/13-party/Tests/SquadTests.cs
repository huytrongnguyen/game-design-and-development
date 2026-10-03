namespace Course.PartyControl.Tests;

public class SquadTests
{
    [Fact]
    public void TryRecallSquad_AfterChangingLeaderLineUpAndStances_RestoresTheSavedLineUp()
    {
        var party = TestParties.Create();
        party.Characters[0].Stance = "Sword";
        party.Characters[1].Stance = "Guard";
        party.Characters[2].Stance = "Bow";
        Assert.True(party.TrySaveSquad(1));

        party.TrySwitchLeader(2);
        party.Characters[1].Active = false;
        party.Characters[0].Stance = "Default";
        party.Characters[2].Stance = "Default";

        Assert.True(party.TryRecallSquad(1));

        Assert.Equal(0, party.LeaderIndex);
        Assert.All(party.Characters, c => Assert.True(c.Active));
        Assert.Equal(["Sword", "Guard", "Bow"], party.Characters.Select(c => c.Stance));
    }

    [Fact]
    public void TryRecallSquad_SquadWithABenchedCharacter_BenchesThemAgain()
    {
        var party = TestParties.Create();
        party.Characters[1].Active = false;
        party.TrySaveSquad(2);
        party.Characters[1].Active = true;

        party.TryRecallSquad(2);

        Assert.False(party.Characters[1].Active);
    }

    [Fact]
    public void TryRecallSquad_EmptySlot_ReturnsFalse()
    {
        var party = TestParties.Create();

        Assert.False(party.TryRecallSquad(3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void TrySaveSquad_SlotOutsideOneToThree_ReturnsFalse(int slot)
    {
        var party = TestParties.Create();

        Assert.False(party.TrySaveSquad(slot));
    }

    [Fact]
    public void TryRecallSquad_ReattachesDetachedCharacters()
    {
        var party = TestParties.Create();
        party.TrySaveSquad(1);
        party.TryMoveUnit(1, new Vec2(5, 5));

        party.TryRecallSquad(1);

        Assert.True(party.Characters[1].FollowsLeader);
    }
}
