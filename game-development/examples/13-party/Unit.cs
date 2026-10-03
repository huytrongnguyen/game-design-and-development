namespace Course.PartyControl;

/// <summary>
/// One entity in the world. A player's party is several of these (its characters and a pet),
/// all carrying the same OwnerId so the zone can treat them as ordinary entities.
/// </summary>
public sealed class Unit
{
    public const int SkillSlotCount = 4;

    public Unit(int id, int ownerId, string name, UnitKind kind, Vec2 position, double speed)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Kind = kind;
        Position = position;
        Speed = speed;
    }

    public int Id { get; }
    public int OwnerId { get; }
    public string Name { get; }
    public UnitKind Kind { get; }
    public double Speed { get; }

    public Vec2 Position { get; set; }
    public double HeadingDegrees { get; set; }
    public Vec2? Destination { get; set; }

    /// <summary>In the field line-up. Inactive characters take no part in group orders.</summary>
    public bool Active { get; set; } = true;

    /// <summary>False while the player drives this unit directly instead of letting it follow.</summary>
    public bool FollowsLeader { get; set; } = true;

    public string Stance { get; set; } = "Default";

    /// <summary>This character's own hotbar. A null entry is an empty slot.</summary>
    public string?[] SkillSlots { get; } = new string?[SkillSlotCount];
}
