using Examples.EventHandling.TransactionScript;
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

namespace Examples.EventHandling.TransactionScript.Tests;

/// <summary>
/// Recipe 3 (transaction-script / CRUD + UnitOfWork with ROUTER-ADDED events, the NON-DDD path).
///
/// Unlike the aggregate path (Outbox recipe), where a domain event raised via AddDomainEvent is
/// routed by the tracker's route-by-durability logic, here a transaction-script service enqueues the
/// integration event DIRECTLY on the concrete OutboxEventRouter within the UnitOfWork. On CommitAsync,
/// the UnitOfWork commit pipeline drains that router buffer to the outbox (Phase 1,
/// PersistBufferedEventsAsync) and then dispatches in-process post-commit (Phase 3, RouteEventsAsync,
/// since ImmediateDispatch defaults to true), marking the row processed.
///
/// Verified framework behaviour: PersistBufferedEventsAsync persists EVERY buffered event
/// unconditionally — it does not consult the routing registry — so router.AddTransactionalEvent(evt,
/// "AppDb") alone is sufficient to write the outbox row. Publish&lt;T&gt;().UseOutbox("AppDb") is only
/// required so Phase 3's in-process producer matches the event and dispatches it to the subscriber.
/// </summary>
public class TransactionScriptRecipeTests
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
                eh.AddSubscriber<StockAdjustedEvent, StockAdjustedHandler>();
                eh.Publish<StockAdjustedEvent>().UseOutbox("AppDb");
            });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task TransactionScript_RouterAddedEvent_PersistsOutboxRow_DispatchesToSubscriber_AndMarksProcessed()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString());

        using (var schemaScope = provider.CreateScope())
        {
            await schemaScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        var handledCountBefore = StockAdjustedHandler.HandledCount;

        using (var scope = provider.CreateScope())
        {
            var stockItems = scope.ServiceProvider.GetRequiredService<ILinqRepository<StockItem>>();
            var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

            // CONCRETE OutboxEventRouter: the (evt, dataStoreName) overload is not on IEventRouter.
            var router = scope.ServiceProvider.GetRequiredService<OutboxEventRouter>();

            var stockItem = new StockItem { Sku = "ABC", Quantity = 10 };

            using var uow = unitOfWorkFactory.Create();

            // CRUD write (no aggregate, no domain event).
            await stockItems.AddAsync(stockItem);

            // Transaction script explicitly enqueues the integration event on the router.
            router.AddTransactionalEvent(
                new StockAdjustedEvent(stockItem.Id, stockItem.Sku, stockItem.Quantity), "AppDb");

            await uow.CommitAsync();
        }

        StockAdjustedHandler.HandledCount.Should().Be(handledCountBefore + 1,
            "Phase 3's immediate in-process dispatch must deliver the router-added event to the subscriber");

        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await dbContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();

            row.EventType.Should().Contain(nameof(StockAdjustedEvent),
                "the router-added event must be persisted to the outbox by the commit pipeline");
            row.ProcessedAtUtc.Should().NotBeNull(
                "Phase 3 must mark the row processed after a successful in-process dispatch");
        }
    }
}
