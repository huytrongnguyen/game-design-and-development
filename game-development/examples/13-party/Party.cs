namespace Course.PartyControl;

/// <summary>
/// Everything one player owns in the field: a configured number of characters and one non-combat pet.
/// Exactly one character is the leader and receives move orders. The others follow a formation
/// slot unless the player takes direct control of them.
/// </summary>
public sealed class Party
{
    private readonly Unit[] _characters;
    private readonly Dictionary<int, Squad> _squads = [];

    public Party(int ownerId, IEnumerable<Unit> characters, Unit pet, PartyConfig? config = null, KeyMap? keys = null)
    {
        Config = config ?? new PartyConfig();
        _characters = characters.ToArray();
        if (_characters.Length != Config.Size || Config.Size < 1)
        {
            throw new ArgumentException($"This party fields exactly {Config.Size} characters.", nameof(characters));
        }
        OwnerId = ownerId;
        Pet = pet;
        Formation = Config.Formation ?? Formation.WedgeOf(Config.Size - 1);
        Keys = keys ?? KeyMap.Default(Config.Size);
        PetStamina = new PetActivity(Config.PetStamina);
        CatchUpDistance = Config.CatchUpDistance;
        LootRadius = Config.LootRadius;
    }

    public int OwnerId { get; }
    public Unit Pet { get; }
    public PartyConfig Config { get; }
    public PetActivity PetStamina { get; }
    public int Size => Config.Size;
    public Formation Formation { get; set; }
    public KeyMap Keys { get; }
    public int LeaderIndex { get; private set; }

    /// <summary>A follower farther than this from its slot snaps to it instead of walking.</summary>
    public double CatchUpDistance { get; set; }

    /// <summary>Pet pick-up radius around the pet.</summary>
    public double LootRadius { get; set; }

    public IReadOnlyList<Unit> Characters => _characters;
    public Unit Leader => _characters[LeaderIndex];

    /// <summary>All the entities the zone simulates for this player.</summary>
    public IEnumerable<Unit> Units => _characters.Append(Pet);

    public bool TrySwitchLeader(int index)
    {
        if (index < 0 || index >= Size || !_characters[index].Active)
        {
            return false;
        }
        LeaderIndex = index;
        foreach (var unit in Units)
        {
            unit.Destination = null;
        }
        return true;
    }

    /// <summary>A ground click: only the leader receives it. The others follow by themselves.</summary>
    public void Move(Vec2 destination) => Leader.Destination = destination;

    /// <summary>Direct control of a non-leader. It stops following until Regroup.</summary>
    public bool TryMoveUnit(int index, Vec2 destination)
    {
        if (index < 0 || index >= Size || index == LeaderIndex)
        {
            return false;
        }
        var unit = _characters[index];
        unit.FollowsLeader = false;
        unit.Destination = destination;
        return true;
    }

    /// <summary>Calls every detached character back into formation.</summary>
    public void Regroup()
    {
        foreach (var unit in _characters)
        {
            unit.FollowsLeader = true;
            if (unit != Leader)
            {
                unit.Destination = null;
            }
        }
    }

    /// <summary>Where the follower at this roster index wants to stand right now.</summary>
    public Vec2 SlotFor(int characterIndex)
    {
        // Slot order follows roster order, skipping the leader, so a slot never changes
        // just because another character is detached.
        int ordinal = characterIndex < LeaderIndex ? characterIndex : characterIndex - 1;
        return Formation.FollowerSlot(ordinal, Leader.Position, Leader.HeadingDegrees);
    }

    public Vec2 PetSlot() => Formation.PetSlot(Leader.Position, Leader.HeadingDegrees);

    public bool TryUseSkill(char key, out SkillCast cast)
    {
        cast = default;
        if (!Keys.TryResolve(key, out var target))
        {
            return false;
        }
        var character = _characters[target.Character];
        string? skill = character.SkillSlots[target.Slot];
        if (!character.Active || skill is null)
        {
            return false;
        }
        cast = new SkillCast(target.Character, skill);
        return true;
    }

    /// <summary>The pet picks up the nearest item inside its radius, if it has stamina left.</summary>
    public GroundItem? TryPetLoot(IEnumerable<GroundItem> items)
    {
        GroundItem? best = null;
        double bestDistance = LootRadius;
        foreach (var item in items)
        {
            double d = Pet.Position.DistanceTo(item.Position);
            if (d <= bestDistance)
            {
                best = item;
                bestDistance = d;
            }
        }
        return best is not null && PetStamina.TryUse() ? best : null;
    }

    public bool TrySaveSquad(int slot)
    {
        if (slot < 1 || slot > Config.MaxSquads)
        {
            return false;
        }
        _squads[slot] = new Squad(
            LeaderIndex,
            _characters.Select(c => c.Active).ToArray(),
            _characters.Select(c => c.Stance).ToArray());
        return true;
    }

    public bool TryRecallSquad(int slot)
    {
        if (!_squads.TryGetValue(slot, out var squad))
        {
            return false;
        }
        for (int i = 0; i < Size; i++)
        {
            _characters[i].Active = squad.Active[i];
            _characters[i].Stance = squad.Stances[i];
            _characters[i].FollowsLeader = true;
        }
        return TrySwitchLeader(squad.LeaderIndex);
    }

    public void Tick(double dt)
    {
        for (int i = 0; i < Size; i++)
        {
            var unit = _characters[i];
            if (i == LeaderIndex || !unit.FollowsLeader)
            {
                WalkToDestination(unit, dt);
            }
        }
        for (int i = 0; i < Size; i++)
        {
            if (i != LeaderIndex && _characters[i].Active && _characters[i].FollowsLeader)
            {
                Follow(_characters[i], SlotFor(i), dt);
            }
        }
        Follow(Pet, PetSlot(), dt);
    }

    private static void WalkToDestination(Unit unit, double dt)
    {
        if (unit.Destination is { } target && StepToward(unit, target, dt))
        {
            unit.Destination = null;
        }
    }

    private void Follow(Unit unit, Vec2 slot, double dt)
    {
        if (unit.Position.DistanceTo(slot) > CatchUpDistance)
        {
            unit.Position = slot;
            return;
        }
        StepToward(unit, slot, dt);
    }

    private static bool StepToward(Unit unit, Vec2 target, double dt)
    {
        var delta = target - unit.Position;
        double distance = delta.Length;
        double step = unit.Speed * dt;
        if (distance <= step)
        {
            unit.Position = target;
            return true;
        }
        unit.HeadingDegrees = Math.Atan2(delta.Y, delta.X) * 180.0 / Math.PI;
        unit.Position += delta * (step / distance);
        return false;
    }
}
