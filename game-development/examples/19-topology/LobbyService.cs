namespace Course.Topology;

/// <summary>
/// The character-select process: trusts only a valid token, lists the account's characters
/// and asks the directory to place the chosen one in a zone.
/// </summary>
public sealed class LobbyService
{
    private readonly SessionTokenService _tokens;
    private readonly CharacterStore _store;
    private readonly ZoneDirectory _directory;

    public LobbyService(SessionTokenService tokens, CharacterStore store, ZoneDirectory directory)
    {
        _tokens = tokens;
        _store = store;
        _directory = directory;
    }

    public (TokenStatus Status, IReadOnlyList<CharacterState> Characters) ListCharacters(string token)
    {
        var check = _tokens.Validate(token);
        if (check.Status != TokenStatus.Valid) return (check.Status, []);
        return (TokenStatus.Valid, _store.ListByAccount(check.Claims!.AccountId));
    }

    public EnterResult EnterWorld(string token, int characterId)
    {
        var check = _tokens.Validate(token);
        if (check.Status != TokenStatus.Valid) return new EnterResult(EnterStatus.InvalidToken, null);

        var record = _store.Get(characterId);
        if (record is null) return new EnterResult(EnterStatus.UnknownCharacter, null);
        if (record.AccountId != check.Claims!.AccountId)
            return new EnterResult(EnterStatus.NotYourCharacter, null);

        return _directory.Enter(characterId);
    }
}
