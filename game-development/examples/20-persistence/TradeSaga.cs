namespace Course.Persistence;

/// <summary>A two-step trade as a saga: (1) move the items into escrow, (2) settle gold and
/// items in one atomic transaction. If step 2 is rejected, a compensating transaction returns
/// the items. Every step has a fixed operation id and the saga's progress is persisted after
/// each step, so after a crash <see cref="Recover"/> simply continues; repeated steps are no-ops.</summary>
public sealed class TradeSaga
{
    private readonly IGameStore _store;
    private readonly Action<string>? _afterStep;

    /// <param name="afterStep">Test hook called after each step with its name; throwing simulates a crash.</param>
    public TradeSaga(IGameStore store, Action<string>? afterStep = null)
    {
        _store = store;
        _afterStep = afterStep;
    }

    public static string EscrowId(string orderId) => $"escrow:{orderId}";

    public SagaStatus Run(TradeOrder order)
    {
        if (_store.FindSaga(order.OrderId) is null)
            _store.SaveSaga(new SagaRecord(order.OrderId, order, SagaStatus.Started));
        return Recover(order.OrderId);
    }

    public SagaStatus Recover(string orderId)
    {
        var saga = _store.FindSaga(orderId) ?? throw new InvalidOperationException($"unknown saga {orderId}");
        var o = saga.Order;
        var escrow = EscrowId(orderId);

        while (saga.Status is SagaStatus.Started or SagaStatus.Escrowed or SagaStatus.Compensating)
        {
            switch (saga.Status)
            {
                case SagaStatus.Started:
                    _store.CreatePlayer(PlayerRecord.New(escrow));
                    var r1 = _store.Apply($"{orderId}:escrow", "trade.escrow",
                        [new(o.Seller, ItemId: o.Item, ItemDelta: -o.Quantity),
                         new(escrow, ItemId: o.Item, ItemDelta: o.Quantity)]);
                    _afterStep?.Invoke("escrow-applied");
                    saga = Save(saga, r1.Ok ? SagaStatus.Escrowed : SagaStatus.Failed);
                    _afterStep?.Invoke("escrow-saved");
                    break;

                case SagaStatus.Escrowed:
                    var r2 = _store.Apply($"{orderId}:settle", "trade.settle",
                        [new(escrow, ItemId: o.Item, ItemDelta: -o.Quantity),
                         new(o.Buyer, GoldDelta: -o.Price, ItemId: o.Item, ItemDelta: o.Quantity),
                         new(o.Seller, GoldDelta: o.Price)]);
                    _afterStep?.Invoke("settle-applied");
                    saga = Save(saga, r2.Ok ? SagaStatus.Completed : SagaStatus.Compensating);
                    if (saga.Status == SagaStatus.Compensating) _afterStep?.Invoke("compensating-saved");
                    break;

                case SagaStatus.Compensating:
                    _store.Apply($"{orderId}:refund", "trade.refund",
                        [new(escrow, ItemId: o.Item, ItemDelta: -o.Quantity),
                         new(o.Seller, ItemId: o.Item, ItemDelta: o.Quantity)]);
                    _afterStep?.Invoke("refund-applied");
                    saga = Save(saga, SagaStatus.Compensated);
                    break;
            }
        }
        return saga.Status;
    }

    private SagaRecord Save(SagaRecord saga, SagaStatus next)
    {
        var updated = saga with { Status = next };
        _store.SaveSaga(updated);
        return updated;
    }
}
