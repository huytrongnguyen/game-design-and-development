namespace Course.Scripting;

/// <summary>
/// Validated quest content. <see cref="Load"/> resolves every hook name and compiles every formula up front,
/// reporting all problems at once, so a typo fails the build or server start, never a player's quest turn-in.
/// </summary>
public sealed class QuestCatalog
{
    private readonly Dictionary<string, (QuestDef Def, ContentHook Hook, CompiledExpression? Gold)> _quests = new();

    private QuestCatalog() { }

    public static QuestCatalog Load(IEnumerable<QuestDef> defs, HookRegistry registry, IReadOnlySet<string> knownProperties)
    {
        var catalog = new QuestCatalog();
        var problems = new List<string>();

        foreach (var def in defs)
        {
            if (catalog._quests.ContainsKey(def.Id))
            {
                problems.Add($"Quest '{def.Id}': duplicate id.");
                continue;
            }

            if (!registry.TryGet(def.CompleteHook, out var hook))
            {
                problems.Add($"Quest '{def.Id}': hook '{def.CompleteHook}' is not registered.");
                continue;
            }

            CompiledExpression? gold = null;
            if (def.GoldFormula is not null)
            {
                try
                {
                    gold = ExpressionParser.Parse(def.GoldFormula);
                    foreach (var variable in gold.Variables.Where(v => !knownProperties.Contains(v)))
                        problems.Add($"Quest '{def.Id}': formula uses unknown property '{variable}'.");
                }
                catch (ExpressionException ex)
                {
                    problems.Add($"Quest '{def.Id}': bad formula '{def.GoldFormula}': {ex.Message}");
                    continue;
                }
            }

            catalog._quests[def.Id] = (def, hook, gold);
        }

        if (problems.Count > 0) throw new ContentValidationException(problems);
        return catalog;
    }

    /// <summary>Grants the formula's gold (if any), then runs the quest's hook. Returns false for an unknown quest id.</summary>
    public bool TryComplete(string questId, PlayerState player, IPropertyLookup properties)
    {
        if (!_quests.TryGetValue(questId, out var quest)) return false;

        var context = new HookContext(player, properties);
        if (quest.Gold is not null) context.GrantGold((long)quest.Gold.Evaluate(properties));
        quest.Hook(context);
        return true;
    }
}
