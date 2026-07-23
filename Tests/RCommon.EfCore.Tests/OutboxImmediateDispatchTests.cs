using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.EfCore.Tests;

public record OverrideCreatedEvent(string OverrideId) : ISerializableEvent;

/// <summary>
/// End-to-end proof of the cross-host outbox fix using a real (SQLite) outbox store shared between a
/// producer "host" and a processor "host":
/// <list type="bullet">
///   <item><description>Scope A — producer host with <see cref="OutboxOptions.ImmediateDispatch"/> = false.
///   Committing persists the row but must NOT run Phase 3 (no in-process dispatch, no mark-processed),
///   leaving <c>ProcessedAtUtc == null</c>.</description></item>
///   <item><description>Scope B — processor host runs the poller, which claims the still-unprocessed row,
///   dispatches it to its subscriber, and marks it processed.</description></item>
/// </list>
/// A back-compat scope confirms the default (<c>ImmediateDispatch == true</c>) still dispatches and marks
/// in Phase 3.
/// </summary>
public class OutboxImmediateDispatchTests : IDisposable
{
    private readonly TestOutboxDbContext _dbContext;
    private readonly EFCoreOutboxStore _store;
    private readonly IOutboxSerializer _serializer = new JsonOutboxSerializer();

    public OutboxImmediateDispatchTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new TestOutboxDbContext(dbOptions);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        var factoryMock = new Mock<IDataStoreFactory>();
        factoryMock.Setup(f => f.Resolve<RCommonDbContext>(It.IsAny<string>()))
            .Returns(_dbContext);
        var outboxOpts = Options.Create(new OutboxOptions());

        _store = new EFCoreOutboxStore(factoryMock.Object, outboxOpts);
    }

    private const string DataStore = "test";

    [Fact]
    public async Task ProducerHost_ImmediateDispatchDisabled_LeavesRowUnprocessed_ThenPollerDrainsIt()
    {
        var @event = new OverrideCreatedEvent("override-1");

        // --- Scope A: producer host, ImmediateDispatch = false (persist only) ---
        var producerSpy = new CapturingProducer();
        var router = CreateRouter(producerSpy, new OutboxOptions { ImmediateDispatch = false });

        router.AddTransactionalEvent(@event);
        await router.PersistBufferedEventsAsync(); // Phase 1: write the row (committed in SQLite)
        await router.RouteEventsAsync();           // Phase 3: must be skipped entirely

        var rows = await _dbContext.Set<OutboxMessage>().AsNoTracking().ToListAsync();
        rows.Should().HaveCount(1, "the event must still be persisted to the outbox");
        rows[0].ProcessedAtUtc.Should().BeNull("the producer host must not mark the row processed");
        producerSpy.Produced.Should().BeEmpty("Phase 3 dispatch must not run on the producer host");

        // --- Scope B: processor host drains the still-unprocessed row via the poller's claim loop ---
        // This mirrors OutboxProcessingService.ProcessBatchAsync (claim -> dispatch -> mark), exercising
        // the real store's ClaimAsync (WHERE ProcessedAtUtc IS NULL). The full hosted service is covered
        // by OutboxProcessingServiceTests; we drive the loop directly here to keep the test hermetic and
        // focused on the cross-host visibility mechanism.
        var pollerSpy = new CapturingProducer();
        var claimed = await _store.ClaimAsync("processor-host", batchSize: 100, lockDuration: TimeSpan.FromMinutes(5), dataStoreName: DataStore);

        claimed.Should().ContainSingle("the poller must see the unprocessed row the producer left behind");
        foreach (var message in claimed)
        {
            var deserialized = _serializer.Deserialize(message.EventType, message.EventPayload);
            await pollerSpy.ProduceEventAsync(deserialized);
            await _store.MarkProcessedAsync(message.Id, DataStore);
        }

        pollerSpy.Produced.Should().ContainSingle("the poller is the sole dispatcher for cross-host delivery");
        pollerSpy.Produced[0].Should().BeOfType<OverrideCreatedEvent>();
        var drained = await _dbContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
        drained.ProcessedAtUtc.Should().NotBeNull("the poller marks the row processed after dispatch");
    }

    [Fact]
    public async Task ProducerHost_ImmediateDispatchDefault_DispatchesAndMarksInPhase3()
    {
        var @event = new OverrideCreatedEvent("override-2");

        var producerSpy = new CapturingProducer();
        var router = CreateRouter(producerSpy, new OutboxOptions()); // default ImmediateDispatch = true

        router.AddTransactionalEvent(@event);
        await router.PersistBufferedEventsAsync();
        await router.RouteEventsAsync();

        producerSpy.Produced.Should().ContainSingle("Phase 3 immediate dispatch runs by default");
        var row = await _dbContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
        row.ProcessedAtUtc.Should().NotBeNull("the default single-host behaviour marks the row processed in Phase 3");
    }

    private OutboxEventRouter CreateRouter(IEventProducer producer, OutboxOptions options)
    {
        var guidGenMock = new Mock<IGuidGenerator>();
        guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid());
        var tenantMock = new Mock<ITenantIdAccessor>();

        var services = new ServiceCollection();
        services.AddSingleton(producer);
        var provider = services.BuildServiceProvider();

        return new OutboxEventRouter(
            _store,
            _serializer,
            guidGenMock.Object,
            tenantMock.Object,
            provider,
            new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(options),
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = DataStore }));
    }

    private sealed class CapturingProducer : IEventProducer
    {
        public List<ISerializableEvent> Produced { get; } = new();

        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            Produced.Add(@event);
            return Task.CompletedTask;
        }
    }

    public void Dispose() => _dbContext.Dispose();
}
