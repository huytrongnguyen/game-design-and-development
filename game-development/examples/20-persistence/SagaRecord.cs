namespace Course.Persistence;

/// <summary>The saga's own durable progress marker, so a restarted server knows where to resume.</summary>
public sealed record SagaRecord(string Id, TradeOrder Order, SagaStatus Status);
