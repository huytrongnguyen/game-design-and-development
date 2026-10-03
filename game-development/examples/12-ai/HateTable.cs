namespace M12.Ai;

/// <summary>
/// Per-monster threat table. Damage and heals add threat, threat decays, a challenger must beat
/// the current target by <see cref="SwitchRatio"/> to steal it, and a taunt forces the target for a while.
/// </summary>
public sealed class HateTable
{
    private readonly Dictionary<int, double> _threat = new();
    private readonly List<int> _scratch = new();
    private int _tauntId = -1;
    private long _tauntUntilTick;

    /// <summary>Challenger needs more than current * ratio to take over (1.1 = "110% rule").</summary>
    public double SwitchRatio { get; init; } = 1.1;

    /// <summary>Healing adds this fraction of the healed amount as threat on every engaged monster.</summary>
    public double HealFactor { get; init; } = 0.5;

    public int Count => _threat.Count;

    public double ThreatOf(int id) => _threat.TryGetValue(id, out var t) ? t : 0;

    public void Add(int id, double amount) => _threat[id] = ThreatOf(id) + amount;

    public void AddDamage(int attackerId, double damage) => Add(attackerId, damage);

    public void AddHeal(int healerId, double healedAmount) => Add(healerId, healedAmount * HealFactor);

    public void Remove(int id)
    {
        _threat.Remove(id);
        if (_tauntId == id) _tauntId = -1;
    }

    public void Clear()
    {
        _threat.Clear();
        _tauntId = -1;
    }

    /// <summary>Every entry loses <paramref name="rate"/> of its value; entries below 1 are dropped.</summary>
    public void Decay(double rate)
    {
        _scratch.Clear();
        foreach (var id in _threat.Keys) _scratch.Add(id);
        foreach (var id in _scratch)
        {
            var value = _threat[id] * (1 - rate);
            if (value < 1) Remove(id);
            else _threat[id] = value;
        }
    }

    /// <summary>Raises the taunter to the current top threat and forces it as the target until the deadline tick.</summary>
    public void Taunt(int id, long currentTick, long durationTicks)
    {
        var top = 0.0;
        foreach (var t in _threat.Values) top = Math.Max(top, t);
        _threat[id] = Math.Max(ThreatOf(id), top);
        _tauntId = id;
        _tauntUntilTick = currentTick + durationTicks;
    }

    /// <summary>Picks who the monster should attack. Returns null when the table is empty.</summary>
    public int? SelectTarget(long currentTick, int? currentTargetId)
    {
        if (_tauntId >= 0 && currentTick < _tauntUntilTick && _threat.ContainsKey(_tauntId))
            return _tauntId;

        var best = -1;
        var bestThreat = double.MinValue;
        foreach (var (id, threat) in _threat)
        {
            // Ties go to the lowest id so the result never depends on dictionary order.
            if (threat > bestThreat || (threat == bestThreat && id < best))
            {
                best = id;
                bestThreat = threat;
            }
        }
        if (best < 0) return null;

        if (currentTargetId is int cur && cur != best && _threat.TryGetValue(cur, out var curThreat)
            && bestThreat <= curThreat * SwitchRatio)
            return cur;

        return best;
    }
}
