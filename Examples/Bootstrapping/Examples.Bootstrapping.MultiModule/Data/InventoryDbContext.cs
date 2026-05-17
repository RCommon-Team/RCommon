using Examples.Bootstrapping.MultiModule.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;

namespace Examples.Bootstrapping.MultiModule.Data;

/// <summary>
/// DbContext for the Inventory module. A separate type from <see cref="OrderingDbContext"/> so that
/// both can coexist under distinct data-store names without conflicting.
/// </summary>
public class InventoryDbContext : RCommonDbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<InventoryItem> Items => Set<InventoryItem>();
}
