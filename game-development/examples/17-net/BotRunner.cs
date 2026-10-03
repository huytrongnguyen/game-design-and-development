namespace M17.Net;

/// <summary>Scripted bot: wanders to random points and chats now and then. Seeded, so runs repeat.</summary>
public static class BotRunner
{
    public static async Task RunAsync(Uri url, string name, int seed, int steps, int pauseMilliseconds)
    {
        var random = new Random(seed);
        await using var bot = await BotClient.ConnectAsync(url, name);
        await bot.WaitForOpAsync(Ops.Welcome);
        Console.WriteLine($"{name}: connected as player {bot.PlayerId}, session {bot.SessionId}");
        for (var i = 0; i < steps; i++)
        {
            var x = random.Next(0, 101);
            var y = random.Next(0, 101);
            await bot.SendAsync(Ops.Move, new { x, y });
            Console.WriteLine($"{name}: move to ({x}, {y})");
            if (i % 3 == 0)
            {
                await bot.SendAsync(Ops.Chat, new { text = $"{name} step {i}" });
            }

            await Task.Delay(pauseMilliseconds);
        }
    }
}
