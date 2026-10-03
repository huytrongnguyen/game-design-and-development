namespace Course.PartyControl.Tests;

internal static class TestParties
{
    /// <summary>Characters at the origin, speed 5, skills "Q1".."Q4", "A1".., "Z1".. on the three hotbars.</summary>
    public static Party Create(int ownerId = 1, Vec2? at = null, int size = 3)
    {
        var start = at ?? new Vec2(0, 0);
        string[] prefixes = ["Q", "A", "Z", "1"];
        var characters = new List<Unit>();
        for (int i = 0; i < size; i++)
        {
            var unit = new Unit(ownerId * 10 + i, ownerId, $"Hero{i}", UnitKind.Character, start, speed: 5);
            for (int s = 0; s < Unit.SkillSlotCount; s++)
            {
                unit.SkillSlots[s] = $"{prefixes[i]}{s + 1}";
            }
            characters.Add(unit);
        }
        var pet = new Unit(ownerId * 10 + size, ownerId, "Pet", UnitKind.Pet, start, speed: 5);
        return new Party(ownerId, characters, pet, new PartyConfig { Size = size });
    }
}
