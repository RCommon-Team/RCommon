namespace Examples.Bootstrapping.MultiModule.Domain.Inventory;

/// <summary>
/// Minimal InventoryItem aggregate used by the Inventory module.
/// </summary>
public class InventoryItem
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
