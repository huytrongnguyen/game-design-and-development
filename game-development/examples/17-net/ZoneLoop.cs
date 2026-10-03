using System.Threading.Channels;

namespace M17.Net;

/// <summary>
/// The authoritative simulation of one zone. Network threads only <see cref="Enqueue"/> intents;
/// everything else happens inside <see cref="Tick"/>, on one thread, in a fixed order.
/// No sockets, clocks or randomness in here, so it is trivial to test.
/// </summary>
public sealed class ZoneLoop
{
    public const double WorldSize = 100;
    public const double Speed = 2.0;
    public const double SpawnX = 50;
    public const double SpawnY = 50;
    public const int MaxChatLength = 200;

    private readonly Channel<Intent> _queue = Channel.CreateBounded<Intent>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    private readonly SortedDictionary<int, Player> _players = [];
    private long _tick;

    public long CurrentTick => Interlocked.Read(ref _tick);

    /// <summary>Thread-safe. Returns false if the queue is full and the intent was dropped.</summary>
    public bool Enqueue(Intent intent) => _queue.Writer.TryWrite(intent);

    public IReadOnlyList<ZoneOutput> Tick()
    {
        var tick = Interlocked.Increment(ref _tick);
        var outputs = new List<ZoneOutput>();
        var membershipChanged = false;

        while (_queue.Reader.TryRead(out var intent))
        {
            switch (intent.Kind)
            {
                case IntentKind.Join:
                    _players[intent.PlayerId] = new Player(intent.PlayerId, intent.Name);
                    membershipChanged = true;
                    break;
                case IntentKind.Leave:
                    membershipChanged |= _players.Remove(intent.PlayerId);
                    break;
                case IntentKind.Move when _players.TryGetValue(intent.PlayerId, out var mover):
                    mover.SetTarget(
                        Math.Clamp(intent.X, 0, WorldSize),
                        Math.Clamp(intent.Y, 0, WorldSize));
                    break;
                case IntentKind.Chat when _players.TryGetValue(intent.PlayerId, out var speaker):
                    var text = intent.Text.Length > MaxChatLength ? intent.Text[..MaxChatLength] : intent.Text;
                    outputs.Add(new ZoneOutput(Ops.Chat, new ChatBroadcast(speaker.Name, text)));
                    break;
            }
        }

        var moved = new List<PlayerView>();
        foreach (var player in _players.Values)
        {
            if (player.Step(Speed))
            {
                moved.Add(player.ToView());
            }
        }

        if (membershipChanged)
        {
            var everyone = _players.Values.Select(p => p.ToView()).ToList();
            outputs.Insert(0, new ZoneOutput(Ops.State, new StatePayload(tick, true, everyone)));
        }
        else if (moved.Count > 0)
        {
            outputs.Insert(0, new ZoneOutput(Ops.State, new StatePayload(tick, false, moved)));
        }

        return outputs;
    }

    private sealed class Player(int id, string name)
    {
        private double _x = SpawnX;
        private double _y = SpawnY;
        private double _targetX = SpawnX;
        private double _targetY = SpawnY;

        public string Name => name;

        public void SetTarget(double x, double y)
        {
            _targetX = x;
            _targetY = y;
        }

        /// <summary>Advance toward the target by at most <paramref name="speed"/>. True if the position changed.</summary>
        public bool Step(double speed)
        {
            var dx = _targetX - _x;
            var dy = _targetY - _y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance == 0)
            {
                return false;
            }

            if (distance <= speed)
            {
                _x = _targetX;
                _y = _targetY;
            }
            else
            {
                _x += dx / distance * speed;
                _y += dy / distance * speed;
            }

            return true;
        }

        public PlayerView ToView() => new(id, name, Math.Round(_x, 2), Math.Round(_y, 2));
    }
}
