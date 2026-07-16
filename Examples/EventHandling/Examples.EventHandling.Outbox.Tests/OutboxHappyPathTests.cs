using Examples.EventHandling.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.EventHandling;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;
using Xunit;

namespace Examples.EventHandling.Outbox.Tests;

/// <summary>
/// Proves the full three-phase outbox flow completes end-to-end: persisting the outbox row within
/// the transaction (Phase 1), committing (Phase 2), and the best-effort immediate in-process dispatch
/// to the registered subscriber, which also marks the row processed (Phase 3). Enough moving parts
/// (UnitOfWork, EventTracker, OutboxEventRouter, the in-memory event bus, and the EF Core outbox
/// store) are involved here that a silent regression in any one of them would be easy to miss without
/// an end-to-end test.
/// </summary>
public class OutboxHappyPathTests
{
    private static ServiceProvider BuildProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("AppDb", options => options.UseInMemoryDatabase(databaseName));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
                ef.AddOutbox<EFCoreOutboxStore>();
            })
            .WithEventHandling<InMemoryEventBusBuilder>(eh =>
            {
                eh.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
            });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CommitAsync_PersistsOutboxRow_DispatchesToSubscriber_AndMarksProcessed()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString());

        using (var schemaScope = provider.CreateScope())
        {
            await schemaScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        var handledCountBefore = OrderPlacedEventHandler.HandledCount;

        Guid orderId;
        using (var scope = provider.CreateScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Order, Guid>>();
            var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

            var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
            order.Place();
            orderId = order.Id;

            using var uow = unitOfWorkFactory.Create();
            await orders.AddAsync(order);
            await uow.CommitAsync();
        }

        OrderPlacedEventHandler.HandledCount.Should().Be(handledCountBefore + 1,
            "the subscriber must receive the event via Phase 3's immediate dispatch");

        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await dbContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();

            row.ProcessedAtUtc.Should().NotBeNull("Phase 3 must mark the row processed after a successful dispatch");
            row.EventType.Should().Contain(nameof(OrderPlacedEvent));
        }
    }
}
