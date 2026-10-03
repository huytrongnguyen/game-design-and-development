namespace Course.MiniEngine;

/// <summary>Counts down by one tick each Step; fires <see cref="EventName"/> as a GameEvent when it reaches zero.</summary>
public readonly record struct Timer(int TicksRemaining, string EventName);
