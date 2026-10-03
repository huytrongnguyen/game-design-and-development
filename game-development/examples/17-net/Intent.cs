namespace M17.Net;

/// <summary>
/// What a client asks for, after the network layer has validated it. The zone decides what happens.
/// </summary>
public sealed record Intent(IntentKind Kind, int PlayerId, string Name = "", double X = 0, double Y = 0, string Text = "")
{
    public static Intent Join(int playerId, string name) => new(IntentKind.Join, playerId, Name: name);

    public static Intent Leave(int playerId) => new(IntentKind.Leave, playerId);

    public static Intent Move(int playerId, double x, double y) => new(IntentKind.Move, playerId, X: x, Y: y);

    public static Intent Chat(int playerId, string text) => new(IntentKind.Chat, playerId, Text: text);
}
