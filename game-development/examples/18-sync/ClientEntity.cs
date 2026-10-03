namespace Course.Sync;

/// <summary>The client's replica of one visible entity.</summary>
public sealed class ClientEntity
{
    public ClientEntity(EntityId id) => Id = id;

    public EntityId Id { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public int Hp { get; set; }
    public int Appearance { get; set; }
    public InterpolationBuffer Path { get; } = new();
}
