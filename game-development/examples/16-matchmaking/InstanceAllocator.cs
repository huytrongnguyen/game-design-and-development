namespace Course.Matchmaking;

/// <summary>A fixed pool of instance slots. Real systems ask a fleet manager; the contract is the same:
/// give me a slot or tell me there is none, and take it back when the run ends.</summary>
public sealed class InstanceAllocator
{
    private readonly Queue<string> _free = new();

    public InstanceAllocator(int capacity)
    {
        for (var i = 1; i <= capacity; i++) _free.Enqueue($"inst-{i}");
    }

    public int FreeCount => _free.Count;

    public string? TryAllocate() => _free.Count > 0 ? _free.Dequeue() : null;

    public void Release(string id) => _free.Enqueue(id);
}
