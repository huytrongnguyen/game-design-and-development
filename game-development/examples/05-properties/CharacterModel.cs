namespace Course.Properties;

/// <summary>
/// A tiny character model: the schema of the job table, the property schema (formulas plus declared dependencies),
/// and a helper that builds a live bag from a job row.
/// </summary>
public static class CharacterModel
{
    public static TableSchema JobTable { get; } = new("job",
    [
        new FieldSpec("name", FieldKind.Text),
        new FieldSpec("str", FieldKind.Number, Min: 1, Max: 200),
        new FieldSpec("dex", FieldKind.Number, Min: 1, Max: 200),
        new FieldSpec("con", FieldKind.Number, Min: 1, Max: 200),
        new FieldSpec("note", FieldKind.Text, Required: false),
    ]);

    /// <summary>Teaching formulas: MHP = CON*10 + Level*5, ATK = STR*2 + DEX, Rating = ATK + MHP/10. Real games put their own formulas here.</summary>
    public static PropertySchema CreateSchema()
    {
        return new PropertySchema()
            .Add(PropertyDefinition.Base("STR"))
            .Add(PropertyDefinition.Base("DEX"))
            .Add(PropertyDefinition.Base("CON"))
            .Add(PropertyDefinition.Base("Level"))
            .Add(PropertyDefinition.Calculated("MHP", b => b.Get("CON") * 10 + b.Get("Level") * 5, "CON", "Level"))
            .Add(PropertyDefinition.Calculated("ATK", b => b.Get("STR") * 2 + b.Get("DEX"), "STR", "DEX"))
            .Add(PropertyDefinition.Calculated("Rating", b => b.Get("ATK") + b.Get("MHP") / 10, "ATK", "MHP"));
    }

    public static PropertyBag Spawn(PropertySchema schema, DataRow job, int level, bool strict = false)
    {
        var bag = new PropertyBag(schema, strict);
        bag.Set("STR", job.GetNumber("str"));
        bag.Set("DEX", job.GetNumber("dex"));
        bag.Set("CON", job.GetNumber("con"));
        bag.Set("Level", level);
        return bag;
    }
}
