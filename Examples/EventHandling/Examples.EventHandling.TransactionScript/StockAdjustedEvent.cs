using RCommon.Models.Events;

namespace Examples.EventHandling.TransactionScript;

/// <summary>
/// Integration event for the transaction-script recipe. It is a plain <see cref="ISyncEvent"/>
/// (serializable) — NOT an <c>IDomainEvent</c> raised by an aggregate. The transaction-script service
/// constructs and enqueues this directly on the OutboxEventRouter within the UnitOfWork.
/// </summary>
public class StockAdjustedEvent : ISyncEvent
{
    public StockAdjustedEvent(Guid stockItemId, string sku, int quantity)
    {
        StockItemId = stockItemId;
        Sku = sku;
        Quantity = quantity;
    }

    public Guid StockItemId { get; }
    public string Sku { get; }
    public int Quantity { get; }
}
