using System.Text.Json;

namespace Course.MiniEngine;

/// <summary>
/// Loads entities and their starting components from a small JSON array — the "content" side of the
/// engine, kept separate from the systems that act on it (module 05 covers a full data-driven design).
/// </summary>
public static class DataLoader
{
    public static List<EntityId> LoadEntities(string json, EntityStore store)
    {
        var ids = new List<EntityId>();
        using var document = JsonDocument.Parse(json);

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var id = store.CreateEntity();
            ids.Add(id);

            if (element.TryGetProperty("position", out var position))
                store.Set(id, new Position(position.GetProperty("x").GetSingle(), position.GetProperty("y").GetSingle()));

            if (element.TryGetProperty("velocity", out var velocity))
                store.Set(id, new Velocity(velocity.GetProperty("dx").GetSingle(), velocity.GetProperty("dy").GetSingle()));

            if (element.TryGetProperty("timer", out var timer))
            {
                var eventName = timer.GetProperty("eventName").GetString()
                    ?? throw new FormatException("timer.eventName is required");
                store.Set(id, new Timer(timer.GetProperty("ticksRemaining").GetInt32(), eventName));
            }

            if (element.TryGetProperty("randomWalker", out var randomWalker) && randomWalker.GetBoolean())
                store.Set(id, new RandomWalker());
        }

        return ids;
    }
}
