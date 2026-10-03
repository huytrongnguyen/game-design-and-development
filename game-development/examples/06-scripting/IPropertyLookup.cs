namespace Course.Scripting;

/// <summary>Read-only source of named numeric properties. Formulas are evaluated against one of these.</summary>
public interface IPropertyLookup
{
    bool TryGet(string name, out double value);
}
