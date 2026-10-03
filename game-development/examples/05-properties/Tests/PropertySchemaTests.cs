namespace Course.Properties.Tests;

public class PropertySchemaTests
{
    [Fact]
    public void Add_DirectCycle_ThrowsWithThePath()
    {
        var schema = new PropertySchema()
            .Add(PropertyDefinition.Calculated("A", b => b.Get("B"), "B"));

        var ex = Assert.Throws<PropertyCycleException>(() =>
            schema.Add(PropertyDefinition.Calculated("B", b => b.Get("A"), "A")));

        Assert.Equal(new[] { "B", "A", "B" }, ex.Path);
    }

    [Fact]
    public void Add_LongerCycle_IsDetected()
    {
        var schema = new PropertySchema()
            .Add(PropertyDefinition.Calculated("A", b => b.Get("B"), "B"))
            .Add(PropertyDefinition.Calculated("B", b => b.Get("C"), "C"));

        var ex = Assert.Throws<PropertyCycleException>(() =>
            schema.Add(PropertyDefinition.Calculated("C", b => b.Get("A"), "A")));

        Assert.Equal("dependency cycle: C -> A -> B -> C", ex.Message);
    }

    [Fact]
    public void Add_SelfDependency_Throws()
    {
        var schema = new PropertySchema();

        Assert.Throws<PropertyCycleException>(() =>
            schema.Add(PropertyDefinition.Calculated("A", b => b.Get("A"), "A")));
    }

    [Fact]
    public void Add_RejectedCycle_LeavesSchemaUsable()
    {
        var schema = new PropertySchema()
            .Add(PropertyDefinition.Calculated("A", b => b.Get("B"), "B"));
        Assert.Throws<PropertyCycleException>(() =>
            schema.Add(PropertyDefinition.Calculated("B", b => b.Get("A"), "A")));

        schema.Add(PropertyDefinition.Base("B"));

        Assert.Empty(schema.FindUnknownDependencies());
    }

    [Fact]
    public void FindUnknownDependencies_TypoInDeclaration_IsReported()
    {
        var schema = new PropertySchema()
            .Add(PropertyDefinition.Base("STR"))
            .Add(PropertyDefinition.Calculated("ATK", b => b.Get("STR"), "STR", "DEXX"));

        Assert.Equal(new[] { "DEXX" }, schema.FindUnknownDependencies());
    }

    [Fact]
    public void DependentsOf_Str_ListsOnlyAtk()
    {
        var schema = CharacterModel.CreateSchema();

        Assert.Equal(new[] { "ATK" }, schema.DependentsOf("STR"));
        Assert.Equal(new[] { "Rating" }, schema.DependentsOf("ATK"));
    }
}
