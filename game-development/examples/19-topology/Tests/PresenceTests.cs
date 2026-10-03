namespace Course.Topology.Tests;

public class PresenceTests
{
    private static int ZonesHolding(TestWorld w, int id) =>
        new[] { w.Meadow, w.Harbor }.Count(z => z.CharacterIds.Contains(id));

    [Fact]
    public void Enter_CalledTwice_SecondIsRejectedAndCharacterIsInOneZone()
    {
        var w = new TestWorld();
        var token = w.LoginMira();
        Assert.Equal(EnterStatus.Ok, w.Lobby.EnterWorld(token, 1).Status);
        Assert.Equal(EnterStatus.AlreadyOnline, w.Lobby.EnterWorld(token, 1).Status);
        Assert.Equal(1, ZonesHolding(w, 1));
    }

    [Fact]
    public void Enter_WhileInTransit_IsRejected()
    {
        var w = new TestWorld();
        w.Lobby.EnterWorld(w.LoginMira(), 1);
        w.Directory.BeginTransfer(1, "harbor");

        Assert.Equal(0, ZonesHolding(w, 1)); // in flight: in no zone at all
        Assert.True(w.Directory.IsInTransit(1));
        Assert.Equal(EnterStatus.AlreadyOnline, w.Directory.Enter(1).Status);
        Assert.Equal(0, ZonesHolding(w, 1));
    }

    [Fact]
    public void Transfer_ThereAndBack_AlwaysExactlyOneZone()
    {
        var w = new TestWorld();
        w.Lobby.EnterWorld(w.LoginMira(), 1);
        for (var i = 0; i < 4; i++)
        {
            var to = i % 2 == 0 ? "harbor" : "meadow";
            var t = w.Directory.BeginTransfer(1, to).Ticket!;
            Assert.Equal(0, ZonesHolding(w, 1));
            w.Directory.CompleteTransfer(t);
            Assert.Equal(1, ZonesHolding(w, 1));
            Assert.Equal(to, w.Directory.LocationOf(1));
        }
    }

    [Fact]
    public void Logout_SavesLiveStateAndFreesTheCharacterToEnterAgain()
    {
        var w = new TestWorld();
        w.Lobby.EnterWorld(w.LoginMira(), 1);
        w.Meadow.ApplyDamage(1, 400); // 900 -> 500

        Assert.True(w.Directory.Logout(1));
        Assert.Equal(500, w.Store.Get(1)!.Hp);
        Assert.Equal(0, ZonesHolding(w, 1));
        Assert.Equal(EnterStatus.Ok, w.Directory.Enter(1).Status);
        Assert.Equal(500, w.Meadow.Peek(1)!.Hp);
    }

    [Fact]
    public void ExpireTransfers_DropsOnlyPastDeadlineTickets()
    {
        var w = new TestWorld();
        var t = w.LoginMira();
        w.Lobby.EnterWorld(t, 1);
        w.Directory.BeginTransfer(1, "harbor");          // expires at 1020
        w.Clock.Advance(10);
        w.Lobby.EnterWorld(t, 2);
        w.Directory.BeginTransfer(2, "harbor");          // expires at 1030
        w.Clock.Advance(10);                            // now 1020

        Assert.Equal(1, w.Directory.ExpireTransfers());
        Assert.False(w.Directory.IsInTransit(1));
        Assert.True(w.Directory.IsInTransit(2));
    }
}
