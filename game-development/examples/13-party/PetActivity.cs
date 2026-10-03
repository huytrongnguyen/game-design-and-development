namespace Course.PartyControl;

/// <summary>
/// A pet's stamina. Each helpful action (picking up loot, casting a small buff) spends a fixed cost,
/// and the pet refuses to act once the pool falls to the floor. Feeding refills it.
/// </summary>
public sealed class PetActivity
{
    private readonly PetStaminaConfig _config;

    public PetActivity(PetStaminaConfig? config = null, int? value = null)
    {
        _config = config ?? new PetStaminaConfig();
        Value = Math.Clamp(value ?? _config.Max, 0, _config.Max);
    }

    public int Max => _config.Max;
    public int Value { get; private set; }

    public bool TryUse()
    {
        if (Value <= _config.Floor)
        {
            return false;
        }
        Value -= _config.ActionCost;
        return true;
    }

    public void Feed(int amount) => Value = Math.Clamp(Value + amount, 0, _config.Max);
}
