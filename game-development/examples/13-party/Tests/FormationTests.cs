namespace Course.PartyControl.Tests;

public class FormationTests
{
    private const int Precision = 9;

    [Fact]
    public void SlotFor_HeadingZero_UsesOffsetsUnrotated()
    {
        var party = TestParties.Create(at: new Vec2(10, 10));
        party.Leader.HeadingDegrees = 0;

        // Offsets (-3, 2) and (-3, -2) added to the leader at (10, 10).
        AssertVec(new Vec2(7, 12), party.SlotFor(1));
        AssertVec(new Vec2(7, 8), party.SlotFor(2));
    }

    [Fact]
    public void SlotFor_HeadingNinetyDegrees_RotatesOffsetsToFaceNorth()
    {
        var party = TestParties.Create(at: new Vec2(10, 10));
        party.Leader.HeadingDegrees = 90;

        // Rotating by 90 degrees maps (x, y) to (-y, x): (-3, 2) -> (-2, -3) and (-3, -2) -> (2, -3).
        AssertVec(new Vec2(8, 7), party.SlotFor(1));
        AssertVec(new Vec2(12, 7), party.SlotFor(2));
        // The pet offset (-4, 0) becomes (0, -4).
        AssertVec(new Vec2(10, 6), party.PetSlot());
    }

    [Fact]
    public void SlotFor_LeaderIsSecondCharacter_FollowersAreZeroAndTwoInRosterOrder()
    {
        var party = TestParties.Create(at: new Vec2(10, 10));
        party.TrySwitchLeader(1);
        party.Leader.HeadingDegrees = 90;

        AssertVec(new Vec2(8, 7), party.SlotFor(0));
        AssertVec(new Vec2(12, 7), party.SlotFor(2));
    }

    [Fact]
    public void Tick_LeaderWalksNorth_HeadingBecomesNinetyAndSlotsTurnWithIt()
    {
        var party = TestParties.Create();
        party.Move(new Vec2(0, 10));

        party.Tick(1.0); // speed 5 -> leader at (0, 5)

        AssertVec(new Vec2(0, 5), party.Leader.Position);
        Assert.Equal(90.0, party.Leader.HeadingDegrees, precision: Precision);
        AssertVec(new Vec2(-2, 2), party.SlotFor(1));
    }

    [Fact]
    public void Tick_FollowerWithinLeash_WalksTowardItsSlotAtItsOwnSpeed()
    {
        var party = TestParties.Create();
        party.Characters[1].Position = new Vec2(-3, 12); // slot is (-3, 2): 10 away, inside the 20 leash

        party.Tick(1.0);

        AssertVec(new Vec2(-3, 7), party.Characters[1].Position);
    }

    [Fact]
    public void Tick_FollowerBeyondLeash_SnapsStraightToItsSlot()
    {
        var party = TestParties.Create();
        party.Characters[2].Position = new Vec2(500, 500);

        party.Tick(0.05);

        AssertVec(new Vec2(-3, -2), party.Characters[2].Position);
    }

    [Fact]
    public void Tick_PetFarBehind_CatchesUpToPetSlot()
    {
        var party = TestParties.Create();
        party.Pet.Position = new Vec2(-100, 0);

        party.Tick(0.05);

        AssertVec(new Vec2(-4, 0), party.Pet.Position);
    }

    [Fact]
    public void TryMoveUnit_DetachedCharacter_StaysOutOfFormationUntilRegroup()
    {
        var party = TestParties.Create();

        Assert.True(party.TryMoveUnit(2, new Vec2(0, -10)));
        party.Tick(1.0); // detached unit walks its own way: (0, 0) -> (0, -5)
        AssertVec(new Vec2(0, -5), party.Characters[2].Position);

        party.Regroup();
        party.Tick(1.0); // slot (-3, -2) is sqrt(18) = 4.24 away, under one step of 5: it arrives
        Assert.True(party.Characters[2].FollowsLeader);
        AssertVec(new Vec2(-3, -2), party.Characters[2].Position);
    }

    [Fact]
    public void TryMoveUnit_Leader_IsRefused()
    {
        var party = TestParties.Create();

        Assert.False(party.TryMoveUnit(0, new Vec2(1, 1)));
    }

    private static void AssertVec(Vec2 expected, Vec2 actual)
    {
        Assert.Equal(expected.X, actual.X, precision: Precision);
        Assert.Equal(expected.Y, actual.Y, precision: Precision);
    }
}
