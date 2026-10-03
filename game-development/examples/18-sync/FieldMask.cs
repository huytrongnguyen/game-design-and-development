namespace Course.Sync;

/// <summary>Which replicated fields (components) an update carries.</summary>
[Flags]
public enum FieldMask : byte
{
    None = 0,
    Position = 1,
    Hp = 2,
    Appearance = 4,
    All = Position | Hp | Appearance,
}
