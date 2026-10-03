using System.Text.RegularExpressions;

namespace M24.Pipeline;

/// <summary>
/// Build-time checks that run in CI: unique ids, naming rules, reference integrity, string keys that exist.
/// </summary>
public static partial class ContentValidator
{
    // Ids look like "kind.name_with_underscores".
    [GeneratedRegex(@"^(skill|item|monster)\.[a-z][a-z0-9_]*$")]
    private static partial Regex IdPattern();

    public static IReadOnlyList<Issue> Validate(ContentSet content, StringTable strings, string baseLanguage)
    {
        var issues = new List<Issue>();
        var skillIds = CheckIds("skills", "skill", content.Skills.Select(s => s.Id), issues);
        var itemIds = CheckIds("items", "item", content.Items.Select(i => i.Id), issues);
        CheckIds("monsters", "monster", content.Monsters.Select(m => m.Id), issues);

        foreach (var s in content.Skills)
        {
            CheckKey($"skills[{s.Id}].nameKey", s.NameKey, strings, baseLanguage, issues);
            if (s.Power <= 0)
            {
                issues.Add(new Issue(Severity.Error, $"skills[{s.Id}].power", $"power must be positive, got {s.Power}"));
            }
        }

        foreach (var i in content.Items)
        {
            CheckKey($"items[{i.Id}].nameKey", i.NameKey, strings, baseLanguage, issues);
            if (i.Skill is not null && !skillIds.Contains(i.Skill))
            {
                issues.Add(new Issue(Severity.Error, $"items[{i.Id}].skill", $"unknown skill '{i.Skill}'"));
            }
        }

        foreach (var m in content.Monsters)
        {
            CheckKey($"monsters[{m.Id}].nameKey", m.NameKey, strings, baseLanguage, issues);
            for (var n = 0; n < m.Drops.Length; n++)
            {
                if (!itemIds.Contains(m.Drops[n]))
                {
                    issues.Add(new Issue(Severity.Error, $"monsters[{m.Id}].drops[{n}]", $"unknown item '{m.Drops[n]}'"));
                }
            }
        }

        return issues;
    }

    private static HashSet<string> CheckIds(string table, string kind, IEnumerable<string> ids, List<Issue> issues)
    {
        var seen = new HashSet<string>();
        foreach (var id in ids)
        {
            if (!IdPattern().IsMatch(id) || !id.StartsWith(kind + ".", StringComparison.Ordinal))
            {
                issues.Add(new Issue(Severity.Error, $"{table}[{id}]", $"id must look like '{kind}.lower_snake_case'"));
            }

            if (!seen.Add(id))
            {
                issues.Add(new Issue(Severity.Error, $"{table}[{id}]", "duplicate id"));
            }
        }

        return seen;
    }

    private static void CheckKey(string location, string key, StringTable strings, string language, List<Issue> issues)
    {
        if (!strings.Has(language, key))
        {
            issues.Add(new Issue(Severity.Error, location, $"string '{key}' is missing in '{language}'"));
        }
    }
}
