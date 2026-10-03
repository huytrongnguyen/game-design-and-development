namespace Course.PartyControl;

/// <summary>A request to cast, resolved from a key press: which character, which skill.</summary>
public readonly record struct SkillCast(int CharacterIndex, string SkillId);
