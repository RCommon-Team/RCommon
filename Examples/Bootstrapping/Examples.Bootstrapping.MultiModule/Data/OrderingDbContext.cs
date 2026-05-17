using Examples.Bootstrapping.MultiModule.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;

namespace Examples.Bootstrapping.MultiModule.Data;

/// <summary>
/// DbContext for the Ordering module. Inherits directly from <see cref="RCommonDbContext"/> so that
/// <c>DataStoreFactoryOptions.Register&lt;RCommonDbContext, OrderingDbContext&gt;("Ordering")</c> wires up correctly.
/// </summary>
public class OrderingDbContext : RCommonDbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
}
