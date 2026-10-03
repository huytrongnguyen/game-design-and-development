using M17.Net;

// dotnet run --project examples/17-net                          -> server on http://localhost:5080
// dotnet run --project examples/17-net bot ws://localhost:5080/ws -> a bot that wanders and chats
if (args is ["bot", var url, ..])
{
    await BotRunner.RunAsync(new Uri(url), "bot" + Environment.ProcessId % 1000, seed: 7, steps: 20, pauseMilliseconds: 500);
    return;
}

var app = GameServer.Create("http://localhost:5080", args: args);
Console.WriteLine("Open http://localhost:5080 in a browser (several tabs = several players).");
await app.RunAsync();
