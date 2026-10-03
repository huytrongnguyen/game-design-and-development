namespace M17.Net;

/// <summary>Drives the zone at a fixed tick rate and fans the results out to all sessions.</summary>
public sealed class ZoneService(SessionRegistry registry, ServerOptions options) : BackgroundService
{
    public ZoneLoop Zone { get; } = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.TickMilliseconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var output in Zone.Tick())
                {
                    registry.Broadcast(MessageCodec.Serialize(output.Op, 0, output.Payload));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}
