namespace Course.Scripting;

/// <summary>
/// The engine API a hook is allowed to use. It is the "binding surface": a hook cannot touch
/// <see cref="PlayerState"/> directly, so every mutation goes through a small, auditable set of methods.
/// </summary>
public sealed class HookContext
{
    private readonly PlayerState _player;

    public HookContext(PlayerState player, IPropertyLookup properties)
    {
        _player = player;
        Properties = properties;
    }

    /// <summary>Read-only numeric properties of the acting character (STR, LV, ...).</summary>
    public IPropertyLookup Properties { get; }

    public void GrantGold(long amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Use a spend method for negative amounts.");
        _player.Gold += amount;
    }

    public void GrantItem(string itemId, int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        _player.AddItem(itemId, count);
    }
}
