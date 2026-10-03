namespace Course.Quests;

/// <summary>Owns every live instance. The one place that creates them and, crucially,
/// the one place that destroys them, so a finished run cannot leak.</summary>
public sealed class MissionManager
{
    private readonly List<MissionInstance> _live = new();

    public int LiveCount => _live.Count;

    public MissionInstance Create(MissionDef def)
    {
        var instance = new MissionInstance(def);
        _live.Add(instance);
        return instance;
    }

    /// <summary>Advances all instances one tick, collects results, and destroys every ended run.</summary>
    public List<MissionResult> Tick(long nowTick)
    {
        var results = new List<MissionResult>();
        for (var i = _live.Count - 1; i >= 0; i--)
        {
            var m = _live[i];
            if (m.Tick(nowTick) is { } r) results.Add(r);
            if (m.State == MissionInstance.Phase.Ended)
            {
                m.Destroy();
                _live.RemoveAt(i);
            }
        }
        return results;
    }
}
