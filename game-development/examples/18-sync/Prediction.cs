namespace Course.Sync;

/// <summary>One player input: a numbered movement request. The number lets the server say "I have applied up to N".</summary>
public readonly record struct MoveInput(int Seq, float Dx, float Dy);

/// <summary>The server's answer: the last input it applied and the authoritative position after it.</summary>
public readonly record struct MoveAck(int LastSeq, float X, float Y);

/// <summary>
/// The movement rules, shared by client and server. Prediction only works when both sides run the same
/// pure function on the same inputs: (position, input) in, position out, no hidden state.
/// </summary>
public static class MoveRules
{
    public const float WorldSize = 100f;

    public static (float X, float Y) Step(float x, float y, MoveInput input, float speed)
    {
        var nx = Math.Clamp(x + input.Dx * speed, 0f, WorldSize);
        var ny = Math.Clamp(y + input.Dy * speed, 0f, WorldSize);
        return (nx, ny);
    }
}

/// <summary>The authoritative side: applies inputs in order and acknowledges each one.</summary>
public sealed class AuthoritativeMover
{
    public AuthoritativeMover(float x, float y, float speed) { X = x; Y = y; Speed = speed; }

    public float X { get; private set; }
    public float Y { get; private set; }

    /// <summary>The server's speed can differ from what the client assumes (a slow effect, a rule the client did not know).</summary>
    public float Speed { get; set; }

    public MoveAck Process(MoveInput input)
    {
        (X, Y) = MoveRules.Step(X, Y, input, Speed);
        return new MoveAck(input.Seq, X, Y);
    }
}

/// <summary>
/// Client-side prediction with reconciliation. The client applies each input locally at once, keeps it in a
/// pending list, and sends it. When an acknowledgement arrives it (1) drops the inputs the server has
/// applied, (2) restarts from the server's position and (3) replays the inputs still in flight.
/// If the prediction was right, nothing visibly changes; if not, the client is pulled to the server's truth.
/// </summary>
public sealed class PredictedClient
{
    private readonly List<MoveInput> _pending = new();
    private readonly float _speed;
    private int _nextSeq = 1;

    public PredictedClient(float x, float y, float speed) { X = x; Y = y; _speed = speed; }

    public float X { get; private set; }
    public float Y { get; private set; }
    public int PendingCount => _pending.Count;

    /// <summary>Distance moved by the most recent reconciliation that disagreed with the prediction.</summary>
    public float LastCorrection { get; private set; }

    /// <summary>Apply a new input locally right now and return it, ready to send.</summary>
    public MoveInput Predict(float dx, float dy)
    {
        var input = new MoveInput(_nextSeq++, dx, dy);
        (X, Y) = MoveRules.Step(X, Y, input, _speed);
        _pending.Add(input);
        return input;
    }

    public void Reconcile(MoveAck ack)
    {
        var beforeX = X;
        var beforeY = Y;
        _pending.RemoveAll(i => i.Seq <= ack.LastSeq);
        (X, Y) = (ack.X, ack.Y);
        foreach (var input in _pending) (X, Y) = MoveRules.Step(X, Y, input, _speed);
        var dx = X - beforeX;
        var dy = Y - beforeY;
        LastCorrection = MathF.Sqrt(dx * dx + dy * dy);
    }
}
