namespace M24.Pipeline;

public class StringTableTests
{
    private static StringTable Build()
    {
        var t = new StringTable("en");
        t.Add("en", "greet", "Hello, {name}!");
        t.Add("en", "loot.one", "You found {count} coin.");
        t.Add("en", "loot.other", "You found {count} coins.");
        t.Add("en", "only.english", "English only");
        t.Add("vi", "greet", "Xin chao, {name}!");
        return t;
    }

    private static Dictionary<string, object> Args(params (string, object)[] pairs) =>
        pairs.ToDictionary(p => p.Item1, p => p.Item2);

    [Fact]
    public void Get_Placeholder_IsSubstituted()
    {
        Assert.Equal("Hello, Mira!", Build().Get("en", "greet", Args(("name", "Mira"))));
    }

    [Theory]
    [InlineData(1, "You found 1 coin.")]
    [InlineData(5, "You found 5 coins.")]
    [InlineData(0, "You found 0 coins.")]
    public void Get_Count_PicksPluralVariant(int count, string expected)
    {
        Assert.Equal(expected, Build().Get("en", "loot", Args(("count", count))));
    }

    [Fact]
    public void Get_MissingInLanguage_FallsBackToFallbackLanguage()
    {
        Assert.Equal("English only", Build().Get("vi", "only.english"));
    }

    [Fact]
    public void Get_UnknownId_ReturnsVisibleMarker()
    {
        Assert.Equal("[nope]", Build().Get("vi", "nope"));
    }

    [Fact]
    public void Get_UnknownPlaceholder_IsLeftInPlace()
    {
        Assert.Equal("Xin chao, {name}!", Build().Get("vi", "greet", Args(("other", 1))));
    }

    [Fact]
    public void Has_PluralOnlyId_IsTrue()
    {
        Assert.True(Build().Has("en", "loot"));
        Assert.False(Build().Has("vi", "loot"));
    }
}
