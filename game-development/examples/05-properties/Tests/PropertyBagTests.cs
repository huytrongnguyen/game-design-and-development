namespace Course.Properties.Tests;

public class PropertyBagTests
{
    private static PropertyBag Fighter(bool strict = false)
    {
        // STR 70, DEX 40, CON 50, Level 10.
        var json = """{"rows":[{"id":"fighter","name":"Fighter","str":70,"dex":40,"con":50}]}""";
        var table = TableLoader.Load(CharacterModel.JobTable, json).Table!;
        return CharacterModel.Spawn(CharacterModel.CreateSchema(), table["fighter"], level: 10, strict);
    }

    [Fact]
    public void Get_CalculatedProperty_ComputesFromBaseValues()
    {
        var bag = Fighter();

        Assert.Equal(550, bag.Get("MHP"));  // 50*10 + 10*5
        Assert.Equal(180, bag.Get("ATK"));  // 70*2 + 40
        Assert.Equal(235, bag.Get("Rating")); // 180 + 550/10
    }

    [Fact]
    public void Get_ReadTwice_ComputesOnce()
    {
        var bag = Fighter();

        bag.Get("MHP");
        bag.Get("MHP");

        Assert.Equal(1, bag.RecomputesOf("MHP"));
        Assert.Equal(1, bag.RecomputeCount);
    }

    [Fact]
    public void Set_Str_InvalidatesOnlyStatsThatDependOnIt()
    {
        var bag = Fighter();
        bag.Get("Rating"); // computes Rating, ATK, MHP once each
        Assert.Equal(3, bag.RecomputeCount);

        bag.Set("STR", 80);

        Assert.False(bag.IsCached("ATK"));
        Assert.False(bag.IsCached("Rating")); // reached through ATK: recursive invalidation
        Assert.True(bag.IsCached("MHP"));     // does not depend on STR
        Assert.Equal(255, bag.Get("Rating")); // 80*2 + 40 + 550/10
        Assert.Equal(1, bag.RecomputesOf("MHP"));
        Assert.Equal(2, bag.RecomputesOf("ATK"));
        Assert.Equal(2, bag.RecomputesOf("Rating"));
        Assert.Equal(5, bag.RecomputeCount); // 3 first time + ATK + Rating, and nothing else
    }

    [Fact]
    public void Set_SameValue_DoesNotInvalidate()
    {
        var bag = Fighter();
        bag.Get("ATK");

        bag.Set("STR", 70);
        bag.Get("ATK");

        Assert.Equal(1, bag.RecomputesOf("ATK"));
    }

    [Fact]
    public void Set_PropertyNobodyHasReadYet_ComputesNothing()
    {
        var bag = Fighter();

        bag.Set("STR", 80);

        Assert.Equal(0, bag.RecomputeCount);
    }

    [Fact]
    public void Set_CalculatedProperty_Throws()
    {
        var bag = Fighter();

        Assert.Throws<InvalidOperationException>(() => bag.Set("MHP", 5));
    }

    [Fact]
    public void Get_ForgottenDependency_GoesStaleWhenNotStrict()
    {
        // The formula reads CON but declares only Level: the classic stale-stat bug.
        var schema = new PropertySchema()
            .Add(PropertyDefinition.Base("CON"))
            .Add(PropertyDefinition.Base("Level"))
            .Add(PropertyDefinition.Calculated("MHP", b => b.Get("CON") * 10 + b.Get("Level"), "Level"));
        var bag = new PropertyBag(schema);
        bag.Set("CON", 50);
        bag.Set("Level", 1);
        Assert.Equal(501, bag.Get("MHP"));

        bag.Set("CON", 60);

        Assert.Equal(501, bag.Get("MHP")); // should be 601: stale
    }

    [Fact]
    public void Get_ForgottenDependency_ThrowsWhenStrict()
    {
        var schema = new PropertySchema()
            .Add(PropertyDefinition.Base("CON"))
            .Add(PropertyDefinition.Base("Level"))
            .Add(PropertyDefinition.Calculated("MHP", b => b.Get("CON") * 10 + b.Get("Level"), "Level"));
        var bag = new PropertyBag(schema, strict: true);
        bag.Set("CON", 50);
        bag.Set("Level", 1);

        var ex = Assert.Throws<UndeclaredDependencyException>(() => bag.Get("MHP"));

        Assert.Equal("MHP", ex.Property);
        Assert.Equal("CON", ex.Undeclared);
    }

    [Fact]
    public void Get_DeclaredDependenciesInStrictMode_Works()
    {
        var bag = Fighter(strict: true);

        Assert.Equal(235, bag.Get("Rating"));
    }
}
