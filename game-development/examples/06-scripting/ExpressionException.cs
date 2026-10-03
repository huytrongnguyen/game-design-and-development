namespace Course.Scripting;

/// <summary>A formula could not be parsed or evaluated. The message says what and, for parse errors, where.</summary>
public sealed class ExpressionException : Exception
{
    public ExpressionException(string message) : base(message) { }
}
