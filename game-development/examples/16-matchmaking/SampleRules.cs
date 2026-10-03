namespace Course.Matchmaking;

public static class SampleRules
{
    /// <summary>4-player party: 1 tank, 1 healer, 2 damage. Bracket 2 levels, +2 every 100 ticks, capped at 10.
    /// Other shards are mixed in after 300 ticks. Each leave strike locks the player out for 600 ticks.</summary>
    public static readonly QueueRules Dungeon = new(1, 1, 2, 2, 100, 2, 10, 300, 600);
}
