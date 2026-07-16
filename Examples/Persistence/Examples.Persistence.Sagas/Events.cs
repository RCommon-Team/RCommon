using RCommon.Models.Events;

namespace Examples.Persistence.Sagas;

public class PaymentReceivedEvent : ISerializableEvent
{
    public PaymentReceivedEvent(Guid orderId, string transactionId)
    {
        OrderId = orderId;
        TransactionId = transactionId;
    }

    public Guid OrderId { get; }
    public string TransactionId { get; }
}

public class InventoryConfirmedEvent : ISerializableEvent
{
    public InventoryConfirmedEvent(Guid orderId)
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}

public class ShipmentDispatchedEvent : ISerializableEvent
{
    public ShipmentDispatchedEvent(Guid orderId)
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}

public class OrderDeliveredEvent : ISerializableEvent
{
    public OrderDeliveredEvent(Guid orderId)
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}

// Not one of the explicitly mapped events -- OrderFulfillmentSaga.MapEventToTrigger's default
// switch arm maps any unrecognized event to StepFailed, demonstrating the compensation path.
public class InventoryUnavailableEvent : ISerializableEvent
{
    public InventoryUnavailableEvent(Guid orderId)
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}
