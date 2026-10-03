namespace Course.PartyControl;

/// <summary>
/// Everything that makes one game's party different from another's. A single-character game with
/// a companion uses Size = 1; a three-character party game uses Size = 3. All values are data.
/// </summary>
public sealed record PartyConfig
{
    /// <summary>How many characters the player fields at once.</summary>
    public int Size { get; init; } = 3;

    /// <summary>Saved line-up slots (1..MaxSquads).</summary>
    public int MaxSquads { get; init; } = 3;

    /// <summary>A follower farther than this from its slot snaps to it instead of walking.</summary>
    public double CatchUpDistance { get; init; } = 20;

    /// <summary>Companion pick-up radius around the pet.</summary>
    public double LootRadius { get; init; } = 5;

    /// <summary>Null means a wedge sized for Size - 1 followers.</summary>
    public Formation? Formation { get; init; }

    public PetStaminaConfig PetStamina { get; init; } = new();
}

/// <summary>A pet's stamina pool: a maximum, a cost per helpful action and a floor below which it refuses to act.</summary>
public sealed record PetStaminaConfig(int Max = 1000, int ActionCost = 10, int Floor = 100);
