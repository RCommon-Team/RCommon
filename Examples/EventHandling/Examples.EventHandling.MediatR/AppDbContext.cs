using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;

namespace Examples.EventHandling.MediatR;

// Recipe 5 persists the aggregate through EF Core so the domain event flows through the real
// UnitOfWork commit pipeline (repository -> EntityEventTracker -> transactional event router ->
// MediatR producer -> subscriber). This is a TRANSIENT event route (no outbox), so unlike Recipe 1
// there is no __OutboxMessages mapping here.
public class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = null!;
}
