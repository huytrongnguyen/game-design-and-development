namespace M12.Ai;

/// <summary>A minimal fighter: a player character or an ally that monsters can target.</summary>
public sealed class Unit(int id, Vec2 position, int maxHp)
{
    public int Id { get; } = id;
    public Vec2 Position { get; set; } = position;
    public int MaxHp { get; } = maxHp;
    public int Hp { get; private set; } = maxHp;
    public bool IsAlive => Hp > 0;
    public double HpFraction => (double)Hp / MaxHp;

    public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);

    public void Heal(int amount)
    {
        if (IsAlive) Hp = Math.Min(MaxHp, Hp + amount);
    }

    public void SetHp(int hp) => Hp = Math.Clamp(hp, 0, MaxHp);
}
