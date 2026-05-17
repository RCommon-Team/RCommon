namespace Examples.Bootstrapping.MultiModule.Domain.Orders;

/// <summary>
/// Minimal Order aggregate used by the Ordering module. Persistence configuration is
/// intentionally lightweight; the example focuses on bootstrapping semantics, not domain depth.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;
}
