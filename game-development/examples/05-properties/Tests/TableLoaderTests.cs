namespace Course.Properties.Tests;

public class TableLoaderTests
{
    private const string Good = """
        {"rows":[
          {"id":"fighter","name":"Fighter","str":70,"dex":40,"con":50},
          {"id":"wizard","name":"Wizard","str":20,"dex":30,"con":35,"note":"glass cannon"}
        ]}
        """;

    [Fact]
    public void Load_ValidFile_ReturnsTypedRows()
    {
        var result = TableLoader.Load(CharacterModel.JobTable, Good);

        Assert.True(result.Ok);
        Assert.Equal(2, result.Table!.Count);
        Assert.Equal(70, result.Table["fighter"].GetNumber("str"));
        Assert.Equal("Wizard", result.Table["wizard"].GetText("name"));
        Assert.False(result.Table["fighter"].Has("note"));
    }

    [Fact]
    public void Load_MissingRequiredField_NamesTableRowAndColumn()
    {
        var json = """{"rows":[{"id":"wizard","name":"Wizard","str":20,"dex":30}]}""";

        var result = TableLoader.Load(CharacterModel.JobTable, json);

        Assert.False(result.Ok);
        var error = Assert.Single(result.Errors);
        Assert.Equal("job[wizard].con: required field is missing", error.ToString());
    }

    [Fact]
    public void Load_SeveralProblems_ReportsAllOfThemAtOnce()
    {
        var json = """
            {"rows":[
              {"id":"a","name":"A","str":999,"dex":10,"con":10},
              {"id":"b","name":"B","str":"ten","dex":10,"con":10,"strr":1},
              {"id":"a","name":"A2","str":1,"dex":1,"con":1}
            ]}
            """;

        var result = TableLoader.Load(CharacterModel.JobTable, json);

        var messages = result.Errors.Select(e => e.ToString()).ToList();
        Assert.Equal(4, messages.Count);
        Assert.Contains("job[a].str: 999 is outside the allowed range 1..200", messages);
        Assert.Contains("job[b].str: expected a number", messages);
        Assert.Contains("job[b].strr: unknown field (typo?)", messages);
        Assert.Contains("job[a].id: duplicate id", messages);
    }

    [Fact]
    public void Load_BrokenJson_ReturnsOneErrorInsteadOfThrowing()
    {
        var result = TableLoader.Load(CharacterModel.JobTable, "{ not json");

        Assert.False(result.Ok);
        Assert.Contains("not valid JSON", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Reload_InvalidEdit_KeepsPreviousTable()
    {
        var store = new TableStore(CharacterModel.JobTable, Good);

        var bad = store.Reload("""{"rows":[{"id":"fighter","name":"Fighter"}]}""");

        Assert.False(bad.Ok);
        Assert.Equal(0, store.Version);
        Assert.Equal(70, store.Current["fighter"].GetNumber("str"));
    }

    [Fact]
    public void Reload_ValidEdit_SwapsTableAndBumpsVersion()
    {
        var store = new TableStore(CharacterModel.JobTable, Good);

        var ok = store.Reload("""{"rows":[{"id":"fighter","name":"Fighter","str":75,"dex":40,"con":50}]}""");

        Assert.True(ok.Ok);
        Assert.Equal(1, store.Version);
        Assert.Equal(75, store.Current["fighter"].GetNumber("str"));
        Assert.False(store.Current.TryGet("wizard", out _));
    }
}
