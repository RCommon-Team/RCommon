using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;

namespace Examples.EventHandling.NoUnitOfWork;

/// <summary>
/// Minimal DbContext used ONLY by the optional standalone-outbox demonstration. It maps the
/// __OutboxMessages table via <c>AddOutboxMessages()</c>; there are no domain entities because the
/// escape-hatch recipe deliberately avoids repositories and UnitOfWork.
/// </summary>
public class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOutboxMessages();
    }
}
