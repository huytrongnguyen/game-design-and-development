namespace Course.Scripting;

/// <summary>Thrown at load time when content refers to something that does not exist.</summary>
public sealed class ContentValidationException : Exception
{
    public ContentValidationException(IReadOnlyList<string> problems)
        : base("Content is invalid:" + Environment.NewLine + string.Join(Environment.NewLine, problems))
    {
        Problems = problems;
    }

    public IReadOnlyList<string> Problems { get; }
}
