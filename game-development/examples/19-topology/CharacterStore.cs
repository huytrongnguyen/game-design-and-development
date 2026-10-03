namespace Course.Topology;

/// <summary>The "database proxy": the only owner of persisted character data.</summary>
public sealed class CharacterStore
{
    private readonly Dictionary<int, CharacterState> _characters = new();

    public void Save(CharacterState state) => _characters[state.Id] = state;

    public CharacterState? Get(int id) => _characters.GetValueOrDefault(id);

    public IReadOnlyList<CharacterState> ListByAccount(string accountId) =>
        _characters.Values.Where(c => c.AccountId == accountId).OrderBy(c => c.Id).ToList();
}
