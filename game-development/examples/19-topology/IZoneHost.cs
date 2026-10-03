namespace Course.Topology;

/// <summary>
/// What the rest of the system may ask of a zone process. Everything crosses this boundary as
/// plain data, so an implementation can later live in another process behind a network call.
/// </summary>
public interface IZoneHost
{
    string Name { get; }
    (int X, int Y) Spawn { get; }
    bool IsFull { get; }
    IReadOnlyCollection<int> CharacterIds { get; }
    void Admit(CharacterState state);
    CharacterState? Remove(int characterId);
}
