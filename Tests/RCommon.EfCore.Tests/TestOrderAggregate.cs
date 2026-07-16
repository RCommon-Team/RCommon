using RCommon.Entities;

namespace RCommon.EfCore.Tests;

/// <summary>
/// Test aggregate root with a child collection navigation, used to reproduce and guard against
/// the UpdateAsync new-child misclassification bug (docs/superpowers/specs/2026-03-17-aggregate-repository-design.md addendum).
/// </summary>
public class TestOrderAggregate : AggregateRoot<Guid>
{
    public string? CustomerName { get; set; }

    public ICollection<TestOrderLineItem> LineItems { get; set; } = new List<TestOrderLineItem>();

    public TestOrderAggregate() : base()
    {
    }

    public TestOrderAggregate(Guid id, string customerName) : base(id)
    {
        CustomerName = customerName;
    }

    /// <summary>
    /// Adds a new line item to this order. The line item's Id is expected to already be set by the
    /// caller (simulating RCommon's own recommended client-generated sequential-GUID strategy).
    /// </summary>
    public void AddLineItem(TestOrderLineItem lineItem)
    {
        lineItem.OrderId = Id;
        LineItems.Add(lineItem);
    }
}

/// <summary>
/// Child entity within the <see cref="TestOrderAggregate"/> boundary. Derives from
/// <see cref="BusinessEntity{TKey}"/> (not <see cref="DomainEntity{TKey}"/>) so it can also be used
/// directly via <c>ILinqRepository&lt;TestOrderLineItem&gt;</c>, matching the real-world workaround
/// pattern documented in item 13 of the consumer feedback review.
/// </summary>
public class TestOrderLineItem : BusinessEntity<Guid>
{
    public Guid OrderId { get; set; }
    public string? ProductName { get; set; }

    public TestOrderLineItem() : base()
    {
    }

    public TestOrderLineItem(Guid id, string productName) : base(id)
    {
        ProductName = productName;
    }
}
