namespace M17.Net;

/// <summary>Operation names. The server and the clients share this vocabulary.</summary>
public static class Ops
{
    public const int ProtocolVersion = 1;

    // client -> server
    public const string Hello = "hello";
    public const string Ping = "ping";
    public const string Move = "move";
    public const string Chat = "chat";

    // server -> client
    public const string Welcome = "welcome";
    public const string Pong = "pong";
    public const string State = "state";
    public const string Error = "error";
}
