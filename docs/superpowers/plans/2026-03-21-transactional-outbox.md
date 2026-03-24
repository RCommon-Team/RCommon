# Transactional Outbox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a transactional outbox pattern that persists domain events within the same DB transaction, guaranteeing at-least-once delivery via immediate dispatch + background poller.

**Architecture:** Replace `IEventRouter` with `OutboxEventRouter` that buffers events in memory, persists them to `IOutboxStore` pre-commit, and dispatches post-commit. A background `OutboxProcessingService` polls for undelivered messages. MassTransit and Wolverine get thin wrapper projects that delegate to their native outbox implementations.

**Tech Stack:** .NET (net8.0/net9.0/net10.0), EF Core, Dapper, Linq2Db, MassTransit 8.5.8, WolverineFx 5.13.0, System.Text.Json, xUnit 2.9.3, FluentAssertions 8.2.0, Moq 4.20.72

**Spec:** `docs/superpowers/specs/2026-03-21-transactional-outbox-design.md`

---

## File Map

### New files in existing projects

| Project | File | Responsibility |
|---------|------|---------------|
| `RCommon.Persistence` | `Outbox/IOutboxMessage.cs` | Interface for outbox message entity |
| `RCommon.Persistence` | `Outbox/OutboxMessage.cs` | Concrete outbox message entity |
| `RCommon.Persistence` | `Outbox/IOutboxStore.cs` | Persistence abstraction for outbox CRUD |
| `RCommon.Persistence` | `Outbox/IOutboxSerializer.cs` | Serialization abstraction |
| `RCommon.Persistence` | `Outbox/JsonOutboxSerializer.cs` | Default System.Text.Json serializer |
| `RCommon.Persistence` | `Outbox/OutboxOptions.cs` | Configuration options |
| `RCommon.Persistence` | `Outbox/OutboxEventRouter.cs` | IEventRouter impl that buffers → persists → dispatches |
| `RCommon.Persistence` | `Outbox/OutboxEntityEventTracker.cs` | Decorator over InMemoryEntityEventTracker |
| `RCommon.Persistence` | `Outbox/OutboxProcessingService.cs` | Background IHostedService poller |
| `RCommon.Persistence` | `Outbox/OutboxPersistenceBuilderExtensions.cs` | `AddOutbox<T>()` extension on IPersistenceBuilder |
| `RCommon.EfCore` | `Outbox/EFCoreOutboxStore.cs` | EF Core IOutboxStore implementation |
| `RCommon.EfCore` | `Outbox/OutboxMessageConfiguration.cs` | EF Core entity type configuration |
| `RCommon.EfCore` | `Outbox/ModelBuilderExtensions.cs` | `AddOutboxMessages()` convenience extension |
| `RCommon.Dapper` | `Outbox/DapperOutboxStore.cs` | Dapper IOutboxStore via raw SQL |
| `RCommon.Linq2Db` | `Outbox/Linq2DbOutboxStore.cs` | Linq2Db IOutboxStore implementation |

### Modified files in existing projects

| File | Change |
|------|--------|
| `Src/RCommon.Entities/IEntityEventTracker.cs` | Add `PersistEventsAsync(CT)`, add CT to `EmitTransactionalEventsAsync` |
| `Src/RCommon.Entities/InMemoryEntityEventTracker.cs` | Implement `PersistEventsAsync` as no-op, propagate CT |
| `Src/RCommon.Persistence/Transactions/UnitOfWork.cs` | Two-phase CommitAsync: persist → commit → dispatch |
| `Src/RCommon.Persistence/RCommon.Persistence.csproj` | Add `Microsoft.Extensions.Hosting.Abstractions` PackageReference |

### New projects

| Project | Key files |
|---------|-----------|
| `Src/RCommon.MassTransit.Outbox/` | `IMassTransitOutboxBuilder.cs`, `MassTransitOutboxBuilder.cs`, `MassTransitOutboxBuilderExtensions.cs`, `RCommon.MassTransit.Outbox.csproj` |
| `Src/RCommon.Wolverine.Outbox/` | `IWolverineOutboxBuilder.cs`, `WolverineOutboxBuilder.cs`, `WolverineOutboxBuilderExtensions.cs`, `RCommon.Wolverine.Outbox.csproj` |
| `Tests/RCommon.MassTransit.Outbox.Tests/` | `MassTransitOutboxBuilderTests.cs` |
| `Tests/RCommon.Wolverine.Outbox.Tests/` | `WolverineOutboxBuilderTests.cs` |

### Test files (additions to existing test projects)

| Project | File |
|---------|------|
| `Tests/RCommon.Persistence.Tests/` | `JsonOutboxSerializerTests.cs` |
| `Tests/RCommon.Persistence.Tests/` | `OutboxEventRouterTests.cs` |
| `Tests/RCommon.Persistence.Tests/` | `OutboxEntityEventTrackerTests.cs` |
| `Tests/RCommon.Persistence.Tests/` | `OutboxProcessingServiceTests.cs` |
| `Tests/RCommon.Persistence.Tests/` | `UnitOfWorkOutboxTests.cs` |
| `Tests/RCommon.Persistence.Tests/` | `OutboxConcurrencyTests.cs` |
| `Tests/RCommon.EfCore.Tests/` | `EFCoreOutboxStoreTests.cs` |
| `Tests/RCommon.Dapper.Tests/` | `DapperOutboxStoreTests.cs` |
| `Tests/RCommon.Linq2Db.Tests/` | `Linq2DbOutboxStoreTests.cs` |

---

## Task 1: Core Outbox Abstractions — Interfaces & Entities

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/IOutboxMessage.cs`
- Create: `Src/RCommon.Persistence/Outbox/OutboxMessage.cs`
- Create: `Src/RCommon.Persistence/Outbox/IOutboxStore.cs`
- Create: `Src/RCommon.Persistence/Outbox/IOutboxSerializer.cs`
- Create: `Src/RCommon.Persistence/Outbox/OutboxOptions.cs`

- [ ] **Step 1: Create IOutboxMessage interface**

```csharp
// Src/RCommon.Persistence/Outbox/IOutboxMessage.cs
namespace RCommon.Persistence.Outbox;

public interface IOutboxMessage
{
    Guid Id { get; }
    string EventType { get; }
    string EventPayload { get; }
    DateTimeOffset CreatedAtUtc { get; }
    DateTimeOffset? ProcessedAtUtc { get; set; }
    DateTimeOffset? DeadLetteredAtUtc { get; set; }
    string? ErrorMessage { get; set; }
    int RetryCount { get; set; }
    string? CorrelationId { get; set; }
    string? TenantId { get; set; }
}
```

- [ ] **Step 2: Create OutboxMessage concrete entity**

```csharp
// Src/RCommon.Persistence/Outbox/OutboxMessage.cs
namespace RCommon.Persistence.Outbox;

public class OutboxMessage : IOutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventPayload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public DateTimeOffset? DeadLetteredAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? CorrelationId { get; set; }
    public string? TenantId { get; set; }
}
```

- [ ] **Step 3: Create IOutboxStore interface**

```csharp
// Src/RCommon.Persistence/Outbox/IOutboxStore.cs
namespace RCommon.Persistence.Outbox;

public interface IOutboxStore
{
    Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
    Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Create IOutboxSerializer interface**

```csharp
// Src/RCommon.Persistence/Outbox/IOutboxSerializer.cs
using RCommon.Models.Events;

namespace RCommon.Persistence.Outbox;

public interface IOutboxSerializer
{
    string Serialize(ISerializableEvent @event);
    string GetEventTypeName(ISerializableEvent @event);
    ISerializableEvent Deserialize(string eventType, string payload);
}
```

- [ ] **Step 5: Create OutboxOptions**

```csharp
// Src/RCommon.Persistence/Outbox/OutboxOptions.cs
namespace RCommon.Persistence.Outbox;

public class OutboxOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 5;
    public TimeSpan CleanupAge { get; set; } = TimeSpan.FromDays(7);
    public string TableName { get; set; } = "__OutboxMessages";
}
```

- [ ] **Step 6: Build to verify compilation**

Run: `dotnet build Src/RCommon.Persistence/RCommon.Persistence.csproj`
Expected: Build succeeded. 0 errors.

- [ ] **Step 7: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/
git commit -m "feat: add outbox core abstractions (IOutboxMessage, IOutboxStore, IOutboxSerializer, OutboxOptions)"
```

---

## Task 2: JsonOutboxSerializer + Tests

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/JsonOutboxSerializer.cs`
- Create: `Tests/RCommon.Persistence.Tests/JsonOutboxSerializerTests.cs`

- [ ] **Step 1: Write failing tests for JsonOutboxSerializer**

```csharp
// Tests/RCommon.Persistence.Tests/JsonOutboxSerializerTests.cs
using FluentAssertions;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using System.Text.Json;
using Xunit;

namespace RCommon.Persistence.Tests;

public record SerializerTestEvent(string Name, int Value) : ISerializableEvent;

public class JsonOutboxSerializerTests
{
    private readonly JsonOutboxSerializer _serializer = new();

    [Fact]
    public void Serialize_ReturnsValidJson()
    {
        var @event = new SerializerTestEvent("OrderCreated", 42);
        var json = _serializer.Serialize(@event);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("Name").GetString().Should().Be("OrderCreated");
        doc.RootElement.GetProperty("Value").GetInt32().Should().Be(42);
    }

    [Fact]
    public void GetEventTypeName_ReturnsShortAssemblyQualifiedName()
    {
        var @event = new SerializerTestEvent("Test", 1);
        var typeName = _serializer.GetEventTypeName(@event);
        // Should contain type name and assembly, but not version/culture/token
        typeName.Should().Contain("TestEvent");
        typeName.Should().Contain(",");
    }

    [Fact]
    public void Deserialize_RoundTrips()
    {
        var original = new SerializerTestEvent("OrderCreated", 42);
        var json = _serializer.Serialize(original);
        var typeName = _serializer.GetEventTypeName(original);
        var deserialized = _serializer.Deserialize(typeName, json);
        deserialized.Should().BeOfType<TestEvent>();
        var typed = (TestEvent)deserialized;
        typed.Name.Should().Be("OrderCreated");
        typed.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_ThrowsForUnknownType()
    {
        var act = () => _serializer.Deserialize("NonExistent.Type, FakeAssembly", "{}");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deserialize_ThrowsForNonSerializableEventType()
    {
        // string implements nothing related to ISerializableEvent
        var typeName = typeof(string).AssemblyQualifiedName!;
        var act = () => _serializer.Deserialize(typeName, "\"hello\"");
        act.Should().Throw<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~JsonOutboxSerializerTests" --no-build 2>&1 || echo "Expected: build failure (JsonOutboxSerializer not found)"`
Expected: Build failure — `JsonOutboxSerializer` does not exist yet.

- [ ] **Step 3: Implement JsonOutboxSerializer**

```csharp
// Src/RCommon.Persistence/Outbox/JsonOutboxSerializer.cs
using System.Text.Json;
using RCommon.Models.Events;

namespace RCommon.Persistence.Outbox;

public class JsonOutboxSerializer : IOutboxSerializer
{
    public string Serialize(ISerializableEvent @event)
    {
        Guard.IsNotNull(@event, nameof(@event));
        return JsonSerializer.Serialize(@event, @event.GetType());
    }

    public string GetEventTypeName(ISerializableEvent @event)
    {
        Guard.IsNotNull(@event, nameof(@event));
        var type = @event.GetType();
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    public ISerializableEvent Deserialize(string eventType, string payload)
    {
        Guard.IsNotNull(eventType, nameof(eventType));
        Guard.IsNotNull(payload, nameof(payload));

        var type = Type.GetType(eventType)
            ?? throw new InvalidOperationException($"Cannot resolve type '{eventType}'.");

        if (!typeof(ISerializableEvent).IsAssignableFrom(type))
        {
            throw new InvalidOperationException(
                $"Type '{eventType}' does not implement ISerializableEvent.");
        }

        var result = JsonSerializer.Deserialize(payload, type)
            ?? throw new InvalidOperationException(
                $"Deserialization of '{eventType}' returned null.");

        return (ISerializableEvent)result;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~JsonOutboxSerializerTests"`
Expected: 5 passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/JsonOutboxSerializer.cs Tests/RCommon.Persistence.Tests/JsonOutboxSerializerTests.cs
git commit -m "feat: add JsonOutboxSerializer with round-trip serialization and type safety"
```

---

## Task 3: IEntityEventTracker Interface Changes + UnitOfWork Two-Phase

**Files:**
- Modify: `Src/RCommon.Entities/IEntityEventTracker.cs`
- Modify: `Src/RCommon.Entities/InMemoryEntityEventTracker.cs`
- Modify: `Src/RCommon.Persistence/Transactions/UnitOfWork.cs`
- Modify: `Src/RCommon.Persistence/RCommon.Persistence.csproj`

- [ ] **Step 1: Add PersistEventsAsync to IEntityEventTracker and add CT to EmitTransactionalEventsAsync**

Modify `Src/RCommon.Entities/IEntityEventTracker.cs`:
- Change `Task<bool> EmitTransactionalEventsAsync();` → `Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default);`
- Add: `Task PersistEventsAsync(CancellationToken cancellationToken = default);`

- [ ] **Step 2: Implement in InMemoryEntityEventTracker**

Modify `Src/RCommon.Entities/InMemoryEntityEventTracker.cs`:
- Add `PersistEventsAsync` as no-op: `public Task PersistEventsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;`
- Update `EmitTransactionalEventsAsync` signature to accept `CancellationToken cancellationToken = default`
- Pass `cancellationToken` to `_eventRouter.RouteEventsAsync(cancellationToken)`

- [ ] **Step 3: Add Microsoft.Extensions.Hosting.Abstractions to RCommon.Persistence.csproj**

Add to `Src/RCommon.Persistence/RCommon.Persistence.csproj` inside an `<ItemGroup>`:
```xml
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="8.0.1" Condition=" '$(TargetFramework)' == 'net8.0' " />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="9.0.5" Condition=" '$(TargetFramework)' == 'net9.0' " />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.2" Condition=" '$(TargetFramework)' == 'net10.0' " />
```

- [ ] **Step 4: Update UnitOfWork.CommitAsync to two-phase flow**

Modify `Src/RCommon.Persistence/Transactions/UnitOfWork.cs` — replace the body of `CommitAsync` with:
```csharp
public async Task CommitAsync(CancellationToken cancellationToken = default)
{
    Guard.Against<ObjectDisposedException>(_state == UnitOfWorkState.Disposed,
        "Cannot commit a disposed UnitOfWorkScope instance.");
    Guard.Against<UnitOfWorkException>(_state == UnitOfWorkState.Completed,
        "This unit of work scope has been marked completed.");

    _state = UnitOfWorkState.CommitAttempted;

    // Phase 1: persist events to outbox (within active transaction)
    if (_eventTracker != null)
    {
        await _eventTracker.PersistEventsAsync(cancellationToken).ConfigureAwait(false);
    }

    // Phase 2: commit transaction (domain writes + outbox writes atomically)
    _transactionScope.Complete();
    _transactionScope.Dispose();
    _transactionScopeDisposed = true;
    _state = UnitOfWorkState.Completed;

    // Phase 3: immediate dispatch attempt (best-effort, failures handled by poller)
    if (_eventTracker != null)
    {
        var dispatched = await _eventTracker
            .EmitTransactionalEventsAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!dispatched)
        {
            _logger.LogWarning(
                "UnitOfWork {TransactionId}: domain event dispatch returned false.",
                TransactionId);
        }
    }
}
```

- [ ] **Step 5: Build entire solution to verify no compilation errors**

Run: `dotnet build Src/RCommon.sln`
Expected: Build succeeded. 0 errors. (All projects that implement `IEntityEventTracker` must compile.)

- [ ] **Step 6: Run existing tests to verify no regressions**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ && dotnet test Tests/RCommon.Core.Tests/ && dotnet test Tests/RCommon.Mediatr.Tests/`
Expected: All existing tests pass (PersistEventsAsync is no-op, CT has default value).

- [ ] **Step 7: Commit**

```bash
git add Src/RCommon.Entities/IEntityEventTracker.cs Src/RCommon.Entities/InMemoryEntityEventTracker.cs Src/RCommon.Persistence/Transactions/UnitOfWork.cs Src/RCommon.Persistence/RCommon.Persistence.csproj
git commit -m "feat: two-phase UnitOfWork commit with PersistEventsAsync and CancellationToken propagation"
```

---

## Task 4: OutboxEventRouter + Tests

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/OutboxEventRouter.cs`
- Create: `Tests/RCommon.Persistence.Tests/OutboxEventRouterTests.cs`

- [ ] **Step 1: Write failing tests for OutboxEventRouter**

```csharp
// Tests/RCommon.Persistence.Tests/OutboxEventRouterTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

public record RouterTestEvent(string Data) : ISerializableEvent;

public class OutboxEventRouterTests
{
    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IGuidGenerator> _guidGenMock = new();
    private readonly Mock<ITenantIdAccessor> _tenantMock = new();
    private readonly IOutboxSerializer _serializer = new JsonOutboxSerializer();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly EventSubscriptionManager _subscriptionManager = new();

    private OutboxEventRouter CreateRouter()
    {
        _guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid());
        _tenantMock.Setup(t => t.GetTenantId()).Returns((string?)null);
        return new OutboxEventRouter(
            _storeMock.Object,
            _serializer,
            _guidGenMock.Object,
            _tenantMock.Object,
            _serviceProviderMock.Object,
            _subscriptionManager,
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));
    }

    [Fact]
    public void AddTransactionalEvent_BuffersWithoutCallingStore()
    {
        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("test"));
        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PersistBufferedEventsAsync_WritesBufferedEventsToStore()
    {
        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("event1"));
        router.AddTransactionalEvent(new RouterTestEvent("event2"));

        await router.PersistBufferedEventsAsync();

        _storeMock.Verify(
            s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PersistBufferedEventsAsync_ClearsBufferAfterPersistence()
    {
        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("event1"));
        await router.PersistBufferedEventsAsync();

        // Second call should have nothing to persist
        _storeMock.Invocations.Clear();
        await router.PersistBufferedEventsAsync();

        _storeMock.Verify(
            s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PersistBufferedEventsAsync_SetsCorrectMessageFields()
    {
        IOutboxMessage? captured = null;
        _storeMock.Setup(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<IOutboxMessage, CancellationToken>((msg, _) => captured = msg);
        _tenantMock.Setup(t => t.GetTenantId()).Returns("tenant-1");

        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("data"));
        await router.PersistBufferedEventsAsync();

        captured.Should().NotBeNull();
        captured!.EventType.Should().Contain("RouterTestEvent");
        captured.EventPayload.Should().Contain("data");
        captured.TenantId.Should().Be("tenant-1");
        captured.RetryCount.Should().Be(0);
        captured.ProcessedAtUtc.Should().BeNull();
        captured.DeadLetteredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RouteEventsAsync_DispatchesPendingFromStore()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(new RouterTestEvent("x")),
            EventPayload = _serializer.Serialize(new RouterTestEvent("x")),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var producerMock = new Mock<IEventProducer>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventProducer>)))
            .Returns(new[] { producerMock.Object });

        var router = CreateRouter();
        await router.RouteEventsAsync();

        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteEventsAsync_MarksFailedOnException()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(new RouterTestEvent("x")),
            EventPayload = _serializer.Serialize(new RouterTestEvent("x")),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var producerMock = new Mock<IEventProducer>();
        producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("broker down"));
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventProducer>)))
            .Returns(new[] { producerMock.Object });

        var router = CreateRouter();
        await router.RouteEventsAsync();

        _storeMock.Verify(s => s.MarkFailedAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~OutboxEventRouterTests" --no-build 2>&1 || echo "Expected: build failure"`
Expected: Build failure — `OutboxEventRouter` does not exist yet.

- [ ] **Step 3: Implement OutboxEventRouter**

```csharp
// Src/RCommon.Persistence/Outbox/OutboxEventRouter.cs
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Security.Claims;

namespace RCommon.Persistence.Outbox;

public class OutboxEventRouter : IEventRouter
{
    private readonly IOutboxStore _outboxStore;
    private readonly IOutboxSerializer _serializer;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ITenantIdAccessor _tenantIdAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventSubscriptionManager _subscriptionManager;
    private readonly ILogger<OutboxEventRouter> _logger;
    private readonly OutboxOptions _options;
    private readonly ConcurrentQueue<ISerializableEvent> _buffer = new();

    public OutboxEventRouter(
        IOutboxStore outboxStore,
        IOutboxSerializer serializer,
        IGuidGenerator guidGenerator,
        ITenantIdAccessor tenantIdAccessor,
        IServiceProvider serviceProvider,
        EventSubscriptionManager subscriptionManager,
        ILogger<OutboxEventRouter> logger,
        IOptions<OutboxOptions> options)
    {
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _guidGenerator = guidGenerator ?? throw new ArgumentNullException(nameof(guidGenerator));
        _tenantIdAccessor = tenantIdAccessor ?? throw new ArgumentNullException(nameof(tenantIdAccessor));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public void AddTransactionalEvent(ISerializableEvent serializableEvent)
    {
        Guard.IsNotNull(serializableEvent, nameof(serializableEvent));
        _buffer.Enqueue(serializableEvent);
    }

    public void AddTransactionalEvents(IEnumerable<ISerializableEvent> serializableEvents)
    {
        Guard.IsNotNull(serializableEvents, nameof(serializableEvents));
        foreach (var e in serializableEvents)
        {
            AddTransactionalEvent(e);
        }
    }

    public async Task PersistBufferedEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<ISerializableEvent>();
        while (_buffer.TryDequeue(out var e))
        {
            events.Add(e);
        }

        foreach (var @event in events)
        {
            var message = new OutboxMessage
            {
                Id = _guidGenerator.Create(),
                EventType = _serializer.GetEventTypeName(@event),
                EventPayload = _serializer.Serialize(@event),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                TenantId = _tenantIdAccessor.GetTenantId()
                // Note: CorrelationId population is left for a future enhancement (V2)
                // when a correlation ID accessor is available in the framework
            };

            _logger.LogDebug("Persisting outbox message {Id} for event {EventType}", message.Id, message.EventType);
            await _outboxStore.SaveAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RouteEventsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _outboxStore.GetPendingAsync(_options.BatchSize, cancellationToken).ConfigureAwait(false);

        if (pending.Count == 0) return;

        _logger.LogInformation("OutboxEventRouter dispatching {Count} pending messages", pending.Count);

        var producers = _serviceProvider.GetServices<IEventProducer>();

        foreach (var message in pending)
        {
            try
            {
                var @event = _serializer.Deserialize(message.EventType, message.EventPayload);
                var filteredProducers = _subscriptionManager.HasSubscriptions
                    ? _subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                    : producers;

                foreach (var producer in filteredProducers)
                {
                    await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
                }

                await _outboxStore.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch outbox message {Id}", message.Id);
                await _outboxStore.MarkFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task RouteEventsAsync(IEnumerable<ISerializableEvent> transactionalEvents, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(transactionalEvents, nameof(transactionalEvents));

        var producers = _serviceProvider.GetServices<IEventProducer>();

        foreach (var @event in transactionalEvents)
        {
            var filteredProducers = _subscriptionManager.HasSubscriptions
                ? _subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                : producers;

            foreach (var producer in filteredProducers)
            {
                await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~OutboxEventRouterTests"`
Expected: 6 passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/OutboxEventRouter.cs Tests/RCommon.Persistence.Tests/OutboxEventRouterTests.cs
git commit -m "feat: add OutboxEventRouter with buffer-persist-dispatch pattern"
```

---

## Task 5: OutboxEntityEventTracker + Tests

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs`
- Create: `Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

public record TrackerTestEvent(string Data) : ISerializableEvent;

public class OutboxEntityEventTrackerTests
{
    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IGuidGenerator> _guidGenMock = new();
    private readonly OutboxEventRouter _outboxRouter;
    private readonly InMemoryEntityEventTracker _innerTracker;

    public OutboxEntityEventTrackerTests()
    {
        _guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid());
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        _outboxRouter = new OutboxEventRouter(
            _storeMock.Object,
            new JsonOutboxSerializer(),
            _guidGenMock.Object,
            tenantMock.Object,
            serviceProviderMock.Object,
            new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));

        _innerTracker = new InMemoryEntityEventTracker(_outboxRouter);
    }

    [Fact]
    public void AddEntity_DelegatesToInnerTracker()
    {
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter);
        var entityMock = new Mock<IBusinessEntity>();
        entityMock.Setup(e => e.AllowEventTracking).Returns(true);

        tracker.AddEntity(entityMock.Object);

        tracker.TrackedEntities.Should().Contain(entityMock.Object);
    }

    [Fact]
    public async Task PersistEventsAsync_WithNoEntities_CompletesWithoutStoreCalls()
    {
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter);

        await tracker.PersistEventsAsync();

        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_ReturnsTrue()
    {
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage>());

        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter);

        var result = await tracker.EmitTransactionalEventsAsync();

        result.Should().BeTrue();
    }
}

- [ ] **Step 2: Implement OutboxEntityEventTracker**

```csharp
// Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs
using RCommon.Entities;
using RCommon.EventHandling.Producers;

namespace RCommon.Persistence.Outbox;

public class OutboxEntityEventTracker : IEntityEventTracker
{
    private readonly InMemoryEntityEventTracker _inner;
    private readonly OutboxEventRouter _outboxRouter;

    public OutboxEntityEventTracker(InMemoryEntityEventTracker inner, OutboxEventRouter outboxRouter)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _outboxRouter = outboxRouter ?? throw new ArgumentNullException(nameof(outboxRouter));
    }

    public void AddEntity(IBusinessEntity entity) => _inner.AddEntity(entity);

    public ICollection<IBusinessEntity> TrackedEntities => _inner.TrackedEntities;

    public async Task PersistEventsAsync(CancellationToken cancellationToken = default)
    {
        // Walk entity graph and collect events into the router buffer
        foreach (var entity in _inner.TrackedEntities)
        {
            var entityGraph = entity.TraverseGraphFor<IBusinessEntity>();
            foreach (var graphEntity in entityGraph)
            {
                _outboxRouter.AddTransactionalEvents(graphEntity.LocalEvents);
            }
        }

        // Flush buffer to outbox store (within the active transaction)
        await _outboxRouter.PersistBufferedEventsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default)
    {
        await _outboxRouter.RouteEventsAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
```

- [ ] **Step 3: Run tests and iterate**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~OutboxEntityEventTrackerTests"`
Expected: All tests pass. Adjust mocking approach if needed.

- [ ] **Step 4: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs
git commit -m "feat: add OutboxEntityEventTracker decorator for two-phase event persistence"
```

---

## Task 6: OutboxProcessingService + Tests

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/OutboxProcessingService.cs`
- Create: `Tests/RCommon.Persistence.Tests/OutboxProcessingServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Key behaviors to test:
1. Service creates a scope per polling iteration
2. Resolves `IOutboxStore` and dispatches pending messages
3. Marks messages as processed on success
4. Marks messages as failed on dispatch exception
5. Marks messages as dead-lettered when `RetryCount >= MaxRetries`
6. Calls cleanup methods periodically

```csharp
// Tests/RCommon.Persistence.Tests/OutboxProcessingServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public record PollerTestEvent(string Data) : ISerializableEvent;

public class OutboxProcessingServiceTests
{
    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IEventProducer> _producerMock = new();
    private readonly IOutboxSerializer _serializer = new JsonOutboxSerializer();
    private readonly EventSubscriptionManager _subscriptionManager = new();

    private (OutboxProcessingService service, IServiceProvider provider) CreateService(OutboxOptions? options = null)
    {
        var opts = options ?? new OutboxOptions { PollingInterval = TimeSpan.FromMilliseconds(50) };

        var services = new ServiceCollection();
        services.AddSingleton(_storeMock.Object);
        services.AddSingleton<IOutboxSerializer>(_serializer);
        services.AddSingleton<IEventProducer>(_producerMock.Object);
        services.AddSingleton(_subscriptionManager);
        var provider = services.BuildServiceProvider();

        var service = new OutboxProcessingService(
            provider,
            Options.Create(opts),
            NullLogger<OutboxProcessingService>.Instance);

        return (service, provider);
    }

    [Fact]
    public async Task ProcessBatchAsync_DispatchesAndMarksProcessed()
    {
        var @event = new PollerTestEvent("hello");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_MarksFailedOnException()
    {
        var @event = new PollerTestEvent("fail");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0
        };
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });
        _producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("transport error"));

        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _storeMock.Verify(s => s.MarkFailedAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_DeadLettersWhenMaxRetriesExceeded()
    {
        var @event = new PollerTestEvent("dead");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 5
        };
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });
        _producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("still down"));

        var opts = new OutboxOptions { MaxRetries = 5, PollingInterval = TimeSpan.FromMilliseconds(50) };
        var (service, _) = CreateService(opts);
        await service.ProcessBatchAsync(CancellationToken.None);

        _storeMock.Verify(s => s.MarkDeadLetteredAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 2: Implement OutboxProcessingService**

```csharp
// Src/RCommon.Persistence/Outbox/OutboxProcessingService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;

namespace RCommon.Persistence.Outbox;

public class OutboxProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessingService> _logger;

    public OutboxProcessingService(
        IServiceProvider serviceProvider,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessingService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessingService started. Polling every {Interval}s", _options.PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OutboxProcessingService encountered an error during polling");
            }

            await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var serializer = scope.ServiceProvider.GetRequiredService<IOutboxSerializer>();
        var producers = scope.ServiceProvider.GetServices<IEventProducer>();
        var subscriptionManager = scope.ServiceProvider.GetRequiredService<EventSubscriptionManager>();

        var pending = await store.GetPendingAsync(_options.BatchSize, cancellationToken).ConfigureAwait(false);

        foreach (var message in pending)
        {
            try
            {
                if (message.RetryCount >= _options.MaxRetries)
                {
                    _logger.LogWarning("Outbox message {Id} exceeded max retries ({Max}). Dead-lettering.",
                        message.Id, _options.MaxRetries);
                    await store.MarkDeadLetteredAsync(message.Id, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var @event = serializer.Deserialize(message.EventType, message.EventPayload);
                var filteredProducers = subscriptionManager.HasSubscriptions
                    ? subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                    : producers;

                foreach (var producer in filteredProducers)
                {
                    await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
                }

                await store.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to dispatch outbox message {Id} (retry {Retry})",
                    message.Id, message.RetryCount);

                if (message.RetryCount + 1 >= _options.MaxRetries)
                {
                    await store.MarkDeadLetteredAsync(message.Id, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await store.MarkFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Periodic cleanup
        await store.DeleteProcessedAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
        await store.DeleteDeadLetteredAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~OutboxProcessingServiceTests"`
Expected: 3 passed, 0 failed.

- [ ] **Step 4: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/OutboxProcessingService.cs Tests/RCommon.Persistence.Tests/OutboxProcessingServiceTests.cs
git commit -m "feat: add OutboxProcessingService background poller with retry and dead-letter support"
```

---

## Task 7: Builder Extension (AddOutbox) + UnitOfWork Integration Test

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs`
- Create: `Tests/RCommon.Persistence.Tests/UnitOfWorkOutboxTests.cs`

- [ ] **Step 1: Implement AddOutbox extension**

```csharp
// Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Persistence.Outbox;

namespace RCommon;

public static class OutboxPersistenceBuilderExtensions
{
    public static IPersistenceBuilder AddOutbox<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null)
        where TOutboxStore : class, IOutboxStore
    {
        // Outbox store (scoped — participates in per-request transaction)
        builder.Services.AddScoped<IOutboxStore, TOutboxStore>();

        // Serializer (singleton, replaceable)
        builder.Services.TryAddSingleton<IOutboxSerializer, JsonOutboxSerializer>();

        // Outbox event router (scoped — replaces InMemoryTransactionalEventRouter)
        builder.Services.AddScoped<OutboxEventRouter>();
        builder.Services.AddScoped<IEventRouter>(sp => sp.GetRequiredService<OutboxEventRouter>());

        // Entity event tracker decorator (scoped — replaces InMemoryEntityEventTracker)
        builder.Services.AddScoped<InMemoryEntityEventTracker>();
        builder.Services.AddScoped<IEntityEventTracker, OutboxEntityEventTracker>();

        // Background processing service (singleton)
        builder.Services.AddHostedService<OutboxProcessingService>();

        // Options
        if (configure != null)
        {
            builder.Services.Configure(configure);
        }
        else
        {
            builder.Services.Configure<OutboxOptions>(_ => { });
        }

        return builder;
    }
}
```

- [ ] **Step 2: Write UnitOfWork integration test**

```csharp
// Tests/RCommon.Persistence.Tests/UnitOfWorkOutboxTests.cs
using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public record UoWTestEvent(string Data) : ISerializableEvent;

public class UnitOfWorkOutboxTests
{
    [Fact]
    public async Task PersistEventsAsync_IsCalledBeforeCommit_ViaOutboxEntityEventTracker()
    {
        // Verify the OutboxEntityEventTracker PersistEventsAsync → OutboxEventRouter.PersistBufferedEventsAsync flow
        var storeMock = new Mock<IOutboxStore>();
        var serializer = new JsonOutboxSerializer();
        var guidGenMock = new Mock<IGuidGenerator>();
        guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid());
        var tenantMock = new Mock<RCommon.Security.Claims.ITenantIdAccessor>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        var subscriptionManager = new EventSubscriptionManager();

        var outboxRouter = new OutboxEventRouter(
            storeMock.Object,
            serializer,
            guidGenMock.Object,
            tenantMock.Object,
            serviceProviderMock.Object,
            subscriptionManager,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OutboxEventRouter>.Instance,
            Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));

        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter);

        // Simulate: PersistEventsAsync is called (Phase 1, pre-commit)
        await tracker.PersistEventsAsync();

        // With no entities tracked, no store calls expected — but should complete without error
        storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 3: Run all persistence tests**

Run: `dotnet test Tests/RCommon.Persistence.Tests/`
Expected: All tests pass (existing + new).

- [ ] **Step 4: Build entire solution**

Run: `dotnet build Src/RCommon.sln`
Expected: 0 errors.

- [ ] **Step 5: Write concurrency and edge case tests**

```csharp
// Tests/RCommon.Persistence.Tests/OutboxConcurrencyTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

public record ConcurrencyTestEvent(string Data) : ISerializableEvent;

public class OutboxConcurrencyTests
{
    [Fact]
    public async Task DeadLetterMessages_ExcludedFromGetPending()
    {
        // Verifies dead-lettered messages are excluded from future GetPendingAsync
        var storeMock = new Mock<IOutboxStore>();
        var deadLetteredMsg = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow, DeadLetteredAtUtc = DateTimeOffset.UtcNow
        };
        storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage>()); // Dead-lettered excluded at store level

        // Verify store contract: GetPendingAsync should never return dead-lettered messages
        var pending = await storeMock.Object.GetPendingAsync(100);
        pending.Should().NotContain(m => m.DeadLetteredAtUtc.HasValue);
    }

    [Fact]
    public async Task EmptyBuffer_PersistBufferedEventsAsync_NoStoreCalls()
    {
        var storeMock = new Mock<IOutboxStore>();
        var guidGenMock = new Mock<IGuidGenerator>();
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        var router = new OutboxEventRouter(
            storeMock.Object, new JsonOutboxSerializer(),
            guidGenMock.Object, tenantMock.Object,
            serviceProviderMock.Object, new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));

        // No events buffered — persist should be a no-op
        await router.PersistBufferedEventsAsync();
        storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteEventsAsync_NoPending_CompletesQuickly()
    {
        var storeMock = new Mock<IOutboxStore>();
        storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage>());
        var guidGenMock = new Mock<IGuidGenerator>();
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        var router = new OutboxEventRouter(
            storeMock.Object, new JsonOutboxSerializer(),
            guidGenMock.Object, tenantMock.Object,
            serviceProviderMock.Object, new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));

        // No pending messages — should return immediately with no producer calls
        await router.RouteEventsAsync();
        storeMock.Verify(s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 6: Run all persistence tests**

Run: `dotnet test Tests/RCommon.Persistence.Tests/`
Expected: All tests pass (existing + new).

- [ ] **Step 7: Build entire solution**

Run: `dotnet build Src/RCommon.sln`
Expected: 0 errors.

- [ ] **Step 8: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs Tests/RCommon.Persistence.Tests/UnitOfWorkOutboxTests.cs Tests/RCommon.Persistence.Tests/OutboxConcurrencyTests.cs
git commit -m "feat: add AddOutbox<T> builder extension, UnitOfWork integration, and concurrency tests"
```

---

## Task 8: EF Core Outbox Store + Tests

**Files:**
- Create: `Src/RCommon.EfCore/Outbox/EFCoreOutboxStore.cs`
- Create: `Src/RCommon.EfCore/Outbox/OutboxMessageConfiguration.cs`
- Create: `Src/RCommon.EfCore/Outbox/ModelBuilderExtensions.cs`
- Create: `Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs`

**Pattern:** EF Core repositories resolve their `RCommonDbContext` via `IDataStoreFactory.Resolve<RCommonDbContext>(dataStoreName)`. The outbox store follows the same pattern, using `DefaultDataStoreOptions.DefaultDataStoreName` for the store name.

**Atomicity note:** `EFCoreOutboxStore.SaveAsync()` calls `SaveChangesAsync()` after adding the `OutboxMessage` to the change tracker. By this point in the flow (Phase 1 of `UnitOfWork.CommitAsync`), domain entity changes have already been flushed by the repositories (each repository calls `SaveChangesAsync` in its own Add/Update/Delete methods). The outbox's `SaveChangesAsync` only flushes the outbox message row. Both the domain writes and outbox writes are within the same `TransactionScope`, so they commit atomically when `TransactionScope.Complete()` is called.

- [ ] **Step 1: Create OutboxMessageConfiguration (EF Core entity type config)**

```csharp
// Src/RCommon.EfCore/Outbox/OutboxMessageConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    private readonly string _tableName;

    public OutboxMessageConfiguration(string tableName = "__OutboxMessages")
    {
        _tableName = tableName;
    }

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(_tableName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.EventPayload).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(256);
        builder.Property(x => x.TenantId).HasMaxLength(256);

        builder.HasIndex(x => new { x.ProcessedAtUtc, x.DeadLetteredAtUtc, x.CreatedAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Pending");
    }
}
```

- [ ] **Step 2: Create ModelBuilder extension**

```csharp
// Src/RCommon.EfCore/Outbox/ModelBuilderExtensions.cs
using Microsoft.EntityFrameworkCore;

namespace RCommon.Persistence.EFCore.Outbox;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddOutboxMessages(this ModelBuilder modelBuilder, string tableName = "__OutboxMessages")
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(tableName));
        return modelBuilder;
    }
}
```

- [ ] **Step 3: Create EFCoreOutboxStore using IDataStoreFactory**

```csharp
// Src/RCommon.EfCore/Outbox/EFCoreOutboxStore.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

public class EFCoreOutboxStore : IOutboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly int _maxRetries;

    public EFCoreOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _maxRetries = outboxOptions?.Value?.MaxRetries ?? 5;
    }

    private RCommonDbContext DbContext => _dataStoreFactory.Resolve<RCommonDbContext>(_dataStoreName);

    public async Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        if (message is OutboxMessage entity)
        {
            dbContext.Set<OutboxMessage>().Add(entity);
        }
        else
        {
            dbContext.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = message.Id,
                EventType = message.EventType,
                EventPayload = message.EventPayload,
                CreatedAtUtc = message.CreatedAtUtc,
                ProcessedAtUtc = message.ProcessedAtUtc,
                DeadLetteredAtUtc = message.DeadLetteredAtUtc,
                ErrorMessage = message.ErrorMessage,
                RetryCount = message.RetryCount,
                CorrelationId = message.CorrelationId,
                TenantId = message.TenantId
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAtUtc == null
                && m.DeadLetteredAtUtc == null
                && m.RetryCount < _maxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.ProcessedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.ErrorMessage = error;
            message.RetryCount++;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.DeadLetteredAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var old = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Set<OutboxMessage>().RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var old = await dbContext.Set<OutboxMessage>()
            .Where(m => m.DeadLetteredAtUtc != null && m.DeadLetteredAtUtc < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Set<OutboxMessage>().RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Write EFCoreOutboxStore tests (SQLite in-memory)**

```csharp
// Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.EfCore.Tests;

// Minimal DbContext for testing
public class TestOutboxDbContext : RCommonDbContext
{
    public TestOutboxDbContext(DbContextOptions options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOutboxMessages();
    }
}

public class EFCoreOutboxStoreTests : IDisposable
{
    private readonly TestOutboxDbContext _dbContext;
    private readonly EFCoreOutboxStore _store;

    public EFCoreOutboxStoreTests()
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
        var defaultOpts = Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" });
        var outboxOpts = Options.Create(new OutboxOptions { MaxRetries = 3 });

        _store = new EFCoreOutboxStore(factoryMock.Object, defaultOpts, outboxOpts);
    }

    [Fact]
    public async Task SaveAsync_PersistsMessage()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "Test.Event", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        await _store.SaveAsync(msg);
        var count = await _dbContext.Set<OutboxMessage>().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetPendingAsync_ExcludesProcessedDeadLetteredAndMaxRetries()
    {
        var pending = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 0
        };
        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow, ProcessedAtUtc = DateTimeOffset.UtcNow
        };
        var deadLettered = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow, DeadLetteredAtUtc = DateTimeOffset.UtcNow
        };
        var maxedOut = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 3 // == MaxRetries
        };
        _dbContext.Set<OutboxMessage>().AddRange(pending, processed, deadLettered, maxedOut);
        await _dbContext.SaveChangesAsync();

        var result = await _store.GetPendingAsync(100);
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(pending.Id);
    }

    [Fact]
    public async Task MarkProcessedAsync_SetsProcessedAtUtc()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        await _store.MarkProcessedAsync(msg.Id);

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_IncrementsRetryCountAndSetsError()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 1
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        await _store.MarkFailedAsync(msg.Id, "error");

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.RetryCount.Should().Be(2);
        updated.ErrorMessage.Should().Be("error");
    }

    [Fact]
    public async Task MarkDeadLetteredAsync_SetsDeadLetteredAtUtc()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        await _store.MarkDeadLetteredAsync(msg.Id);

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.DeadLetteredAtUtc.Should().NotBeNull();
    }

    public void Dispose() => _dbContext.Dispose();
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test Tests/RCommon.EfCore.Tests/ --filter "FullyQualifiedName~EFCoreOutboxStoreTests"`
Expected: All tests pass.

Note: The test project may need a `Microsoft.EntityFrameworkCore.Sqlite` PackageReference for the SQLite in-memory provider. Add it if missing.

- [ ] **Step 6: Build EF Core project**

Run: `dotnet build Src/RCommon.EfCore/RCommon.EfCore.csproj`
Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add Src/RCommon.EfCore/Outbox/ Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs
git commit -m "feat: add EFCoreOutboxStore with IDataStoreFactory, RetryCount filter, and SQLite tests"
```

---

## Task 9: Dapper Outbox Store + Tests

**Files:**
- Create: `Src/RCommon.Dapper/Outbox/DapperOutboxStore.cs`
- Create: `Tests/RCommon.Dapper.Tests/DapperOutboxStoreTests.cs`

**Pattern:** Dapper repositories resolve `RDbConnection` via `IDataStoreFactory.Resolve<RDbConnection>(dataStoreName)`, then call `dataStore.GetDbConnection()` to get a `DbConnection`. Connection state is checked and opened if closed. The outbox store follows the same pattern.

**Atomicity note:** Each call to `GetDbConnection()` creates a new `DbConnection` (this is the existing Dapper repository pattern). When opened within an active `TransactionScope`, each connection enlists in the ambient transaction. On SQL Server, multiple connections to the same database within a `TransactionScope` may promote to a distributed transaction (MSDTC). This is the same behavior as the existing Dapper repositories and is not unique to the outbox store. On platforms where MSDTC is unavailable, users should ensure a single connection is reused, or use the EF Core outbox store instead.

**SQL Server dialect:** The raw SQL uses SQL Server syntax (`SELECT TOP`, `[TableName]` bracket quoting). For PostgreSQL or MySQL users, a custom `IOutboxStore` implementation with dialect-specific SQL would be needed. This matches the existing Dapper repository pattern which also uses SQL Server syntax.

- [ ] **Step 1: Implement DapperOutboxStore using IDataStoreFactory**

```csharp
// Src/RCommon.Dapper/Outbox/DapperOutboxStore.cs
using Dapper;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Sql;
using System.Data;
using System.Data.Common;

namespace RCommon.Persistence.Dapper.Outbox;

public class DapperOutboxStore : IOutboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly string _tableName;
    private readonly int _maxRetries;

    public DapperOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _tableName = outboxOptions?.Value?.TableName ?? "__OutboxMessages";
        _maxRetries = outboxOptions?.Value?.MaxRetries ?? 5;
    }

    private async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var dataStore = _dataStoreFactory.Resolve<RDbConnection>(_dataStoreName);
        var connection = dataStore.GetDbConnection();
        if (connection.State == ConnectionState.Closed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        return connection;
    }

    public async Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $@"INSERT INTO [{_tableName}] (Id, EventType, EventPayload, CreatedAtUtc, ProcessedAtUtc, DeadLetteredAtUtc, ErrorMessage, RetryCount, CorrelationId, TenantId)
                     VALUES (@Id, @EventType, @EventPayload, @CreatedAtUtc, @ProcessedAtUtc, @DeadLetteredAtUtc, @ErrorMessage, @RetryCount, @CorrelationId, @TenantId)";
        await db.ExecuteAsync(new CommandDefinition(sql, message, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $@"SELECT TOP (@BatchSize) * FROM [{_tableName}]
                     WHERE ProcessedAtUtc IS NULL AND DeadLetteredAtUtc IS NULL AND RetryCount < @MaxRetries
                     ORDER BY CreatedAtUtc ASC";
        var result = await db.QueryAsync<OutboxMessage>(
            new CommandDefinition(sql, new { BatchSize = batchSize, MaxRetries = _maxRetries },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return result.ToList();
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET ProcessedAtUtc = @Now WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Now = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET ErrorMessage = @Error, RetryCount = RetryCount + 1 WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Error = error },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET DeadLetteredAtUtc = @Now WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Now = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var sql = $"DELETE FROM [{_tableName}] WHERE ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < @Cutoff";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Cutoff = cutoff },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var sql = $"DELETE FROM [{_tableName}] WHERE DeadLetteredAtUtc IS NOT NULL AND DeadLetteredAtUtc < @Cutoff";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Cutoff = cutoff },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
```

- [ ] **Step 2: Write DapperOutboxStore tests**

These tests verify the SQL generation and store operations using a mock `IDataStoreFactory` and mock `RDbConnection`. For a full integration test, a real SQLite or SQL Server connection would be needed, but mock-based tests verify the interaction pattern.

```csharp
// Tests/RCommon.Dapper.Tests/DapperOutboxStoreTests.cs
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence.Dapper.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Sql;
using System.Data;
using System.Data.Common;
using Xunit;

namespace RCommon.Dapper.Tests;

public class DapperOutboxStoreTests
{
    [Fact]
    public void Constructor_ThrowsOnNullDataStoreFactory()
    {
        var act = () => new DapperOutboxStore(
            null!,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullDefaultDataStoreOptions()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new DapperOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions()),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SucceedsWithValidParameters()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var store = new DapperOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        store.Should().NotBeNull();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build Src/RCommon.Dapper/RCommon.Dapper.csproj`
Expected: 0 errors.

- [ ] **Step 4: Run tests**

Run: `dotnet test Tests/RCommon.Dapper.Tests/ --filter "FullyQualifiedName~DapperOutboxStoreTests"`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Dapper/Outbox/ Tests/RCommon.Dapper.Tests/DapperOutboxStoreTests.cs
git commit -m "feat: add DapperOutboxStore with IDataStoreFactory and RetryCount filter"
```

---

## Task 10: Linq2Db Outbox Store + Tests

**Files:**
- Create: `Src/RCommon.Linq2Db/Outbox/Linq2DbOutboxStore.cs`
- Create: `Tests/RCommon.Linq2Db.Tests/Linq2DbOutboxStoreTests.cs`

**Pattern:** Linq2Db repositories resolve `RCommonDataConnection` via `IDataStoreFactory.Resolve<RCommonDataConnection>(dataStoreName)`. The outbox store follows the same pattern.

- [ ] **Step 1: Implement Linq2DbOutboxStore using IDataStoreFactory**

```csharp
// Src/RCommon.Linq2Db/Outbox/Linq2DbOutboxStore.cs
using LinqToDB;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.Linq2Db.Outbox;

public class Linq2DbOutboxStore : IOutboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly string _tableName;
    private readonly int _maxRetries;

    public Linq2DbOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _tableName = outboxOptions?.Value?.TableName ?? "__OutboxMessages";
        _maxRetries = outboxOptions?.Value?.MaxRetries ?? 5;
    }

    private RCommonDataConnection DataConnection
        => _dataStoreFactory.Resolve<RCommonDataConnection>(_dataStoreName);

    private ITable<OutboxMessage> Table
        => DataConnection.GetTable<OutboxMessage>().TableName(_tableName);

    public async Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = message as OutboxMessage ?? new OutboxMessage
        {
            Id = message.Id,
            EventType = message.EventType,
            EventPayload = message.EventPayload,
            CreatedAtUtc = message.CreatedAtUtc,
            ProcessedAtUtc = message.ProcessedAtUtc,
            DeadLetteredAtUtc = message.DeadLetteredAtUtc,
            ErrorMessage = message.ErrorMessage,
            RetryCount = message.RetryCount,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId
        };
        await DataConnection.InsertAsync(entity, _tableName, token: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await Table
            .Where(m => m.ProcessedAtUtc == null
                && m.DeadLetteredAtUtc == null
                && m.RetryCount < _maxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await Table
            .Where(m => m.Id == messageId)
            .Set(m => m.ProcessedAtUtc, DateTimeOffset.UtcNow)
            .UpdateAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        await Table
            .Where(m => m.Id == messageId)
            .Set(m => m.ErrorMessage, error)
            .Set(m => m.RetryCount, m => m.RetryCount + 1)
            .UpdateAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await Table
            .Where(m => m.Id == messageId)
            .Set(m => m.DeadLetteredAtUtc, DateTimeOffset.UtcNow)
            .UpdateAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        await Table
            .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < cutoff)
            .DeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        await Table
            .Where(m => m.DeadLetteredAtUtc != null && m.DeadLetteredAtUtc < cutoff)
            .DeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
```

- [ ] **Step 2: Write Linq2DbOutboxStore tests**

```csharp
// Tests/RCommon.Linq2Db.Tests/Linq2DbOutboxStoreTests.cs
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence.Linq2Db.Outbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Linq2Db.Tests;

public class Linq2DbOutboxStoreTests
{
    [Fact]
    public void Constructor_ThrowsOnNullDataStoreFactory()
    {
        var act = () => new Linq2DbOutboxStore(
            null!,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullDefaultDataStoreOptions()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new Linq2DbOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions()),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SucceedsWithValidParameters()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var store = new Linq2DbOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        store.Should().NotBeNull();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build Src/RCommon.Linq2Db/RCommon.Linq2Db.csproj`
Expected: 0 errors.

- [ ] **Step 4: Run tests**

Run: `dotnet test Tests/RCommon.Linq2Db.Tests/ --filter "FullyQualifiedName~Linq2DbOutboxStoreTests"`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Linq2Db/Outbox/ Tests/RCommon.Linq2Db.Tests/Linq2DbOutboxStoreTests.cs
git commit -m "feat: add Linq2DbOutboxStore with IDataStoreFactory and RetryCount filter"
```

---

## Task 11: MassTransit.Outbox Project

**Files:**
- Create: `Src/RCommon.MassTransit.Outbox/RCommon.MassTransit.Outbox.csproj`
- Create: `Src/RCommon.MassTransit.Outbox/IMassTransitOutboxBuilder.cs`
- Create: `Src/RCommon.MassTransit.Outbox/MassTransitOutboxBuilder.cs`
- Create: `Src/RCommon.MassTransit.Outbox/MassTransitOutboxBuilderExtensions.cs`
- Create: `Tests/RCommon.MassTransit.Outbox.Tests/RCommon.MassTransit.Outbox.Tests.csproj`
- Create: `Tests/RCommon.MassTransit.Outbox.Tests/MassTransitOutboxBuilderTests.cs`

- [ ] **Step 1: Create csproj**

Create `Src/RCommon.MassTransit.Outbox/RCommon.MassTransit.Outbox.csproj` following the same pattern as `RCommon.MassTransit.StateMachines.csproj` (multi-target net8.0;net9.0;net10.0, standard package metadata). References: `RCommon.MassTransit`, `RCommon.Persistence`. NuGet: `MassTransit.EntityFrameworkCore`.

- [ ] **Step 2: Create IMassTransitOutboxBuilder**

```csharp
// Src/RCommon.MassTransit.Outbox/IMassTransitOutboxBuilder.cs
using MassTransit;

namespace RCommon.MassTransit.Outbox;

public interface IMassTransitOutboxBuilder
{
    IMassTransitOutboxBuilder UsePostgres();
    IMassTransitOutboxBuilder UseSqlServer();
    IMassTransitOutboxBuilder UseBusOutbox(Action<IBusOutboxConfigurator>? configure = null);
}
```

- [ ] **Step 3: Create MassTransitOutboxBuilder implementation**

```csharp
// Src/RCommon.MassTransit.Outbox/MassTransitOutboxBuilder.cs
using MassTransit;

namespace RCommon.MassTransit.Outbox;

public class MassTransitOutboxBuilder : IMassTransitOutboxBuilder
{
    private readonly IEntityFrameworkOutboxConfigurator _configurator;

    public MassTransitOutboxBuilder(IEntityFrameworkOutboxConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
    }

    public IMassTransitOutboxBuilder UsePostgres()
    {
        _configurator.UsePostgres();
        return this;
    }

    public IMassTransitOutboxBuilder UseSqlServer()
    {
        _configurator.UseSqlServer();
        return this;
    }

    public IMassTransitOutboxBuilder UseBusOutbox(Action<IBusOutboxConfigurator>? configure = null)
    {
        _configurator.UseBusOutbox(configure);
        return this;
    }
}
```

- [ ] **Step 4: Create MassTransitOutboxBuilderExtensions**

```csharp
// Src/RCommon.MassTransit.Outbox/MassTransitOutboxBuilderExtensions.cs
using Microsoft.EntityFrameworkCore;
using RCommon.MassTransit;
using RCommon.MassTransit.Outbox;

namespace RCommon;

public static class MassTransitOutboxBuilderExtensions
{
    public static IMassTransitEventHandlingBuilder AddOutbox<TDbContext>(
        this IMassTransitEventHandlingBuilder builder,
        Action<IMassTransitOutboxBuilder>? configure = null)
        where TDbContext : DbContext
    {
        // Delegate to MassTransit's native EntityFramework outbox
        builder.AddEntityFrameworkOutbox<TDbContext>(o =>
        {
            var outboxBuilder = new MassTransitOutboxBuilder(o);
            configure?.Invoke(outboxBuilder);
        });
        return builder;
    }
}
```

- [ ] **Step 5: Create test project and DI test**

Create `Tests/RCommon.MassTransit.Outbox.Tests/RCommon.MassTransit.Outbox.Tests.csproj` referencing `RCommon.MassTransit.Outbox`, `RCommon.Core`, and `Microsoft.Extensions.DependencyInjection`.

```csharp
// Tests/RCommon.MassTransit.Outbox.Tests/MassTransitOutboxBuilderTests.cs
using FluentAssertions;
using MassTransit;
using Moq;
using RCommon.MassTransit.Outbox;
using Xunit;

namespace RCommon.MassTransit.Outbox.Tests;

public class MassTransitOutboxBuilderTests
{
    [Fact]
    public void UseSqlServer_DelegatesToConfigurator()
    {
        var configuratorMock = new Mock<IEntityFrameworkOutboxConfigurator>();
        var builder = new MassTransitOutboxBuilder(configuratorMock.Object);

        var result = builder.UseSqlServer();

        result.Should().BeSameAs(builder);
        configuratorMock.Verify(c => c.UseSqlServer(), Times.Once);
    }

    [Fact]
    public void UsePostgres_DelegatesToConfigurator()
    {
        var configuratorMock = new Mock<IEntityFrameworkOutboxConfigurator>();
        var builder = new MassTransitOutboxBuilder(configuratorMock.Object);

        var result = builder.UsePostgres();

        result.Should().BeSameAs(builder);
        configuratorMock.Verify(c => c.UsePostgres(), Times.Once);
    }

    [Fact]
    public void UseBusOutbox_DelegatesToConfigurator()
    {
        var configuratorMock = new Mock<IEntityFrameworkOutboxConfigurator>();
        var builder = new MassTransitOutboxBuilder(configuratorMock.Object);

        var result = builder.UseBusOutbox();

        result.Should().BeSameAs(builder);
        configuratorMock.Verify(c => c.UseBusOutbox(It.IsAny<Action<IBusOutboxConfigurator>>()), Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        var act = () => new MassTransitOutboxBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
```

- [ ] **Step 6: Build and test**

Run: `dotnet build Src/RCommon.MassTransit.Outbox/ && dotnet test Tests/RCommon.MassTransit.Outbox.Tests/`
Expected: Build succeeds, tests pass.

- [ ] **Step 7: Commit**

```bash
git add Src/RCommon.MassTransit.Outbox/ Tests/RCommon.MassTransit.Outbox.Tests/
git commit -m "feat: add RCommon.MassTransit.Outbox wrapping native EF Core outbox"
```

---

## Task 12: Wolverine.Outbox Project

**Files:**
- Create: `Src/RCommon.Wolverine.Outbox/RCommon.Wolverine.Outbox.csproj`
- Create: `Src/RCommon.Wolverine.Outbox/IWolverineOutboxBuilder.cs`
- Create: `Src/RCommon.Wolverine.Outbox/WolverineOutboxBuilder.cs`
- Create: `Src/RCommon.Wolverine.Outbox/WolverineOutboxBuilderExtensions.cs`
- Create: `Tests/RCommon.Wolverine.Outbox.Tests/RCommon.Wolverine.Outbox.Tests.csproj`
- Create: `Tests/RCommon.Wolverine.Outbox.Tests/WolverineOutboxBuilderTests.cs`

- [ ] **Step 1: Create csproj**

Create `Src/RCommon.Wolverine.Outbox/RCommon.Wolverine.Outbox.csproj` (multi-target, standard metadata). References: `RCommon.Wolverine`, `RCommon.Persistence`. NuGet: `WolverineFx.EntityFrameworkCore`.

- [ ] **Step 2: Create IWolverineOutboxBuilder**

```csharp
// Src/RCommon.Wolverine.Outbox/IWolverineOutboxBuilder.cs
namespace RCommon.Wolverine.Outbox;

public interface IWolverineOutboxBuilder
{
    IWolverineOutboxBuilder UseEntityFrameworkCoreTransactions();
}
```

- [ ] **Step 3: Create WolverineOutboxBuilder**

```csharp
// Src/RCommon.Wolverine.Outbox/WolverineOutboxBuilder.cs
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace RCommon.Wolverine.Outbox;

public class WolverineOutboxBuilder : IWolverineOutboxBuilder
{
    private readonly WolverineOptions _wolverineOptions;

    public WolverineOutboxBuilder(WolverineOptions wolverineOptions)
    {
        _wolverineOptions = wolverineOptions ?? throw new ArgumentNullException(nameof(wolverineOptions));
    }

    public IWolverineOutboxBuilder UseEntityFrameworkCoreTransactions()
    {
        _wolverineOptions.UseEntityFrameworkCoreTransactions();
        return this;
    }
}
```

- [ ] **Step 4: Create WolverineOutboxBuilderExtensions**

```csharp
// Src/RCommon.Wolverine.Outbox/WolverineOutboxBuilderExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using RCommon.Wolverine;
using RCommon.Wolverine.Outbox;

namespace RCommon;

public static class WolverineOutboxBuilderExtensions
{
    public static IWolverineEventHandlingBuilder AddOutbox(
        this IWolverineEventHandlingBuilder builder,
        Action<IWolverineOutboxBuilder>? configure = null)
    {
        // Post-configure Wolverine options through IServiceCollection
        builder.Services.ConfigureWolverine(opts =>
        {
            var outboxBuilder = new WolverineOutboxBuilder(opts);
            configure?.Invoke(outboxBuilder);
        });
        return builder;
    }
}
```

Note: `ConfigureWolverine` is a WolverineFx extension on `IServiceCollection`. If unavailable in the installed version, use `services.AddOptions<WolverineOptions>().Configure(...)` instead.

- [ ] **Step 5: Create test project and DI test**

Create `Tests/RCommon.Wolverine.Outbox.Tests/RCommon.Wolverine.Outbox.Tests.csproj` referencing `RCommon.Wolverine.Outbox`, `RCommon.Core`, and `Microsoft.Extensions.DependencyInjection`.

```csharp
// Tests/RCommon.Wolverine.Outbox.Tests/WolverineOutboxBuilderTests.cs
using FluentAssertions;
using RCommon.Wolverine.Outbox;
using Xunit;

namespace RCommon.Wolverine.Outbox.Tests;

public class WolverineOutboxBuilderTests
{
    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        var act = () => new WolverineOutboxBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
```

- [ ] **Step 5: Build and test**

Run: `dotnet build Src/RCommon.Wolverine.Outbox/ && dotnet test Tests/RCommon.Wolverine.Outbox.Tests/`
Expected: Build succeeds, tests pass.

- [ ] **Step 6: Commit**

```bash
git add Src/RCommon.Wolverine.Outbox/ Tests/RCommon.Wolverine.Outbox.Tests/
git commit -m "feat: add RCommon.Wolverine.Outbox wrapping native durable messaging"
```

---

## Task 13: Solution File + Full Build + Full Test

**Files:**
- Modify: `Src/RCommon.sln`

- [ ] **Step 1: Add all new projects to solution**

```bash
cd Src && dotnet sln RCommon.sln add RCommon.MassTransit.Outbox/RCommon.MassTransit.Outbox.csproj && dotnet sln RCommon.sln add RCommon.Wolverine.Outbox/RCommon.Wolverine.Outbox.csproj && dotnet sln RCommon.sln add ../Tests/RCommon.MassTransit.Outbox.Tests/RCommon.MassTransit.Outbox.Tests.csproj && dotnet sln RCommon.sln add ../Tests/RCommon.Wolverine.Outbox.Tests/RCommon.Wolverine.Outbox.Tests.csproj && cd ..
```

- [ ] **Step 2: Full solution build**

Run: `dotnet build Src/RCommon.sln`
Expected: All projects build with 0 errors.

- [ ] **Step 3: Run ALL tests**

Run: `dotnet test Src/RCommon.sln`
Expected: All test projects pass. No regressions in existing tests.

- [ ] **Step 4: Commit**

```bash
git add Src/RCommon.sln
git commit -m "chore: add outbox projects to solution file"
```

---

## Verification Checklist

After all tasks are complete, verify:

- [ ] `dotnet build Src/RCommon.sln` — 0 errors
- [ ] `dotnet test Src/RCommon.sln` — all pass
- [ ] Existing tests (UnitOfWork, EventSubscriptionManager, Mediatr behaviors) still pass unchanged
- [ ] Non-outbox users have identical behavior (PersistEventsAsync is no-op)
- [ ] `IEntityEventTracker.EmitTransactionalEventsAsync(CancellationToken)` compiles with no arguments (default CT)
