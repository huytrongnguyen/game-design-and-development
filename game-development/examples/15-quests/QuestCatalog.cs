using System.Text.Json;
using System.Text.Json.Serialization;

namespace Course.Quests;

/// <summary>All quest definitions, loaded from data and validated once at load time.
/// Catching a broken chain here (unknown prerequisite, cycle) is the main advantage of
/// data over scripts: a script only reveals the problem when a player gets stuck.</summary>
public sealed class QuestCatalog
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Dictionary<string, QuestDef> _byId;

    private QuestCatalog(Dictionary<string, QuestDef> byId) => _byId = byId;

    public IEnumerable<QuestDef> All => _byId.Values;

    public QuestDef Get(string id) => _byId[id];

    /// <summary>Parses and validates. On failure returns false with every problem found, not just the first.</summary>
    public static bool TryLoad(string json, out QuestCatalog? catalog, out IReadOnlyList<string> errors)
    {
        catalog = null;
        List<QuestDef>? defs;
        try { defs = JsonSerializer.Deserialize<List<QuestDef>>(json, Options); }
        catch (JsonException ex) { errors = [$"invalid JSON: {ex.Message}"]; return false; }

        var problems = Validate(defs ?? []);
        errors = problems;
        if (problems.Count > 0) return false;

        catalog = new QuestCatalog(defs!.ToDictionary(d => d.Id));
        return true;
    }

    public static List<string> Validate(IReadOnlyList<QuestDef> defs)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>();
        foreach (var d in defs)
        {
            if (!ids.Add(d.Id)) errors.Add($"duplicate quest id '{d.Id}'");
            if (d.Objectives is null || d.Objectives.Count == 0) errors.Add($"quest '{d.Id}' has no objectives");
            else if (d.Objectives.Any(o => o.Count <= 0)) errors.Add($"quest '{d.Id}' has an objective with count <= 0");
        }

        foreach (var d in defs)
            foreach (var p in d.Prerequisites ?? [])
                if (!ids.Contains(p)) errors.Add($"quest '{d.Id}' requires unknown quest '{p}'");

        if (errors.Count == 0) errors.AddRange(FindCycles(defs));
        return errors;
    }

    // Depth-first search with three colours: a back edge to a "grey" node is a cycle,
    // meaning none of the quests on it could ever unlock.
    private static IEnumerable<string> FindCycles(IReadOnlyList<QuestDef> defs)
    {
        var byId = defs.ToDictionary(d => d.Id);
        var colour = new Dictionary<string, int>(); // 1 = in progress, 2 = done
        var found = new List<string>();

        void Visit(string id)
        {
            colour[id] = 1;
            foreach (var p in byId[id].Prerequisites ?? [])
            {
                if (!colour.TryGetValue(p, out var c)) Visit(p);
                else if (c == 1) found.Add($"prerequisite cycle through '{p}'");
            }
            colour[id] = 2;
        }

        foreach (var d in defs)
            if (!colour.ContainsKey(d.Id)) Visit(d.Id);
        return found;
    }
}
