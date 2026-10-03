namespace Course.Scripting;

public class ExpressionTests
{
    private static readonly MapProperties Hero = new(new Dictionary<string, double>
    {
        ["STR"] = 12, ["LV"] = 5, ["DEX"] = 7,
    });

    [Theory]
    [InlineData("STR * 2 + LV", 29)]
    [InlineData("(STR + LV) * 2", 34)]
    [InlineData("-STR + 20", 8)]
    [InlineData("STR / 5", 2.4)]
    [InlineData("17 % 5", 2)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("floor(STR / 5)", 2)]
    [InlineData("ceil(STR / 5)", 3)]
    [InlineData("min(STR, LV) + max(DEX, 10)", 15)]
    [InlineData("clamp(STR * 10, 0, 100)", 100)]
    [InlineData("abs(LV - STR)", 7)]
    public void Evaluate_ValidFormula_ReturnsExactValue(string formula, double expected)
    {
        var result = ExpressionParser.Parse(formula).Evaluate(Hero);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Variables_Formula_ListsEachPropertyOnce()
    {
        var expr = ExpressionParser.Parse("STR * 2 + LV + STR");
        Assert.Equal(new[] { "STR", "LV" }, expr.Variables);
    }

    [Theory]
    [InlineData("STR * ", "Unexpected end of formula")]
    [InlineData("(STR + 1", "Expected ')'")]
    [InlineData("STR $ 2", "Unexpected '$'")]
    [InlineData("sqrt(4)", "Unknown function 'sqrt'")]
    [InlineData("min(1)", "takes 2 argument(s), got 1")]
    [InlineData("1.2.3 + 1", "Bad number '1.2.3'")]
    public void Parse_InvalidFormula_ThrowsClearError(string formula, string expectedFragment)
    {
        var ex = Assert.Throws<ExpressionException>(() => ExpressionParser.Parse(formula));
        Assert.Contains(expectedFragment, ex.Message);
        Assert.Contains("position", ex.Message);
    }

    [Fact]
    public void Parse_DeeplyNestedParentheses_IsRejected()
    {
        var formula = new string('(', 100) + "1" + new string(')', 100);
        var ex = Assert.Throws<ExpressionException>(() => ExpressionParser.Parse(formula));
        Assert.Contains("nested too deeply", ex.Message);
    }

    [Fact]
    public void Evaluate_UnknownProperty_ThrowsNamingTheProperty()
    {
        var expr = ExpressionParser.Parse("STR + LUCK");
        var ex = Assert.Throws<ExpressionException>(() => expr.Evaluate(Hero));
        Assert.Equal("Unknown property 'LUCK'.", ex.Message);
    }

    [Fact]
    public void Evaluate_DivisionByZero_Throws()
    {
        var expr = ExpressionParser.Parse("STR / (LV - 5)");
        Assert.Throws<ExpressionException>(() => expr.Evaluate(Hero));
    }

    [Fact]
    public void Evaluate_SameInputRepeated_GivesIdenticalResults()
    {
        var expr = ExpressionParser.Parse("STR * 1.5 + LV / 3 - DEX % 4");
        var first = expr.Evaluate(Hero);
        for (var i = 0; i < 1000; i++)
            Assert.Equal(first, expr.Evaluate(Hero));
        Assert.Equal(12 * 1.5 + 5.0 / 3 - 3, first);
    }

    [Fact]
    public void Evaluate_AfterPropertyChange_UsesNewValuesWithoutReparsing()
    {
        var expr = ExpressionParser.Parse("STR * 2 + LV");
        var stronger = new MapProperties(new Dictionary<string, double> { ["STR"] = 20, ["LV"] = 5 });
        Assert.Equal(29, expr.Evaluate(Hero));
        Assert.Equal(45, expr.Evaluate(stronger));
    }
}
