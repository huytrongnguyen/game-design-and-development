namespace Course.Matchmaking;

/// <summary>The data that configures a queue. A different game changes numbers, not code.</summary>
/// <param name="Tanks">Required tanks per party.</param>
/// <param name="Healers">Required healers per party.</param>
/// <param name="Damage">Required damage dealers per party.</param>
/// <param name="BaseBracket">Allowed level difference from the anchor at wait 0.</param>
/// <param name="WidenEveryTicks">Every this many ticks of waiting the bracket grows.</param>
/// <param name="WidenBy">How much the bracket grows per step.</param>
/// <param name="MaxBracket">The bracket never grows beyond this.</param>
/// <param name="CrossShardAfterTicks">Wait after which players from other shards may be mixed in; -1 = never.</param>
/// <param name="LeaverCooldownTicks">Queue lockout per strike for leaving a started run.</param>
public sealed record QueueRules(
    int Tanks, int Healers, int Damage,
    int BaseBracket, int WidenEveryTicks, int WidenBy, int MaxBracket,
    int CrossShardAfterTicks, int LeaverCooldownTicks)
{
    public int PartySize => Tanks + Healers + Damage;

    public int Need(Role r) => r switch { Role.Tank => Tanks, Role.Healer => Healers, _ => Damage };

    /// <summary>Level bracket after waiting the given number of ticks (integer steps, capped).</summary>
    public int BracketAt(long waitTicks) =>
        Math.Min(MaxBracket, BaseBracket + (int)(waitTicks / WidenEveryTicks) * WidenBy);

    public bool CrossShardAllowed(long waitTicks) => CrossShardAfterTicks >= 0 && waitTicks >= CrossShardAfterTicks;
}
