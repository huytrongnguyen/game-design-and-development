namespace Course.MiniEngine;

/// <summary>
/// The module's worked scenario: two entities moving in a straight line, one timer that fires partway
/// through, and one seeded random walker — just enough to exercise every piece (loop, store, systems,
/// events, data loading) at once. The tests assert exact numbers against this JSON.
/// </summary>
public static class DemoScenario
{
    /// <summary>Entity order after loading: [0] linear mover A, [1] linear mover B, [2] timer "Bell", [3] random walker.</summary>
    public const string Json = """
    [
        { "position": { "x": 0,  "y": 0 }, "velocity": { "dx": 1, "dy": 0 } },
        { "position": { "x": 10, "y": 5 }, "velocity": { "dx": 0, "dy": -2 } },
        { "timer": { "ticksRemaining": 3, "eventName": "Bell" } },
        { "position": { "x": 0,  "y": 0 }, "randomWalker": true }
    ]
    """;
}
