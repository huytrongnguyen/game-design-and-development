namespace M24.Pipeline;

public class ContentValidatorTests
{
    private const string Json = """
    {
      "skills":   [ { "id": "skill.slash", "nameKey": "skill.slash.name", "power": 12 } ],
      "items":    [ { "id": "item.sword",  "nameKey": "item.sword.name",  "skill": "skill.slash" },
                    { "id": "item.staff",  "nameKey": "item.staff.name",  "skill": "skill.fireball" } ],
      "monsters": [ { "id": "monster.wolf", "nameKey": "monster.wolf.name", "drops": ["item.sword", "item.fang"] } ]
    }
    """;

    private static StringTable Strings()
    {
        var t = new StringTable("en");
        foreach (var k in new[] { "skill.slash.name", "item.sword.name", "item.staff.name", "monster.wolf.name" })
        {
            t.Add("en", k, k);
        }

        return t;
    }

    [Fact]
    public void Validate_ItemWithUnknownSkill_ReportsErrorWithLocation()
    {
        var issues = ContentValidator.Validate(ContentLoader.Parse(Json), Strings(), "en");

        Assert.Contains(issues, i => i.Location == "items[item.staff].skill" && i.Message == "unknown skill 'skill.fireball'");
    }

    [Fact]
    public void Validate_DropOfUnknownItem_ReportsIndexOfTheDrop()
    {
        var issues = ContentValidator.Validate(ContentLoader.Parse(Json), Strings(), "en");

        Assert.Contains(issues, i => i.Location == "monsters[monster.wolf].drops[1]");
    }

    [Fact]
    public void Validate_CleanData_HasExactlyTheTwoSeededErrors()
    {
        var issues = ContentValidator.Validate(ContentLoader.Parse(Json), Strings(), "en");

        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public void Validate_DuplicateAndMisnamedIds_AreErrors()
    {
        var json = """
        { "skills": [ { "id": "skill.a", "nameKey": "k", "power": 1 },
                      { "id": "skill.a", "nameKey": "k", "power": 1 },
                      { "id": "Skill.Bad", "nameKey": "k", "power": 1 } ] }
        """;
        var t = new StringTable("en");
        t.Add("en", "k", "x");

        var issues = ContentValidator.Validate(ContentLoader.Parse(json), t, "en");

        Assert.Contains(issues, i => i.Location == "skills[skill.a]" && i.Message == "duplicate id");
        Assert.Contains(issues, i => i.Location == "skills[Skill.Bad]");
    }

    [Fact]
    public void Validate_MissingStringAndBadPower_AreErrors()
    {
        var json = """{ "skills": [ { "id": "skill.x", "nameKey": "nope", "power": 0 } ] }""";

        var issues = ContentValidator.Validate(ContentLoader.Parse(json), new StringTable("en"), "en");

        Assert.Contains(issues, i => i.Location == "skills[skill.x].nameKey");
        Assert.Contains(issues, i => i.Location == "skills[skill.x].power" && i.Message == "power must be positive, got 0");
    }
}
