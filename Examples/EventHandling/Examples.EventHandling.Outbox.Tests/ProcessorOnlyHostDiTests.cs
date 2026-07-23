using Examples.EventHandling.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;
using Xunit;

namespace Examples.EventHandling.Outbox.Tests;

/// <summary>
/// Reproduces the third-party report #16: a host configured with <c>AddOutboxProcessor</c> (the poller
/// half of the producer/processor topology) rather than <c>AddOutbox</c>. Two shapes are exercised:
///   1. Pure poller host, built with ValidateOnBuild — must construct without a DI resolution failure.
///   2. Processor host that ALSO commits domain entities raising a durable event — must not silently
///      drop the event (the #15 failure mode, but reached via the processor-only registration path).
/// Runs on the EF Core InMemory provider (fast lane).
/// </summary>
public class ProcessorOnlyHostDiTests
{
    private static IServiceCollection BaseServices(string db, out IServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddLogging();
        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("AppDb", o => o.UseInMemoryDatabase(db));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
                ef.AddOutboxProcessor<EFCoreOutboxStore>(); // PROCESSOR ONLY (no producer)
            })
            .WithEventHandling<InMemoryEventBusBuilder>(eh =>
            {
                eh.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
                eh.Publish<OrderPlacedEvent>().UseOutbox("AppDb");
            });
        return services;
    }

    [Fact]
    public void ProcessorOnlyHost_BuildsWithValidateOnBuild_WithoutDiResolutionFailure()
    {
        BaseServices(Guid.NewGuid().ToString(), out var services);

        Action build = () =>
        {
            using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
        };

        build.Should().NotThrow("a processor-only host must be constructible (#16)");
    }

    [Fact]
    public async Task ProcessorOnlyHost_ThatCommitsDomainEntities_PersistsDurableEvent()
    {
        BaseServices(Guid.NewGuid().ToString(), out var services);
        using var provider = services.BuildServiceProvider(validateScopes: true);

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var orders = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();

            var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
            order.Place();

            using var uow = uowFactory.Create();
            await orders.AddAsync(order);
            await uow.CommitAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await db.Set<OutboxMessage>().AsNoTracking().CountAsync();
            count.Should().Be(1,
                "a host that runs the poller and also commits domain entities must persist the durable event");
        }
    }
}
