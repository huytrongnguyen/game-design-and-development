namespace Course.PartyControl;

/// <summary>
/// What a client may send. It names a slot in the sender's own party (0..2), never a global unit id,
/// so a client cannot even express an order for someone else's character.
/// </summary>
public readonly record struct PartyCommand(CommandKind Kind, Vec2 Point = default, int Number = 0, char Key = ' ')
{
    public static PartyCommand MoveTo(Vec2 point) => new(CommandKind.Move, Point: point);
    public static PartyCommand SwitchLeader(int index) => new(CommandKind.SwitchLeader, Number: index);
    public static PartyCommand PressKey(char key) => new(CommandKind.SkillKey, Key: key);
    public static PartyCommand SaveSquad(int slot) => new(CommandKind.SaveSquad, Number: slot);
    public static PartyCommand RecallSquad(int slot) => new(CommandKind.RecallSquad, Number: slot);
    public static PartyCommand Regroup() => new(CommandKind.Regroup);
}
