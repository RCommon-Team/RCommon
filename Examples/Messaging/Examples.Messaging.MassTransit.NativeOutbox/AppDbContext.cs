using MassTransit;
using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;

namespace Examples.Messaging.MassTransit.NativeOutbox;

/// <summary>
/// The "Orders" datastore context for recipe 2b. It carries the business <see cref="Order"/> entity AND
/// MassTransit's OWN transactional outbox entities (InboxState + OutboxState + OutboxMessage). Those are
/// mapped via <c>modelBuilder.AddTransactionalOutboxEntities()</c> in <see cref="OnModelCreating"/> — the
/// <c>UseBrokerOutbox&lt;AppDbContext&gt;</c> wrapper cannot inject that mapping.
///
/// This is deliberately MassTransit's outbox, NOT RCommon's <c>AddOutboxMessages()</c>: recipe 2b wraps the
/// broker's native EF Core bus outbox rather than RCommon's per-datastore outbox (recipe 2a).
///
/// Derives from <see cref="RCommonDbContext"/> to satisfy RCommon's <c>AddDbContext</c> constraint while
/// remaining a plain EF <see cref="DbContext"/> for MassTransit's outbox.
/// </summary>
public sealed class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddTransactionalOutboxEntities(); // InboxState + OutboxState + OutboxMessage
    }
}
