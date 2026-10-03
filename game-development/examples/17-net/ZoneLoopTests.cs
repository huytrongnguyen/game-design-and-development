namespace M17.Net;

public class ZoneLoopTests
{
    private static StatePayload State(IReadOnlyList<ZoneOutput> outputs) =>
        (StatePayload)outputs.Single(o => o.Op == Ops.State).Payload;

    [Fact]
    public void Tick_Join_BroadcastsFullSnapshotAtSpawn()
    {
        var zone = new ZoneLoop();
        zone.Enqueue(Intent.Join(1, "ann"));
        var state = State(zone.Tick());
        Assert.True(state.Full);
        Assert.Equal(new PlayerView(1, "ann", 50, 50), state.Players.Single());
    }

    [Fact]
    public void Tick_MoveIntent_StepsTwoUnitsTowardTarget()
    {
        var zone = new ZoneLoop();
        zone.Enqueue(Intent.Join(1, "ann"));
        zone.Tick();
        zone.Enqueue(Intent.Move(1, 60, 50));
        var state = State(zone.Tick());
        Assert.False(state.Full);
        Assert.Equal(52, state.Players.Single().X);
    }

    [Fact]
    public void Tick_MoveWithinOneStep_ArrivesThenGoesQuiet()
    {
        var zone = new ZoneLoop();
        zone.Enqueue(Intent.Join(1, "ann"));
        zone.Tick();
        zone.Enqueue(Intent.Move(1, 51, 50));
        Assert.Equal(51, State(zone.Tick()).Players.Single().X);
        Assert.Empty(zone.Tick());
    }

    [Fact]
    public void Tick_MoveOutsideWorld_IsClampedToTheEdge()
    {
        var zone = new ZoneLoop();
        zone.Enqueue(Intent.Join(1, "ann"));
        zone.Tick();
        zone.Enqueue(Intent.Move(1, 500, 50));
        for (var i = 0; i < 40; i++)
        {
            zone.Tick();
        }

        zone.Enqueue(Intent.Join(2, "bob"));
        var ann = State(zone.Tick()).Players.Single(p => p.Id == 1);
        Assert.Equal(100, ann.X);
    }

    [Fact]
    public void Tick_ChatFromUnknownPlayer_IsIgnored()
    {
        var zone = new ZoneLoop();
        zone.Enqueue(Intent.Chat(99, "hi"));
        Assert.Empty(zone.Tick());
    }

    [Fact]
    public void Tick_LongChat_IsTruncatedTo200Characters()
    {
        var zone = new ZoneLoop();
        zone.Enqueue(Intent.Join(1, "ann"));
        zone.Enqueue(Intent.Chat(1, new string('a', 500)));
        var chat = (ChatBroadcast)zone.Tick().Single(o => o.Op == Ops.Chat).Payload;
        Assert.Equal(200, chat.Text.Length);
    }
}
