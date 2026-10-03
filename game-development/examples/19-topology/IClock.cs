namespace Course.Topology;

/// <summary>Injected time source (whole seconds). Services never read the system clock.</summary>
public interface IClock
{
    long NowSeconds { get; }
}
