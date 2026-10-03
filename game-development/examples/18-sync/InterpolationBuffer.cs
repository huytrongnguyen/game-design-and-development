namespace Course.Sync;

/// <summary>
/// Client-side "render in the past" buffer. Position samples arrive at server ticks; the client draws
/// the entity at (now - delay), which normally lies between two received samples, and blends them.
/// It never extrapolates: past the newest sample the entity simply waits there.
/// </summary>
public sealed class InterpolationBuffer
{
    private readonly List<(double T, float X, float Y)> _samples = new();

    public int Count => _samples.Count;

    public void Add(double time, float x, float y)
    {
        _samples.Add((time, x, y));
        // Keep one sample older than anything we may still render, drop the rest.
        while (_samples.Count > 2 && _samples[1].T < time - 2.0) _samples.RemoveAt(0);
    }

    public bool TrySample(double renderTime, out float x, out float y)
    {
        x = y = 0;
        if (_samples.Count == 0) return false;
        var first = _samples[0];
        var last = _samples[^1];
        if (renderTime <= first.T) { x = first.X; y = first.Y; return true; }
        if (renderTime >= last.T) { x = last.X; y = last.Y; return true; }
        for (var i = 1; i < _samples.Count; i++)
        {
            var b = _samples[i];
            if (renderTime > b.T) continue;
            var a = _samples[i - 1];
            var f = (float)((renderTime - a.T) / (b.T - a.T));
            x = a.X + (b.X - a.X) * f;
            y = a.Y + (b.Y - a.Y) * f;
            return true;
        }
        return false;
    }
}
