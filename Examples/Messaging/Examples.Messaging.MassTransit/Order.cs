using RCommon.Entities;

namespace Examples.Messaging.MassTransit;

/// <summary>
/// Durable domain event raised when an <see cref="Order"/> is confirmed. In recipe 2a it is declared
/// durable via <c>e.Publish&lt;OrderConfirmed&gt;()</c> + <c>e.UseRCommonOutbox("Orders")</c>, so on
/// commit it is staged into RCommon's <c>__OutboxMessages</c> table within the UnitOfWork's transaction.
/// A background poller later relays it to the MassTransit producer (broker) post-commit.
/// </summary>
public class OrderConfirmed : IDomainEvent
{
    public OrderConfirmed(Guid orderId, decimal total)
    {
        OrderId = orderId;
        Total = total;
    }

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    public Guid OrderId { get; }
    public decimal Total { get; }
}

/// <summary>
/// A DDD aggregate root. Confirming the order raises <see cref="OrderConfirmed"/> as a domain event.
/// </summary>
public class Order : AggregateRoot<Guid>
{
    public Order() : base(Guid.NewGuid())
    {
    }

    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }

    public void Confirm()
    {
        AddDomainEvent(new OrderConfirmed(Id, Total));
    }
}
