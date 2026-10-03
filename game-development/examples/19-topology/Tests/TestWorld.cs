namespace Course.Topology.Tests;

/// <summary>A small topology: two zones, one account "mira" with two characters.</summary>
public sealed class TestWorld
{
    public ManualClock Clock { get; } = new(1_000);
    public SessionTokenService Tokens { get; }
    public LoginService Login { get; }
    public CharacterStore Store { get; } = new();
    public ZoneDirectory Directory { get; }
    public LobbyService Lobby { get; }
    public ZoneHost Meadow { get; } = new("meadow", 10, 10, capacity: 2);
    public ZoneHost Harbor { get; } = new("harbor", 500, 40, capacity: 2);

    public TestWorld()
    {
        Tokens = new SessionTokenService(new byte[32].Select((_, i) => (byte)(i + 1)).ToArray(), Clock);
        Login = new LoginService(Tokens, tokenTtlSeconds: 60);
        Login.Register("mira", "pw1");
        Login.Register("ned", "pw2");
        Directory = new ZoneDirectory(Store, Clock, new Random(42), ticketTtlSeconds: 20);
        Directory.Register(Meadow);
        Directory.Register(Harbor);
        Lobby = new LobbyService(Tokens, Store, Directory);

        Store.Save(new CharacterState(1, "mira", "Aria", Level: 30, Hp: 900, Gold: 120, Zone: "meadow", X: 55, Y: 77));
        Store.Save(new CharacterState(2, "mira", "Bram", Level: 12, Hp: 300, Gold: 5, Zone: "meadow", X: 20, Y: 21));
        Store.Save(new CharacterState(3, "ned", "Cora", Level: 8, Hp: 200, Gold: 0, Zone: "harbor", X: 1, Y: 2));
    }

    public string LoginMira() => Login.Login("mira", "pw1").Token!;
}
