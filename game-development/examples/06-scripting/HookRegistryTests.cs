namespace Course.Scripting;

public class HookRegistryTests
{
    [Fact]
    public void Register_ValidName_CanBeFound()
    {
        var registry = new HookRegistry();
        registry.Register("QUEST_DONE_WOLVES", _ => { });
        Assert.True(registry.Contains("QUEST_DONE_WOLVES"));
        Assert.Equal(1, registry.Count);
    }

    [Theory]
    [InlineData("questDoneWolves")]
    [InlineData("WOLVES")]
    [InlineData("quest_done_wolves")]
    [InlineData("QUEST DONE")]
    public void Register_NameBreaksConvention_Throws(string name)
    {
        var registry = new HookRegistry();
        Assert.Throws<ArgumentException>(() => registry.Register(name, _ => { }));
    }

    [Fact]
    public void Register_DuplicateName_Throws()
    {
        var registry = new HookRegistry();
        registry.Register("QUEST_DONE_WOLVES", _ => { });
        Assert.Throws<InvalidOperationException>(() => registry.Register("QUEST_DONE_WOLVES", _ => { }));
    }
}
