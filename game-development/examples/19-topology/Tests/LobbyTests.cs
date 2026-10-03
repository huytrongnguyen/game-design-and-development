namespace Course.Topology.Tests;

public class LobbyTests
{
    [Fact]
    public void ListCharacters_ValidToken_ReturnsOnlyThatAccountsCharacters()
    {
        var w = new TestWorld();
        var (status, chars) = w.Lobby.ListCharacters(w.LoginMira());
        Assert.Equal(TokenStatus.Valid, status);
        Assert.Equal(["Aria", "Bram"], chars.Select(c => c.Name));
    }

    [Fact]
    public void ListCharacters_ExpiredToken_ReturnsNothing()
    {
        var w = new TestWorld();
        var token = w.LoginMira();
        w.Clock.Advance(61);
        var (status, chars) = w.Lobby.ListCharacters(token);
        Assert.Equal(TokenStatus.Expired, status);
        Assert.Empty(chars);
    }

    [Fact]
    public void EnterWorld_AnotherAccountsCharacter_IsRefused()
    {
        var w = new TestWorld();
        var r = w.Lobby.EnterWorld(w.LoginMira(), characterId: 3);
        Assert.Equal(EnterStatus.NotYourCharacter, r.Status);
        Assert.Null(w.Directory.LocationOf(3));
    }

    [Fact]
    public void EnterWorld_TamperedToken_IsRefused()
    {
        var w = new TestWorld();
        var r = w.Lobby.EnterWorld(w.LoginMira() + "x", characterId: 1);
        Assert.Equal(EnterStatus.InvalidToken, r.Status);
    }

    [Fact]
    public void EnterWorld_OwnCharacter_LandsInSavedZone()
    {
        var w = new TestWorld();
        var r = w.Lobby.EnterWorld(w.LoginMira(), characterId: 1);
        Assert.Equal(EnterStatus.Ok, r.Status);
        Assert.Equal("meadow", r.Zone);
        Assert.Equal((55, 77), (w.Meadow.Peek(1)!.X, w.Meadow.Peek(1)!.Y));
    }

    [Fact]
    public void EnterWorld_FullZone_ReturnsZoneFull()
    {
        var w = new TestWorld(); // meadow capacity is 2
        var t = w.LoginMira();
        w.Store.Save(new CharacterState(4, "mira", "Dax", 5, 100, 0, "meadow", 0, 0));
        Assert.Equal(EnterStatus.Ok, w.Lobby.EnterWorld(t, 1).Status);
        Assert.Equal(EnterStatus.Ok, w.Lobby.EnterWorld(t, 2).Status);
        Assert.Equal(EnterStatus.ZoneFull, w.Lobby.EnterWorld(t, 4).Status);
    }
}
