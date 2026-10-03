namespace Course.Progression;

/// <summary>Checks a class tree before the game uses it. Bad data should fail at load, not in a player's session.</summary>
public static class TreeValidator
{
    public static List<string> Validate(ClassTree tree)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>();
        foreach (var c in tree.Classes)
            if (!ids.Add(c.Id)) errors.Add($"duplicate class '{c.Id}'");

        var skillIds = new HashSet<string>();
        foreach (var c in tree.Classes)
        {
            if (c.MaxCircles < 1) errors.Add($"{c.Id}: MaxCircles must be at least 1");
            foreach (var p in c.Requires)
            {
                var req = tree.Find(p.Class);
                if (req is null) errors.Add($"{c.Id}: unknown prerequisite '{p.Class}'");
                else
                {
                    if (p.MinCircles < 1 || p.MinCircles > req.MaxCircles)
                        errors.Add($"{c.Id}: prerequisite '{p.Class}' needs {p.MinCircles} circles but it has at most {req.MaxCircles}");
                    if (req.Rank >= c.Rank)
                        errors.Add($"{c.Id}: prerequisite '{p.Class}' must have a lower rank");
                }
            }
            if (c.Rank == 1 && c.Requires.Length > 0) errors.Add($"{c.Id}: a rank 1 class cannot have prerequisites");
            if (c.Rank > 1 && c.Requires.Length == 0) errors.Add($"{c.Id}: a rank {c.Rank} class needs a prerequisite");
            if (tree.Rules.GrowthBudgetPerCircle > 0 && c.Growth.Total != tree.Rules.GrowthBudgetPerCircle)
                errors.Add($"{c.Id}: growth {c.Growth.Total} per circle, budget is {tree.Rules.GrowthBudgetPerCircle}");
            foreach (var s in c.Skills)
            {
                if (!skillIds.Add(s.Id)) errors.Add($"duplicate skill '{s.Id}'");
                if (s.UnlockCircle < 1 || s.UnlockCircle > c.MaxCircles)
                    errors.Add($"{c.Id}: skill '{s.Id}' unlocks at circle {s.UnlockCircle}, outside 1..{c.MaxCircles}");
            }
        }

        errors.AddRange(FindCycles(tree));
        return errors;
    }

    /// <summary>Depth-first search with three colours; reports each class where a prerequisite chain loops back.</summary>
    private static IEnumerable<string> FindCycles(ClassTree tree)
    {
        var state = new Dictionary<string, int>();   // 1 = on the current path, 2 = finished
        var found = new List<string>();

        void Visit(ClassDef c)
        {
            state[c.Id] = 1;
            foreach (var p in c.Requires)
            {
                var next = tree.Find(p.Class);
                if (next is null) continue;                       // reported elsewhere
                if (state.TryGetValue(next.Id, out int s))
                {
                    if (s == 1) found.Add($"prerequisite cycle: {c.Id} -> {next.Id}");
                }
                else Visit(next);
            }
            state[c.Id] = 2;
        }

        foreach (var c in tree.Classes)
            if (!state.ContainsKey(c.Id)) Visit(c);
        return found;
    }
}
