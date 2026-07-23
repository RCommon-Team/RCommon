using Examples.EventHandling.MediatR;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.MediatR;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;
using Xunit;

namespace Examples.EventHandling.MediatR.Tests;

/// <summary>
/// Recipe 5: DDD + UnitOfWork + in-process mediator (MediatR).
///
/// An aggregate raises a domain event which is dispatched IN-PROCESS through MediatR to an
/// <see cref="RCommon.EventHandling.Subscribers.ISubscriber{T}"/> handler as part of the UnitOfWork
/// COMMIT pipeline. There is no broker and no transport -- MediatR is in-process only. MediatR itself
/// is self-registered by <c>WithEventHandling&lt;MediatREventHandlingBuilder&gt;</c> (it calls
/// <c>services.AddMediatR(...)</c> internally), so the test wires no mediator by hand.
///
/// Wiring requires BOTH verbs: <c>Publish&lt;OrderPlacedEvent&gt;()</c> registers
/// <see cref="RCommon.MediatR.Producers.PublishWithMediatREventProducer"/> (the in-process producer),
/// and <c>AddSubscriber&lt;OrderPlacedEvent, OrderPlacedEventHandler&gt;()</c> bridges the RCommon
/// subscriber to a MediatR notification handler. AddSubscriber alone does not register the producer.
/// The <c>Publish&lt;T&gt;()</c> route here is TRANSIENT (no <c>.UseOutbox</c>), so the event is
/// dispatched immediately by the commit pipeline rather than persisted to an outbox.
///
/// Dispatch shape (the canonical DDD flow): the aggregate raises the event via <c>AddDomainEvent</c>,
/// the repository stages it, and <c>IUnitOfWorkFactory.Create()</c> + <c>CommitAsync()</c> drives the
/// event through the transactional event router to the MediatR publish producer and on to the
/// subscriber in-process. The event is NOT produced by directly resolving the producer -- delivery is
/// proven to happen THROUGH THE COMMIT PIPELINE.
///
/// This end-to-end delivery is only possible because <see cref="RCommon.MediatR.MediatRAdapter"/> wraps
/// the notification by its RUNTIME type: the transactional router hands events to producers statically
/// typed as <c>ISerializableEvent</c>, so wrapping by the compile-time generic argument would build
/// <c>MediatRNotification&lt;ISerializableEvent&gt;</c> and never match the registered
/// <c>INotificationHandler&lt;MediatRNotification&lt;OrderPlacedEvent&gt;&gt;</c> -- silently dropping
/// the event.
/// </summary>
public class MediatRRecipeTests
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
            })
            .WithEventHandling<MediatREventHandlingBuilder>(events =>
            {
                // Publish<T>() registers PublishWithMediatREventProducer (the in-process producer) as a
                // TRANSIENT route (no .UseOutbox); AddSubscriber<T,H>() bridges the RCommon ISubscriber
                // to a MediatR notification handler. Both are required to produce AND handle the event
                // in-process via the commit pipeline.
                events.Publish<OrderPlacedEvent>();
                events.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
            });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CommitAsync_DispatchesDomainEvent_ToInProcessMediatRSubscriber()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString());

        using (var schemaScope = provider.CreateScope())
        {
            await schemaScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        // The handler counts invocations with a static field, so assert delta-based.
        var handledCountBefore = OrderPlacedEventHandler.HandledCount;

        using (var scope = provider.CreateScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Order, Guid>>();
            var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

            // Aggregate raises the domain event via the DDD API.
            var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
            order.Place();

            using var uow = unitOfWorkFactory.Create();
            await orders.AddAsync(order);

            // CommitAsync drives the aggregate-raised domain event through the transactional event
            // router to the MediatR publish producer and on to OrderPlacedEventHandler in-process --
            // NOT by directly resolving and invoking the producer.
            await uow.CommitAsync();
        }

        OrderPlacedEventHandler.HandledCount.Should().Be(handledCountBefore + 1,
            "committing the unit of work must dispatch the domain event in-process via MediatR to the ISubscriber");
    }
}
