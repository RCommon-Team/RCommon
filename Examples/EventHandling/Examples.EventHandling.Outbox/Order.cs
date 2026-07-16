using RCommon.Entities;

namespace Examples.EventHandling.Outbox;

public class OrderPlacedEvent : IDomainEvent
{
    public OrderPlacedEvent(Guid orderId, decimal total)
    {
        OrderId = orderId;
        Total = total;
    }

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    public Guid OrderId { get; }
    public decimal Total { get; }
}

public class Order : AggregateRoot<Guid>
{
    public Order() : base(Guid.NewGuid())
    {
    }

    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }

    public void Place()
    {
        AddDomainEvent(new OrderPlacedEvent(Id, Total));
    }
}
