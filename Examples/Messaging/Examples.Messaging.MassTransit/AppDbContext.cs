using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;

namespace Examples.Messaging.MassTransit;

/// <summary>
/// The "Orders" datastore context for recipe 2a. It carries the business <see cref="Order"/> aggregate
/// AND RCommon's outbox table. The outbox table is not created automatically, so <c>OnModelCreating</c>
/// maps <c>__OutboxMessages</c> via <see cref="ModelBuilderExtensions.AddOutboxMessages"/> — the durable
/// domain event is staged there in the same transaction as the business row.
/// </summary>
public class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxMessages();
    }
}
