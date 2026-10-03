using System.Text.Json;
using System.Text.Json.Serialization;

namespace Course.ActionCombat;

/// <summary>Loads a skill set from JSON and refuses data that fails validation.</summary>
public static class SkillLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyDictionary<string, SkillDef> Load(string json)
    {
        var skills = JsonSerializer.Deserialize<List<SkillDef>>(json, Options)
                     ?? throw new InvalidDataException("empty skill file");
        var errors = skills.SelectMany(s => s.Validate()).ToList();
        if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors));
        return skills.ToDictionary(s => s.Id);
    }
}
