namespace M17.Net;

public sealed class ServerOptions
{
    public int TickMilliseconds { get; init; } = 50;
    public int RateBurst { get; init; } = 20;
    public double RatePerSecond { get; init; } = 10;
    public int MaxStrikes { get; init; } = 30;
    public int HelloTimeoutMilliseconds { get; init; } = 5000;
    public int MaxMessageBytes { get; init; } = 2048;
}
