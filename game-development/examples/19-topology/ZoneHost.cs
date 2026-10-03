namespace Course.Topology;

/// <summary>
/// One zone process. It owns the live state of the characters inside it; the directory and
/// store only ever see snapshots. Gameplay changes (<see cref="ApplyDamage"/>) mutate live state.
/// </summary>
public sealed class ZoneHost : IZoneHost
{
    private readonly Dictionary<int, CharacterState> _live = new();
    private readonly int _capacity;

    public ZoneHost(string name, int spawnX, int spawnY, int capacity)
    {
        Name = name;
        Spawn = (spawnX, spawnY);
        _capacity = capacity;
    }

    public string Name { get; }
    public (int X, int Y) Spawn { get; }
    public bool IsFull => _live.Count >= _capacity;
    public IReadOnlyCollection<int> CharacterIds => _live.Keys;

    public void Admit(CharacterState state)
    {
        if (IsFull) throw new InvalidOperationException($"Zone {Name} is full.");
        _live[state.Id] = state with { Zone = Name };
    }

    public CharacterState? Remove(int characterId)
    {
        if (!_live.Remove(characterId, out var state)) return null;
        return state;
    }

    public CharacterState? Peek(int characterId) => _live.GetValueOrDefault(characterId);

    public void ApplyDamage(int characterId, int damage)
    {
        var s = _live[characterId];
        _live[characterId] = s with { Hp = Math.Max(0, s.Hp - damage) };
    }

    public void MoveTo(int characterId, int x, int y) =>
        _live[characterId] = _live[characterId] with { X = x, Y = y };
}
