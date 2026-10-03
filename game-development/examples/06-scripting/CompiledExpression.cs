namespace Course.Scripting;

/// <summary>
/// A formula parsed once into a tree of delegates. Evaluating it only reads properties and does arithmetic:
/// no loops, no assignment, no calls out, so it cannot hang, allocate unboundedly or change game state.
/// </summary>
public sealed class CompiledExpression
{
    private readonly Func<IPropertyLookup, double> _eval;

    internal CompiledExpression(string source, Func<IPropertyLookup, double> eval, IReadOnlyList<string> variables)
    {
        Source = source;
        _eval = eval;
        Variables = variables;
    }

    public string Source { get; }

    /// <summary>Distinct property names the formula reads, so a loader can check them against the data schema.</summary>
    public IReadOnlyList<string> Variables { get; }

    public double Evaluate(IPropertyLookup properties) => _eval(properties);
}
