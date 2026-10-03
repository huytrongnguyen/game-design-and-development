namespace Course.PartyControl;

/// <summary>A saved line-up: who leads, who is in the field, and each character's stance.</summary>
public sealed record Squad(int LeaderIndex, IReadOnlyList<bool> Active, IReadOnlyList<string> Stances);
