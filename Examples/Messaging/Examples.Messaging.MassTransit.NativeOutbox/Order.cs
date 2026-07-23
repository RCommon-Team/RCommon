namespace Examples.Messaging.MassTransit.NativeOutbox;

/// <summary>
/// A simple business aggregate for recipe 2b. Unlike recipe 2a, the integration event is NOT an RCommon
/// domain event staged in RCommon's own outbox — it is published through MassTransit's <c>IPublishEndpoint</c>
/// and staged by MassTransit's NATIVE EF Core bus outbox (an <c>OutboxMessage</c> row) within the same
/// transaction as this row.
/// </summary>
public sealed class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

/// <summary>
/// Integration event published through MassTransit for recipe 2b. With <c>UseBusOutbox()</c> (configured by
/// RCommon's <c>UseBrokerOutbox&lt;AppDbContext&gt;</c> wrapper), publishing this via a scoped
/// <c>IPublishEndpoint</c> does not hit the broker; it stages a MassTransit <c>OutboxMessage</c> row during
/// the DbContext's SaveChanges, which enlists in RCommon's ambient UnitOfWork TransactionScope.
/// </summary>
public sealed record OrderConfirmed(Guid OrderId, string CustomerName, decimal Total);
