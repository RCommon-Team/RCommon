using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.EventHandling;
using RCommon.Persistence.EFCore.Outbox;

namespace Examples.EventHandling.NoUnitOfWork;

/// <summary>
/// Recipe 4 -- "No UnitOfWork (direct publish), the escape hatch".
///
/// The <c>IEventBus</c> is registered by the RCommon builder as a framework singleton that is
/// completely independent of any UnitOfWork or persistence wiring. An application that just wants to
/// fire an in-process event can resolve it and publish directly -- no <c>WithUnitOfWork</c>, no
/// <c>WithPersistence</c>, no <c>IUnitOfWorkFactory</c>.
/// </summary>
public static class NoUnitOfWorkExample
{
    /// <summary>
    /// The primary escape hatch: RCommon eventing with ONLY WithEventHandling. No UoW, no persistence.
    /// </summary>
    public static ServiceProvider BuildDirectPublishProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRCommon()
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
            {
                events.AddSubscriber<NotificationRequested, NotificationRequestedHandler>();
            });
        // Deliberately NO WithUnitOfWork and NO WithPersistence.
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// The optional variant: wires EF Core (InMemory) + the standalone outbox store. This exists only
    /// to show that <see cref="RCommon.Persistence.Outbox.IOutboxStore"/> persists a row by calling the
    /// DbContext's SaveChangesAsync itself -- still with NO surrounding UnitOfWork.
    /// </summary>
    public static ServiceProvider BuildOutboxProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRCommon()
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("AppDb", options => options.UseInMemoryDatabase(databaseName));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
                ef.AddOutbox<EFCoreOutboxStore>();
            })
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
            {
                events.AddSubscriber<NotificationRequested, NotificationRequestedHandler>();
            });
        // Still NO WithUnitOfWork: the outbox store saves its own changes.
        return services.BuildServiceProvider();
    }
}
