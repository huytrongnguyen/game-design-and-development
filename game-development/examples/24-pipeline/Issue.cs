namespace M24.Pipeline;

/// <summary>One finding. <c>Location</c> is a path a content author can jump to, e.g. <c>items[item.sword].skill</c>.</summary>
public sealed record Issue(Severity Severity, string Location, string Message)
{
    public override string ToString() => $"{Severity}: {Location}: {Message}";
}
