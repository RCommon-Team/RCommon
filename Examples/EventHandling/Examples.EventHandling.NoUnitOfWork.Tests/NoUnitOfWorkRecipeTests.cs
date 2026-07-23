using FluentAssertions;
using Examples.EventHandling.NoUnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.EventHandling;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace Examples.EventHandling.NoUnitOfWork.Tests;

/// <summary>
/// Recipe 4 ("No UnitOfWork -- direct publish, the escape hatch"): proves an application can use
/// RCommon eventing WITHOUT any UnitOfWork / persistence ceremony. <see cref="IEventBus"/> is a
/// framework singleton independent of any UoW; resolving it and calling
/// <c>PublishAsync</c> dispatches synchronously to the registered subscriber. No
/// <c>WithUnitOfWork</c>, no <c>WithPersistence</c>, no <c>IUnitOfWorkFactory</c> anywhere.
/// </summary>
public class NoUnitOfWorkRecipeTests
{
    /// <summary>
    /// The required fact: build a provider with ONLY WithEventHandling (+ AddSubscriber),
    /// resolve IEventBus, publish the event directly, and confirm the subscriber ran. This is
    /// the escape hatch: no UnitOfWork and no persistence are configured or touched.
    /// </summary>
    [Fact]
    public async Task DirectPublish_WithoutUnitOfWork_InvokesSubscriber()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRCommon()
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
            {
                events.AddSubscriber<NotificationRequested, NotificationRequestedHandler>();
            });
        // Deliberately NO WithUnitOfWork and NO WithPersistence.

        using var provider = services.BuildServiceProvider();

        var handledBefore = NotificationRequestedHandler.HandledCount;

        var bus = provider.GetRequiredService<IEventBus>();
        await bus.PublishAsync(new NotificationRequested(Guid.NewGuid(), "Welcome aboard"));

        NotificationRequestedHandler.HandledCount.Should().Be(handledBefore + 1,
            "publishing directly through the IEventBus singleton must dispatch to the subscriber " +
            "without any UnitOfWork or persistence configured");
    }

    /// <summary>
    /// The optional fact: the standalone outbox store is scoped and persists its row by calling
    /// the DbContext's SaveChangesAsync itself, so an application can persist an outbox row with
    /// NO surrounding UnitOfWork. This wires EF Core InMemory + AddOutbox&lt;EFCoreOutboxStore&gt;,
    /// resolves IOutboxStore in a scope, and saves one row directly.
    /// </summary>
    [Fact]
    public async Task StandaloneOutbox_SaveAsync_PersistsRow_WithoutUnitOfWork()
    {
        using var provider = NoUnitOfWorkExample.BuildOutboxProvider(Guid.NewGuid().ToString());

        using (var schemaScope = provider.CreateScope())
        {
            await schemaScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Database.EnsureCreatedAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = nameof(NotificationRequested),
                EventPayload = "{}",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            // No UnitOfWork: EFCoreOutboxStore.SaveAsync calls SaveChangesAsync itself.
            await outbox.SaveAsync(message, "AppDb");
        }

        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await dbContext.Set<OutboxMessage>().AsNoTracking().CountAsync();
            count.Should().Be(1, "SaveAsync persisted the outbox row directly, with no UnitOfWork");
        }
    }
}
