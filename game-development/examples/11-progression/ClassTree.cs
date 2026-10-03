using System.Text.Json;

namespace Course.Progression;

/// <summary>A class that must already be taken, to at least <c>MinCircles</c> circles.</summary>
public sealed record Prerequisite(string Class, int MinCircles);

public sealed record SkillDef
{
    public string Id { get; init; } = "";
    public int MaxLevel { get; init; } = 1;
    /// <summary>The skill unlocks once its class has at least this many circles.</summary>
    public int UnlockCircle { get; init; } = 1;
}

/// <summary>One node of the class tree. A "circle" is one advancement step inside the class.</summary>
public sealed record ClassDef
{
    public string Id { get; init; } = "";
    /// <summary>Tier of the tree: rank 1 are starting classes, higher ranks come later.</summary>
    public int Rank { get; init; } = 1;
    public Prerequisite[] Requires { get; init; } = [];
    public int MaxCircles { get; init; } = 1;
    /// <summary>Stats gained for each circle taken in this class.</summary>
    public StatBlock Growth { get; init; }
    public int SkillPointsPerCircle { get; init; }
    public SkillDef[] Skills { get; init; } = [];
}

/// <summary>Every tunable number of the progression system, as data.</summary>
public sealed record ProgressionRules
{
    public int MaxLevel { get; init; } = 50;
    /// <summary>Exp needed to go from level L to L+1 is <c>round(ExpBase * L ^ ExpPower)</c>.</summary>
    public double ExpBase { get; init; } = 100;
    public double ExpPower { get; init; } = 1.5;
    /// <summary>A character may hold at most <c>level / LevelsPerCircle</c> circles in total.</summary>
    public int LevelsPerCircle { get; init; } = 5;
    public int MaxTotalCircles { get; init; } = 8;
    public int FreePointsPerLevel { get; init; } = 2;
    public int MaxFreePointsPerStat { get; init; } = 40;
    /// <summary>How many different classes of each rank a character may hold (default 1).</summary>
    public Dictionary<int, int> ClassesPerRank { get; init; } = new() { [1] = 1, [2] = 2, [3] = 2 };
    /// <summary>Every class should give this many stat points per circle, so no branch is strictly stronger. 0 = unchecked.</summary>
    public int GrowthBudgetPerCircle { get; init; }
    public int StatRespecGoldPerPoint { get; init; } = 100;
    public int ClassResetGoldPerCircle { get; init; } = 500;
}

public sealed record ClassTree
{
    public ProgressionRules Rules { get; init; } = new();
    public ClassDef[] Classes { get; init; } = [];

    public ClassDef? Find(string id) => Classes.FirstOrDefault(c => c.Id == id);

    public (ClassDef Class, SkillDef Skill)? FindSkill(string skillId)
    {
        foreach (var c in Classes)
            foreach (var s in c.Skills)
                if (s.Id == skillId) return (c, s);
        return null;
    }

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Parses JSON and validates it; throws <see cref="InvalidDataException"/> listing every problem.</summary>
    public static ClassTree Load(string json)
    {
        var tree = JsonSerializer.Deserialize<ClassTree>(json, Options) ?? throw new InvalidDataException("empty tree");
        var errors = TreeValidator.Validate(tree);
        if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors));
        return tree;
    }
}
