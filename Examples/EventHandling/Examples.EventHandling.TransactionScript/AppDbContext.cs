using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;

namespace Examples.EventHandling.TransactionScript;

public class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // A plain CRUD entity — no aggregate, no domain events.
    public DbSet<StockItem> StockItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The outbox message table is not created automatically -- AddOutboxMessages() maps
        // __OutboxMessages the same way the Outbox example's DbContext does.
        modelBuilder.AddOutboxMessages();
    }
}
