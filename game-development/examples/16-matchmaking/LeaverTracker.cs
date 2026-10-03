namespace Course.Matchmaking;

/// <summary>Counts strikes for leaving a started run and locks the player out of the queue for
/// strikes x cooldown ticks.</summary>
public sealed class LeaverTracker
{
    private readonly int _cooldownPerStrike;
    private readonly Dictionary<string, (int Strikes, long Until)> _state = new();

    public LeaverTracker(int cooldownPerStrike) => _cooldownPerStrike = cooldownPerStrike;

    public void Strike(string playerId, long nowTick)
    {
        var strikes = _state.TryGetValue(playerId, out var s) ? s.Strikes + 1 : 1;
        _state[playerId] = (strikes, nowTick + (long)strikes * _cooldownPerStrike);
    }

    public bool CanQueue(string playerId, long nowTick) =>
        !_state.TryGetValue(playerId, out var s) || nowTick >= s.Until;

    public int Strikes(string playerId) => _state.TryGetValue(playerId, out var s) ? s.Strikes : 0;
}
