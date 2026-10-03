namespace Course.PartyControl.Tests;

public class PartySizeTests
{
    [Fact]
    public void SingleCharacter_HasNoFollowers_AndThePetTrailsTheLeader()
    {
        var party = TestParties.Create(size: 1);
        party.Tick(1);

        Assert.Equal(1, party.Size);
        Assert.Equal(new Vec2(-2, 0), party.PetSlot());
        Assert.False(party.TrySwitchLeader(1));
    }

    [Fact]
    public void FourCharacters_FillASecondRowOfTheWedge()
    {
        var party = TestParties.Create(size: 4);

        Assert.Equal(new Vec2(-3, 2), party.SlotFor(1));
        Assert.Equal(new Vec2(-3, -2), party.SlotFor(2));
        Assert.Equal(new Vec2(-5, 2), party.SlotFor(3));
        Assert.Equal(new Vec2(-6, 0), party.PetSlot());
    }

    [Fact]
    public void WrongNumberOfCharacters_IsRejected()
    {
        var two = TestParties.Create(size: 2);
        var config = new PartyConfig { Size = 3 };

        Assert.Throws<ArgumentException>(() => new Party(1, two.Characters, two.Pet, config));
    }

    [Fact]
    public void SquadSlots_FollowTheConfiguration()
    {
        var party = TestParties.Create();

        Assert.True(party.TrySaveSquad(3));
        Assert.False(party.TrySaveSquad(4));
    }
}
