using System.Collections.Concurrent;

namespace M17.Net;

public sealed class SessionRegistry
{
    private readonly ConcurrentDictionary<string, ClientSession> _sessions = new();

    public int Count => _sessions.Count;

    public void Add(ClientSession session) => _sessions[session.Id] = session;

    public void Remove(ClientSession session) => _sessions.TryRemove(session.Id, out _);

    public void Broadcast(string json)
    {
        foreach (var session in _sessions.Values)
        {
            session.TrySend(json);
        }
    }
}
