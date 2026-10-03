namespace Course.Matchmaking;

/// <summary>A formed group. InstanceId stays null until an instance slot is allocated.</summary>
public sealed class Party
{
    public Party(string id, IEnumerable<QueueEntry> members) { Id = id; Members = members.ToList(); }

    public string Id { get; }
    public List<QueueEntry> Members { get; }
    public string? InstanceId { get; internal set; }
    public bool Started => InstanceId != null;
}
