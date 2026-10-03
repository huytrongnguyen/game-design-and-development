namespace Course.Topology;

/// <summary>Everything about a character that must survive a zone change.</summary>
public sealed record CharacterState(
    int Id, string AccountId, string Name, int Level, int Hp, int Gold, string Zone, int X, int Y);
