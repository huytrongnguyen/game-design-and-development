namespace Course.Combat;

public class ModifierStackTests
{
    [Fact]
    public void Evaluate_NoModifiers_ReturnsBase() =>
        Assert.Equal(100, new ModifierStack().Evaluate(100));

    [Fact]
    public void Evaluate_FlatThenPercentThenFinalFlat_AppliesLayersInOrder()
    {
        var stack = new ModifierStack();
        stack.Add(new("ring", ModifierLayer.Flat, 20));          // base 100 -> 120
        stack.Add(new("belt", ModifierLayer.PercentAdd, 10));
        stack.Add(new("buff", ModifierLayer.PercentAdd, 10));    // +20% total -> 144
        stack.Add(new("set", ModifierLayer.FinalFlat, 500));     // -> 644
        Assert.Equal(644, stack.Evaluate(100), 6);
    }

    [Fact]
    public void Evaluate_SamePercentsAsSeparateMultipliers_CompoundInsteadOfAdding()
    {
        var additive = new ModifierStack();
        additive.Add(new("a", ModifierLayer.PercentAdd, 50));
        additive.Add(new("b", ModifierLayer.PercentAdd, 50));
        var multiplicative = new ModifierStack();
        multiplicative.Add(new("a", ModifierLayer.PercentMult, 50));
        multiplicative.Add(new("b", ModifierLayer.PercentMult, 50));
        Assert.Equal(200, additive.Evaluate(100), 6);
        Assert.Equal(225, multiplicative.Evaluate(100), 6);
    }

    [Fact]
    public void Evaluate_FlatBonusBeforeVersusAfterPercent_GivesDifferentResults()
    {
        var before = new ModifierStack();
        before.Add(new("x", ModifierLayer.Flat, 100));
        before.Add(new("y", ModifierLayer.PercentAdd, 100));
        var after = new ModifierStack();
        after.Add(new("x", ModifierLayer.FinalFlat, 100));
        after.Add(new("y", ModifierLayer.PercentAdd, 100));
        Assert.Equal(400, before.Evaluate(100), 6); // (100+100) * 2
        Assert.Equal(300, after.Evaluate(100), 6);  // 100 * 2 + 100
    }

    [Fact]
    public void Evaluate_MaxHpWithTenPercentAndFlat_Is639()
    {
        // MaxHp(20, 30) = 490; * 1.10 = 539, then +100 flat from equipment.
        var stack = new ModifierStack();
        stack.Add(new("armor", ModifierLayer.PercentAdd, 10));
        stack.Add(new("ring", ModifierLayer.FinalFlat, 100));
        Assert.Equal(639, stack.Evaluate(new StatFormulas(new CombatRules()).MaxHp(20, 30)), 6);
    }

    [Fact]
    public void RemoveSource_UnequippingAnItem_RemovesOnlyItsModifiers()
    {
        var stack = new ModifierStack();
        stack.Add(new("sword", ModifierLayer.Flat, 30));
        stack.Add(new("sword", ModifierLayer.PercentAdd, 10));
        stack.Add(new("ring", ModifierLayer.Flat, 5));
        Assert.Equal(2, stack.RemoveSource("sword"));
        Assert.Equal(105, stack.Evaluate(100), 6);
    }

    [Fact]
    public void CachedStat_ReadManyTimes_ComputesOnceUntilSomethingChanges()
    {
        var formulas = new StatFormulas(new CombatRules());
        int vitality = 30;
        var maxHp = new CachedStat(() => formulas.MaxHp(20, vitality));
        for (int i = 0; i < 1000; i++) _ = maxHp.Value;
        Assert.Equal(1, maxHp.RecomputeCount);
        Assert.Equal(490, maxHp.Value);

        maxHp.Modifiers.Add(new("ring", ModifierLayer.FinalFlat, 100));
        Assert.Equal(590, maxHp.Value);
        Assert.Equal(2, maxHp.RecomputeCount);

        vitality = 40;       // an input changed: the owner must say so
        maxHp.MarkDirty();
        Assert.Equal(formulas.MaxHp(20, 40) + 100, maxHp.Value); // 570 + 100
        Assert.Equal(3, maxHp.RecomputeCount);
    }
}
