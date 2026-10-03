namespace M17.Net;

public static class GameServer
{
    /// <summary>Builds the host. Pass port 0 (e.g. http://127.0.0.1:0) to get a free port.</summary>
    public static WebApplication Create(string url, ServerOptions? options = null, string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.WebHost.UseUrls(url);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddSingleton(options ?? new ServerOptions());
        builder.Services.AddSingleton<SessionRegistry>();
        builder.Services.AddSingleton<ZoneService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ZoneService>());
        builder.Services.AddSingleton<GameSocketHandler>();

        var app = builder.Build();
        app.UseWebSockets();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapGet("/ws", async (HttpContext context, GameSocketHandler handler) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await handler.RunAsync(socket, context.RequestAborted);
        });
        return app;
    }
}
