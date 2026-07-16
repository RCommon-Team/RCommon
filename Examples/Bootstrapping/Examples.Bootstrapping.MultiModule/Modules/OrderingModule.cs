using Examples.Bootstrapping.MultiModule.Data;
using Examples.Bootstrapping.MultiModule.Producers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.EventHandling;

namespace Examples.Bootstrapping.MultiModule.Modules;

/// <summary>
/// The Ordering module configures persistence for its own DbContext and a guid generator,
/// and registers an <see cref="AuditProducer"/> for cross-cutting auditing.
/// </summary>
public class OrderingModule : IServiceModule
{
    public void Configure(IServiceCollection services)
    {
        services.AddRCommon()
            .WithSimpleGuidGenerator() // Singleton verb: idempotent across modules when impl matches.
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
                ef.AddDbContext<OrderingDbContext>(
                    "Ordering",
                    o => o.UseInMemoryDatabase("ordering")))
            .WithEventHandling<InMemoryEventBusBuilder>(eh => eh.AddProducer<AuditProducer>());
    }
}
