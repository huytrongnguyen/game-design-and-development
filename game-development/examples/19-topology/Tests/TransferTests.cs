namespace Course.Topology.Tests;

public class TransferTests
{
    private static TestWorld WorldWithAriaInMeadow()
    {
        var w = new TestWorld();
        Assert.Equal(EnterStatus.Ok, w.Lobby.EnterWorld(w.LoginMira(), 1).Status);
        return w;
    }

    [Fact]
    public void Transfer_PreservesLevelHpAndGold()
    {
        var w = WorldWithAriaInMeadow();
        w.Meadow.ApplyDamage(1, 250);   // live change, not yet saved: 900 -> 650
        w.Meadow.MoveTo(1, 60, 61);

        var begin = w.Directory.BeginTransfer(1, "harbor");
        var done = w.Directory.CompleteTransfer(begin.Ticket!);

        Assert.Equal(TransferStatus.Ok, done.Status);
        var s = w.Harbor.Peek(1)!;
        Assert.Equal((30, 650, 120), (s.Level, s.Hp, s.Gold));
        Assert.Equal("harbor", s.Zone);
        Assert.Equal((500, 40), (s.X, s.Y)); // arrives at the destination's spawn
        Assert.Equal(650, w.Store.Get(1)!.Hp); // and the arrival was checkpointed
    }

    [Fact]
    public void CompleteTransfer_SameTicketTwice_SecondIsRejected()
    {
        var w = WorldWithAriaInMeadow();
        var ticket = w.Directory.BeginTransfer(1, "harbor").Ticket!;

        Assert.Equal(TransferStatus.Ok, w.Directory.CompleteTransfer(ticket).Status);
        Assert.Equal(TransferStatus.InvalidTicket, w.Directory.CompleteTransfer(ticket).Status);
        Assert.Single(w.Harbor.CharacterIds);
    }

    [Fact]
    public void CompleteTransfer_ReplayAfterMovingOn_DoesNotDuplicateCharacter()
    {
        var w = WorldWithAriaInMeadow();
        var ticket = w.Directory.BeginTransfer(1, "harbor").Ticket!;
        w.Directory.CompleteTransfer(ticket);
        w.Directory.BeginTransfer(1, "meadow"); // character leaves harbor again
        Assert.Equal(TransferStatus.InvalidTicket, w.Directory.CompleteTransfer(ticket).Status);
        Assert.Empty(w.Harbor.CharacterIds);
    }

    [Fact]
    public void CompleteTransfer_AfterTwentySeconds_IsExpiredAndCharacterIsInNoZone()
    {
        var w = WorldWithAriaInMeadow();
        var ticket = w.Directory.BeginTransfer(1, "harbor").Ticket!;
        w.Clock.Advance(20);

        Assert.Equal(TransferStatus.Expired, w.Directory.CompleteTransfer(ticket).Status);
        Assert.Null(w.Directory.LocationOf(1));
        Assert.False(w.Directory.IsInTransit(1));
        Assert.Equal("meadow", w.Store.Get(1)!.Zone); // may log in again where it left off
    }

    [Fact]
    public void BeginTransfer_ToCurrentZone_IsRejected()
    {
        var w = WorldWithAriaInMeadow();
        Assert.Equal(TransferStatus.SameZone, w.Directory.BeginTransfer(1, "meadow").Status);
        Assert.Equal("meadow", w.Directory.LocationOf(1));
    }

    [Fact]
    public void BeginTransfer_CharacterNotOnline_IsRejected()
    {
        var w = new TestWorld();
        Assert.Equal(TransferStatus.NotInZone, w.Directory.BeginTransfer(1, "harbor").Status);
    }

    [Fact]
    public void CompleteTransfer_DestinationFull_KeepsTicketForRetry()
    {
        var w = WorldWithAriaInMeadow();
        var ticket = w.Directory.BeginTransfer(1, "harbor").Ticket!;
        w.Harbor.Admit(new CharacterState(90, "x", "A", 1, 1, 0, "harbor", 0, 0));
        w.Harbor.Admit(new CharacterState(91, "x", "B", 1, 1, 0, "harbor", 0, 0));

        Assert.Equal(TransferStatus.ZoneFull, w.Directory.CompleteTransfer(ticket).Status);
        w.Harbor.Remove(90);
        Assert.Equal(TransferStatus.Ok, w.Directory.CompleteTransfer(ticket).Status);
    }
}
