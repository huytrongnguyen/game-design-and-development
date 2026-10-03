namespace Course.PartyControl.Tests;

public class PetTests
{
    [Fact]
    public void TryUse_FullStamina_SpendsTheActionCost()
    {
        var stamina = new PetActivity();

        Assert.True(stamina.TryUse());
        Assert.Equal(990, stamina.Value);
    }

    [Fact]
    public void TryUse_AtTheFloor_RefusesUntilFed()
    {
        var stamina = new PetActivity(value: 100);

        Assert.False(stamina.TryUse());
        stamina.Feed(20);
        Assert.True(stamina.TryUse());
        Assert.Equal(110, stamina.Value);
    }

    [Fact]
    public void Feed_PastTheCap_IsClampedToMax()
    {
        var stamina = new PetActivity(value: 950);

        stamina.Feed(500);

        Assert.Equal(1000, stamina.Value);
    }

    [Fact]
    public void TryPetLoot_TwoItemsInRadius_TakesTheNearestAndSpendsStamina()
    {
        var party = TestParties.Create();
        party.Pet.Position = new Vec2(0, 0);
        GroundItem[] items = [new(1, new Vec2(4, 0)), new(2, new Vec2(2, 0)), new(3, new Vec2(9, 0))];

        var picked = party.TryPetLoot(items);

        Assert.Equal(2, picked?.Id);
        Assert.Equal(990, party.PetStamina.Value);
    }

    [Fact]
    public void TryPetLoot_NothingInRadius_SpendsNothing()
    {
        var party = TestParties.Create();

        var picked = party.TryPetLoot([new GroundItem(1, new Vec2(6, 0))]); // radius is 5

        Assert.Null(picked);
        Assert.Equal(1000, party.PetStamina.Value);
    }

    [Fact]
    public void TryPetLoot_ExhaustedPet_LeavesTheItemOnTheGround()
    {
        var party = TestParties.Create();
        party.PetStamina.Feed(-1000 + 100);

        var picked = party.TryPetLoot([new GroundItem(1, new Vec2(1, 0))]);

        Assert.Null(picked);
    }
}
