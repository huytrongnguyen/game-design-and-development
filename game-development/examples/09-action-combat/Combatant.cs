namespace Course.ActionCombat;

public sealed class Combatant(string id, int team, Vec2 position, double radius, int hp)
{
    public string Id { get; } = id;
    public int Team { get; } = team;
    public Vec2 Position { get; internal set; } = position;
    public double FacingDeg { get; set; }
    /// <summary>Hurtbox radius, also used for body collision.</summary>
    public double Radius { get; } = radius;
    public int Hp { get; internal set; } = hp;
    /// <summary>Ticks of rewind applied to targets when this combatant's hits are resolved.</summary>
    public int ViewLagTicks { get; set; }

    internal SkillDef? Action { get; set; }
    internal int ActionStartTick { get; set; }
}
