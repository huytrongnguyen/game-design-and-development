namespace Course.Progression;

internal static class Sample
{
    // Fighter (rank 1) branches into Knight, Berserker and Duelist (rank 2); Knight leads to Guardian (rank 3).
    // Every class grows by 6 stat points per circle, so no branch is strictly stronger.
    public const string Json = """
    {
      "rules": { "growthBudgetPerCircle": 6, "maxTotalCircles": 8 },
      "classes": [
        { "id": "fighter", "rank": 1, "maxCircles": 3, "growth": { "str": 3, "agi": 1, "vit": 2 }, "skillPointsPerCircle": 3,
          "skills": [ { "id": "slash", "maxLevel": 5 }, { "id": "whirl", "maxLevel": 3, "unlockCircle": 2 } ] },
        { "id": "mage", "rank": 1, "maxCircles": 3, "growth": { "agi": 1, "vit": 1, "int": 4 }, "skillPointsPerCircle": 3 },
        { "id": "knight", "rank": 2, "maxCircles": 3, "growth": { "str": 3, "vit": 3 }, "skillPointsPerCircle": 3,
          "requires": [ { "class": "fighter", "minCircles": 3 } ] },
        { "id": "berserker", "rank": 2, "maxCircles": 3, "growth": { "str": 4, "agi": 2 }, "skillPointsPerCircle": 3,
          "requires": [ { "class": "fighter", "minCircles": 3 } ] },
        { "id": "duelist", "rank": 2, "maxCircles": 3, "growth": { "str": 2, "agi": 4 }, "skillPointsPerCircle": 3,
          "requires": [ { "class": "fighter", "minCircles": 3 } ] },
        { "id": "guardian", "rank": 3, "maxCircles": 2, "growth": { "str": 2, "vit": 4 }, "skillPointsPerCircle": 3,
          "requires": [ { "class": "knight", "minCircles": 2 } ] }
      ]
    }
    """;

    public static ClassTree Tree() => ClassTree.Load(Json);

    public static Character CharacterAtLevel(ProgressionService svc, int level, long gold = 0)
    {
        var c = new Character { Gold = gold };
        svc.AddExp(c, new ExpCurve(Tree().Rules).ExpToReach(level));
        return c;
    }
}

public class ExpCurveTests
{
    private readonly ExpCurve _curve = new(new ProgressionRules());   // 100 * level ^ 1.5

    [Fact]
    public void Exp_To_Next_Level_Follows_The_Power_Curve()
    {
        Assert.Equal(100, _curve.ExpToNext(1));
        Assert.Equal(283, _curve.ExpToNext(2));   // 100 * 2^1.5 = 282.84
        Assert.Equal(520, _curve.ExpToNext(3));   // 100 * 3^1.5 = 519.6
        Assert.Equal(800, _curve.ExpToNext(4));   // 100 * 4^1.5
        Assert.Equal(2700, _curve.ExpToNext(9));  // 100 * 9^1.5
    }

    [Fact]
    public void Level_Is_Found_From_Total_Exp()
    {
        Assert.Equal(903, _curve.ExpToReach(4));  // 100 + 283 + 520
        Assert.Equal(4, _curve.LevelFor(903));
        Assert.Equal(3, _curve.LevelFor(902));
        Assert.Equal(1, _curve.LevelFor(0));
        Assert.Equal(50, _curve.LevelFor(long.MaxValue));   // capped at the maximum level
    }
}

public class ValidationTests
{
    private static List<string> Errors(string classesJson, string rules = "{}") =>
        TreeValidator.Validate(System.Text.Json.JsonSerializer.Deserialize<ClassTree>(
            $$"""{ "rules": {{rules}}, "classes": {{classesJson}} }""",
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);

    [Fact]
    public void The_Sample_Tree_Is_Valid() => Assert.Empty(TreeValidator.Validate(Sample.Tree()));

    [Fact]
    public void Unknown_Prerequisite_Is_Reported()
    {
        var errors = Errors("""[ { "id": "a", "rank": 2, "requires": [ { "class": "ghost", "minCircles": 1 } ] } ]""");
        Assert.Contains(errors, e => e.Contains("unknown prerequisite 'ghost'"));
    }

    [Fact]
    public void A_Prerequisite_Cycle_Is_Reported()
    {
        var errors = Errors("""
            [ { "id": "a", "rank": 2, "requires": [ { "class": "b", "minCircles": 1 } ] },
              { "id": "b", "rank": 2, "requires": [ { "class": "a", "minCircles": 1 } ] } ]
            """);
        Assert.Contains(errors, e => e.Contains("prerequisite cycle"));
    }

    [Fact]
    public void Prerequisite_Must_Have_A_Lower_Rank_And_Be_Reachable()
    {
        var errors = Errors("""
            [ { "id": "base", "rank": 1, "maxCircles": 2 },
              { "id": "next", "rank": 2, "requires": [ { "class": "base", "minCircles": 3 } ] },
              { "id": "same", "rank": 1, "requires": [ { "class": "base", "minCircles": 1 } ] } ]
            """);
        Assert.Contains(errors, e => e.Contains("needs 3 circles but it has at most 2"));
        Assert.Contains(errors, e => e.Contains("same") && e.Contains("lower rank"));
    }

    [Fact]
    public void A_Class_Off_The_Power_Budget_Is_Reported()
    {
        var errors = Errors("""[ { "id": "op", "rank": 1, "growth": { "str": 9 } } ]""", """{ "growthBudgetPerCircle": 6 }""");
        Assert.Contains(errors, e => e.Contains("growth 9 per circle, budget is 6"));
    }

    [Fact]
    public void Load_Rejects_Invalid_Data()
    {
        var ex = Assert.Throws<InvalidDataException>(() => ClassTree.Load(
            """{ "classes": [ { "id": "a", "rank": 2, "requires": [ { "class": "ghost", "minCircles": 1 } ] } ] }"""));
        Assert.Contains("ghost", ex.Message);
    }
}

public class AdvancementTests
{
    private readonly ProgressionService _svc = new(Sample.Tree());

    [Fact]
    public void Circles_Are_Limited_By_Character_Level()
    {
        var c = Sample.CharacterAtLevel(_svc, 4);              // 4 / 5 = 0 circles allowed
        Assert.Equal(Denial.LevelTooLow, _svc.Advance(c, "fighter"));
        c = Sample.CharacterAtLevel(_svc, 5);                  // 1 circle allowed
        Assert.Equal(Denial.None, _svc.Advance(c, "fighter"));
        Assert.Equal(Denial.LevelTooLow, _svc.Advance(c, "fighter"));
    }

    [Fact]
    public void A_Class_Needs_Its_Prerequisite_Circles()
    {
        var c = Sample.CharacterAtLevel(_svc, 20);             // 4 circles allowed
        _svc.Advance(c, "fighter");
        _svc.Advance(c, "fighter");
        Assert.Equal(Denial.MissingPrerequisite, _svc.CanAdvance(c, "knight"));   // only 2 of 3 fighter circles
        _svc.Advance(c, "fighter");
        Assert.Equal(Denial.None, _svc.Advance(c, "knight"));
        Assert.Equal(Denial.MaxCircles, _svc.CanAdvance(c, "fighter"));
    }

    [Fact]
    public void Rank_Slots_Limit_Branching()
    {
        var c = Sample.CharacterAtLevel(_svc, 30);             // 6 circles allowed
        for (int i = 0; i < 3; i++) _svc.Advance(c, "fighter");
        Assert.Equal(Denial.None, _svc.Advance(c, "knight"));
        Assert.Equal(Denial.None, _svc.Advance(c, "berserker"));            // second rank 2 class: allowed
        Assert.Equal(Denial.RankFull, _svc.CanAdvance(c, "duelist"));       // a third one: not allowed
        Assert.Equal(Denial.RankFull, _svc.CanAdvance(c, "mage"));          // rank 1 already holds fighter
        Assert.Equal(Denial.None, _svc.Advance(c, "knight"));               // more circles in a held class are fine
    }

    [Fact]
    public void Total_Circle_Cap_Is_Enforced()
    {
        var c = Sample.CharacterAtLevel(_svc, 50);             // level allows 10 but the cap is 8
        for (int i = 0; i < 3; i++) _svc.Advance(c, "fighter");
        for (int i = 0; i < 3; i++) _svc.Advance(c, "knight");
        for (int i = 0; i < 2; i++) _svc.Advance(c, "berserker");
        Assert.Equal(8, c.TotalCircles);
        Assert.Equal(Denial.TotalCircleCap, _svc.CanAdvance(c, "berserker"));
    }

    [Fact]
    public void Guardian_Needs_Two_Knight_Circles_And_A_Rank_3_Slot()
    {
        var c = Sample.CharacterAtLevel(_svc, 30);
        for (int i = 0; i < 3; i++) _svc.Advance(c, "fighter");
        _svc.Advance(c, "knight");
        Assert.Equal(Denial.MissingPrerequisite, _svc.CanAdvance(c, "guardian"));
        _svc.Advance(c, "knight");
        Assert.Equal(Denial.None, _svc.Advance(c, "guardian"));
    }

    [Fact]
    public void Unknown_Class_Is_Refused() => Assert.Equal(Denial.UnknownClass, _svc.CanAdvance(new Character(), "wizard"));
}

public class StatTests
{
    private readonly ProgressionService _svc = new(Sample.Tree());

    [Fact]
    public void Stats_Combine_Base_Class_Growth_And_Free_Points()
    {
        var c = Sample.CharacterAtLevel(_svc, 20);
        for (int i = 0; i < 3; i++) _svc.Advance(c, "fighter");   // 3 x (3,1,2,0) = (9,3,6,0)
        _svc.Advance(c, "knight");       // 1 x (3,0,3,0)
        Assert.Equal(new StatBlock(17, 8, 14, 5), _svc.TotalStats(c));   // + base (5,5,5,5)
        Assert.Equal(Denial.None, _svc.SpendFreePoint(c, Stat.Str, 10));
        Assert.Equal(27, _svc.TotalStats(c).Str);
    }

    [Fact]
    public void Free_Points_Come_From_Levels_And_Are_Capped_Per_Stat()
    {
        var c = Sample.CharacterAtLevel(_svc, 20);                // 2 points x 19 level-ups = 38
        Assert.Equal(38, _svc.FreePointsAvailable(c));
        Assert.Equal(Denial.NoPoints, _svc.SpendFreePoint(c, Stat.Str, 39));
        Assert.Equal(Denial.None, _svc.SpendFreePoint(c, Stat.Str, 38));
        Assert.Equal(0, _svc.FreePointsAvailable(c));

        var high = Sample.CharacterAtLevel(_svc, 30);             // 58 points; the per-stat cap is 40
        Assert.Equal(Denial.StatCap, _svc.SpendFreePoint(high, Stat.Int, 41));
        Assert.Equal(Denial.None, _svc.SpendFreePoint(high, Stat.Int, 40));
    }
}

public class SkillTests
{
    private readonly ProgressionService _svc = new(Sample.Tree());

    [Fact]
    public void Each_Circle_Grants_Skill_Points_And_Skills_Unlock_By_Circle()
    {
        var c = Sample.CharacterAtLevel(_svc, 10);
        Assert.Equal(Denial.SkillLocked, _svc.SpendSkillPoint(c, "slash"));    // class not taken yet
        _svc.Advance(c, "fighter");
        Assert.Equal(3, _svc.SkillPointsAvailable(c));
        Assert.Equal(Denial.SkillLocked, _svc.SpendSkillPoint(c, "whirl"));   // needs circle 2
        for (int i = 0; i < 3; i++) Assert.Equal(Denial.None, _svc.SpendSkillPoint(c, "slash"));
        Assert.Equal(Denial.NoPoints, _svc.SpendSkillPoint(c, "slash"));
        _svc.Advance(c, "fighter");
        Assert.Equal(Denial.None, _svc.SpendSkillPoint(c, "whirl"));
        Assert.Equal(2, _svc.SkillPointsAvailable(c));                        // 6 earned, 4 spent
    }

    [Fact]
    public void A_Skill_Stops_At_Its_Max_Level()
    {
        var c = Sample.CharacterAtLevel(_svc, 15);
        for (int i = 0; i < 3; i++) _svc.Advance(c, "fighter");               // 9 points
        for (int i = 0; i < 5; i++) _svc.SpendSkillPoint(c, "slash");
        Assert.Equal(Denial.SkillMaxed, _svc.SpendSkillPoint(c, "slash"));
        Assert.Equal(Denial.UnknownSkill, _svc.SpendSkillPoint(c, "nothing"));
    }
}

public class RespecTests
{
    private readonly ProgressionService _svc = new(Sample.Tree());

    [Fact]
    public void Stat_Respec_Costs_Gold_Per_Point_And_Fails_Cleanly_When_Short()
    {
        var c = Sample.CharacterAtLevel(_svc, 20, gold: 500);
        _svc.SpendFreePoint(c, Stat.Str, 10);
        Assert.Equal(Denial.NotEnoughGold, _svc.RespecStats(c));              // 10 x 100 = 1000 needed
        Assert.Equal(10, c.FreeAllocated.Str);                                // unchanged
        c.Gold = 1000;
        Assert.Equal(Denial.None, _svc.RespecStats(c));
        Assert.Equal(0, c.Gold);
        Assert.Equal(38, _svc.FreePointsAvailable(c));
    }

    [Fact]
    public void Skill_Respec_Is_Free_And_Refunds_The_Points()
    {
        var c = Sample.CharacterAtLevel(_svc, 10);
        _svc.Advance(c, "fighter");
        _svc.SpendSkillPoint(c, "slash");
        _svc.SpendSkillPoint(c, "slash");
        Assert.Equal(1, _svc.SkillPointsAvailable(c));
        _svc.RespecSkills(c);
        Assert.Equal(3, _svc.SkillPointsAvailable(c));
    }

    [Fact]
    public void Class_Reset_Costs_500_Per_Circle_And_Clears_Skills()
    {
        var c = Sample.CharacterAtLevel(_svc, 20, gold: 2000);
        for (int i = 0; i < 3; i++) _svc.Advance(c, "fighter");
        _svc.Advance(c, "knight");
        _svc.SpendSkillPoint(c, "slash");
        Assert.Equal(Denial.None, _svc.ResetClasses(c));                      // 4 circles x 500
        Assert.Equal(0, c.Gold);
        Assert.Equal(0, c.TotalCircles);
        Assert.Empty(c.SkillLevels);
        Assert.Equal(Denial.None, _svc.Advance(c, "mage"));                   // a different branch is now possible
    }
}
