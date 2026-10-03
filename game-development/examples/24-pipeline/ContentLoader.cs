using System.Text.Json;

namespace M24.Pipeline;

/// <summary>
/// Reads the authoring format (human-edited JSON). The validator runs on what this loader produces,
/// so it sees exactly the data the game would see, with no second parser to drift.
/// </summary>
public static class ContentLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public static ContentSet Parse(string json)
    {
        var set = JsonSerializer.Deserialize<ContentSet>(json, Options)
            ?? throw new JsonException("empty content file");
        return set with
        {
            Skills = set.Skills ?? [],
            Items = set.Items ?? [],
            Monsters = set.Monsters ?? [],
        };
    }
}
