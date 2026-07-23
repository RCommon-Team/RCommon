using RCommon.Entities;

namespace Examples.EventHandling.TransactionScript;

/// <summary>
/// A plain CRUD entity for the transaction-script recipe. Unlike a DDD aggregate (see the Outbox
/// example's <c>Order</c>), <see cref="StockItem"/> has NO domain-event method — it does not raise
/// anything via <c>AddDomainEvent</c>. It is a BusinessEntity purely so the repository/tracker can
/// persist it; the integration event is enqueued DIRECTLY on the OutboxEventRouter by the
/// transaction-script service, not by the entity.
/// </summary>
public class StockItem : BusinessEntity<Guid>
{
    public StockItem() : base()
    {
        Id = Guid.NewGuid();
    }

    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
