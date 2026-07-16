using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;

namespace Examples.EventHandling.Outbox;

public class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The outbox message table is not created automatically -- AddOutboxMessages() maps
        // __OutboxMessages the same way EFCoreOutboxStoreTests configures its test DbContext.
        modelBuilder.AddOutboxMessages();
    }
}
