# DDD Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement IAggregateRepository, automatic domain event dispatch, read-model repositories, and saga/state machine infrastructure for RCommon's DDD support.

**Architecture:** Four layered capabilities built bottom-up: (1) IAggregateRepository with compile-time aggregate enforcement and ORM implementations, (2) UnitOfWork post-commit event dispatch, (3) IReadModelRepository for CQRS query-side with IPagedResult, (4) IStateMachine abstraction + ISaga orchestration with ISagaStore persistence. Each part is independently testable and builds on existing repository/event infrastructure.

**Tech Stack:** C# (.NET 8/9/10 multi-target), xUnit, FluentAssertions, Moq, EF Core, Dapper/Dommel, Linq2Db, MediatR

**Spec:** `docs/superpowers/specs/2026-03-17-aggregate-repository-design.md`

**Solution:** `Src/RCommon.sln`

**Important:** Do NOT commit after implementation steps. The user will commit manually.

---

## File Structure

### Part 1: Aggregate Repository
| File | Action | Responsibility |
|------|--------|----------------|
| `Src/RCommon.Persistence/Crud/IAggregateRepository.cs` | Create | Interface with DDD constraints |
| `Src/RCommon.EfCore/Crud/EFCoreAggregateRepository.cs` | Create | EF Core implementation |
| `Src/RCommon.Dapper/Crud/DapperAggregateRepository.cs` | Create | Dapper implementation |
| `Src/RCommon.Linq2Db/Crud/Linq2DbAggregateRepository.cs` | Create | Linq2Db implementation |
| `Src/RCommon.EfCore/EFCorePerisistenceBuilder.cs` | Modify | Add open-generic DI registration |
| `Src/RCommon.Dapper/DapperPersistenceBuilder.cs` | Modify | Add open-generic DI registration |
| `Src/RCommon.Linq2Db/Linq2DbPersistenceBuilder.cs` | Modify | Add open-generic DI registration |
| `Tests/RCommon.Persistence.Tests/IAggregateRepositoryTests.cs` | Create | Interface constraint tests |

### Part 2: Domain Event Dispatch
| File | Action | Responsibility |
|------|--------|----------------|
| `Src/RCommon.Persistence/Transactions/IUnitOfWork.cs` | Modify | Add CommitAsync, mark Commit obsolete |
| `Src/RCommon.Persistence/Transactions/UnitOfWork.cs` | Modify | Implement CommitAsync with post-commit dispatch |
| `Src/RCommon.Mediatr/Behaviors/UnitOfWorkBehavior.cs` | Modify | Migrate to CommitAsync |
| `Tests/RCommon.Persistence.Tests/UnitOfWorkCommitAsyncTests.cs` | Create | CommitAsync event dispatch tests |

### Part 3: Read-Model Repositories
| File | Action | Responsibility |
|------|--------|----------------|
| `Src/RCommon.Models/IPagedResult.cs` | Create | Paged result interface |
| `Src/RCommon.Models/PagedResult.cs` | Create | Paged result implementation |
| `Src/RCommon.Persistence/IReadModel.cs` | Create | Marker interface |
| `Src/RCommon.Persistence/Crud/IReadModelRepository.cs` | Create | Read-model repository interface |
| `Src/RCommon.EfCore/Crud/EFCoreReadModelRepository.cs` | Create | EF Core read-model implementation |
| `Src/RCommon.Dapper/Crud/DapperReadModelRepository.cs` | Create | Dapper read-model implementation |
| `Src/RCommon.Linq2Db/Crud/Linq2DbReadModelRepository.cs` | Create | Linq2Db read-model implementation |
| `Tests/RCommon.Models.Tests/PagedResultTests.cs` | Create | PagedResult unit tests |
| `Tests/RCommon.Persistence.Tests/IReadModelRepositoryTests.cs` | Create | Interface constraint tests |

### Part 4: State Machines + Sagas
| File | Action | Responsibility |
|------|--------|----------------|
| `Src/RCommon.Core/StateMachines/IStateMachine.cs` | Create | State machine abstraction |
| `Src/RCommon.Core/StateMachines/IStateMachineConfigurator.cs` | Create | Fluent configuration builder |
| `Src/RCommon.Core/StateMachines/IStateConfigurator.cs` | Create | Per-state configuration |
| `Src/RCommon.Persistence/Sagas/SagaState.cs` | Create | Saga state base class |
| `Src/RCommon.Persistence/Sagas/ISaga.cs` | Create | Saga orchestrator interface |
| `Src/RCommon.Persistence/Sagas/SagaOrchestrator.cs` | Create | Abstract orchestrator base |
| `Src/RCommon.Persistence/Sagas/ISagaStore.cs` | Create | Saga persistence interface |
| `Src/RCommon.Persistence/Sagas/InMemorySagaStore.cs` | Create | In-memory saga store |
| `Src/RCommon.EfCore/Sagas/EFCoreSagaStore.cs` | Create | EF Core saga store |
| `Src/RCommon.Dapper/Sagas/DapperSagaStore.cs` | Create | Dapper saga store |
| `Src/RCommon.Linq2Db/Sagas/Linq2DbSagaStore.cs` | Create | Linq2Db saga store |
| `Tests/RCommon.Core.Tests/StateMachineInterfaceTests.cs` | Create | Interface shape tests |
| `Tests/RCommon.Persistence.Tests/SagaOrchestratorTests.cs` | Create | Orchestrator unit tests |
| `Tests/RCommon.Persistence.Tests/InMemorySagaStoreTests.cs` | Create | In-memory store tests |

---

## Chunk 1: Aggregate Repository + Domain Event Dispatch

### Task 1: IAggregateRepository Interface

**Files:**
- Create: `Src/RCommon.Persistence/Crud/IAggregateRepository.cs`
- Test: `Tests/RCommon.Persistence.Tests/IAggregateRepositoryTests.cs`

**Context:** The interface constrains `TAggregate` to `IAggregateRoot<TKey>` (defined in `Src/RCommon.Entities/IAggregateRoot.cs`). It inherits `INamedDataSource` (defined in `Src/RCommon.Persistence/INamedDataSource.cs`) for multi-database targeting. It does NOT inherit from any existing repository interface.

- [ ] **Step 1: Write the interface constraint test**

```csharp
// Tests/RCommon.Persistence.Tests/IAggregateRepositoryTests.cs
using System;
using System.Reflection;
using FluentAssertions;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using Xunit;

namespace RCommon.Persistence.Tests;

public class IAggregateRepositoryTests
{
    [Fact]
    public void Interface_Has_IAggregateRoot_Constraint_On_TAggregate()
    {
        var type = typeof(IAggregateRepository<,>);
        var genericArgs = type.GetGenericArguments();
        var tAggregate = genericArgs[0];
        var constraints = tAggregate.GetGenericParameterConstraints();

        constraints.Should().Contain(t => t.IsGenericType
            && t.GetGenericTypeDefinition() == typeof(IAggregateRoot<>),
            "TAggregate must be constrained to IAggregateRoot<TKey>");
    }

    [Fact]
    public void Interface_Has_IEquatable_Constraint_On_TKey()
    {
        var type = typeof(IAggregateRepository<,>);
        var genericArgs = type.GetGenericArguments();
        var tKey = genericArgs[1];
        var constraints = tKey.GetGenericParameterConstraints();

        constraints.Should().Contain(t => t.IsGenericType
            && t.GetGenericTypeDefinition() == typeof(IEquatable<>),
            "TKey must be constrained to IEquatable<TKey>");
    }

    [Fact]
    public void Interface_Inherits_INamedDataSource()
    {
        var type = typeof(IAggregateRepository<,>);
        type.GetInterfaces().Should().Contain(typeof(INamedDataSource));
    }

    [Fact]
    public void Interface_Does_Not_Inherit_ILinqRepository()
    {
        var type = typeof(IAggregateRepository<,>);
        var interfaces = type.GetInterfaces();
        interfaces.Should().NotContain(i => i.Name.Contains("ILinqRepository"));
        interfaces.Should().NotContain(i => i.Name.Contains("IGraphRepository"));
        interfaces.Should().NotContain(i => i.Name.Contains("IReadOnlyRepository"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~IAggregateRepositoryTests" -v minimal`
Expected: FAIL — `IAggregateRepository<,>` type does not exist yet.

- [ ] **Step 3: Create the IAggregateRepository interface**

```csharp
// Src/RCommon.Persistence/Crud/IAggregateRepository.cs
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Entities;

namespace RCommon.Persistence.Crud;

/// <summary>
/// DDD-constrained repository for aggregate roots. Provides only aggregate-appropriate
/// operations: load by ID, find by specification, existence check, add, update, delete,
/// and eager loading. Does not expose IQueryable or collection queries.
/// </summary>
public interface IAggregateRepository<TAggregate, TKey> : INamedDataSource
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TAggregate?> FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    IAggregateRepository<TAggregate, TKey> Include<TProperty>(
        Expression<Func<TAggregate, TProperty>> path);
    IAggregateRepository<TAggregate, TKey> ThenInclude<TPreviousProperty, TProperty>(
        Expression<Func<TPreviousProperty, TProperty>> path);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~IAggregateRepositoryTests" -v minimal`
Expected: All 4 tests PASS.

- [ ] **Step 5: Verify solution builds**

Run: `dotnet build Src/RCommon.sln --no-restore -v minimal`
Expected: Build succeeded, 0 errors.

---

### Task 2: EFCore Aggregate Repository

**Files:**
- Create: `Src/RCommon.EfCore/Crud/EFCoreAggregateRepository.cs`
- Modify: `Src/RCommon.EfCore/EFCorePerisistenceBuilder.cs`

**Context:** Inherits from `GraphRepositoryBase<TAggregate>` (in `Src/RCommon.Persistence/Crud/GraphRepositoryBase.cs`) for infrastructure reuse. The constructor signature matches `EFCoreRepository<TEntity>` exactly: `IDataStoreFactory`, `ILoggerFactory`, `IEntityEventTracker`, `IOptions<DefaultDataStoreOptions>`, `ITenantIdAccessor`. Refer to `Src/RCommon.EfCore/Crud/EFCoreRepository.cs` for all implementation patterns (ObjectSet, ObjectContext, FilteredRepositoryQuery, SaveAsync, Include chains, soft-delete).

- [ ] **Step 1: Create EFCoreAggregateRepository**

```csharp
// Src/RCommon.EfCore/Crud/EFCoreAggregateRepository.cs
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Security.Claims;

namespace RCommon.Persistence.EFCore.Crud;

public class EFCoreAggregateRepository<TAggregate, TKey>
    : GraphRepositoryBase<TAggregate>, IAggregateRepository<TAggregate, TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    private IQueryable<TAggregate>? _repositoryQuery;
    private IIncludableQueryable<TAggregate, object>? _includableQueryable;
    private readonly IDataStoreFactory _dataStoreFactory;

    public EFCoreAggregateRepository(
        IDataStoreFactory dataStoreFactory,
        ILoggerFactory loggerFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
        _dataStoreFactory = dataStoreFactory;
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }

    // -- Implement all abstract members from GraphRepositoryBase/LinqRepositoryBase --
    // These delegate to the EFCore DbContext, following the same patterns as EFCoreRepository.
    // Refer to Src/RCommon.EfCore/Crud/EFCoreRepository.cs for the full implementation of each.
    // Key members to implement:
    //   - ObjectSet (DbSet<TAggregate>), ObjectContext (RCommonDbContext)
    //   - RepositoryQuery, FindQuery overloads, FindCore
    //   - AddAsync, AddRangeAsync, UpdateAsync, DeleteAsync, DeleteManyAsync overloads
    //   - Include (IEagerLoadableQueryable), ThenInclude (IEagerLoadableQueryable)
    //   - Tracking property, SaveAsync
    //   - GetCountAsync, GetTotalCountAsync, AnyAsync, FindAsync(pk), FindSingleOrDefaultAsync

    // -- IAggregateRepository explicit interface implementation --

    async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.GetByIdAsync(
        TKey id, CancellationToken cancellationToken)
    {
        return await FilteredRepositoryQuery
            .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken)
            .ConfigureAwait(false);
    }

    async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.FindAsync(
        ISpecification<TAggregate> specification, CancellationToken cancellationToken)
    {
        return await FilteredRepositoryQuery
            .Where(specification.Predicate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    async Task<bool> IAggregateRepository<TAggregate, TKey>.ExistsAsync(
        TKey id, CancellationToken cancellationToken)
    {
        return await FilteredRepositoryQuery
            .AnyAsync(e => e.Id.Equals(id), cancellationToken)
            .ConfigureAwait(false);
    }

    async Task IAggregateRepository<TAggregate, TKey>.AddAsync(
        TAggregate aggregate, CancellationToken cancellationToken)
    {
        EventTracker.AddEntity(aggregate);
        await ObjectSet.AddAsync(aggregate, cancellationToken).ConfigureAwait(false);
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task IAggregateRepository<TAggregate, TKey>.UpdateAsync(
        TAggregate aggregate, CancellationToken cancellationToken)
    {
        EventTracker.AddEntity(aggregate);
        ObjectSet.Update(aggregate);
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task IAggregateRepository<TAggregate, TKey>.DeleteAsync(
        TAggregate aggregate, CancellationToken cancellationToken)
    {
        EventTracker.AddEntity(aggregate);
        if (SoftDeleteHelper.IsSoftDeletable<TAggregate>())
        {
            SoftDeleteHelper.MarkAsDeleted(aggregate);
            ObjectSet.Update(aggregate);
        }
        else
        {
            ObjectSet.Remove(aggregate);
        }
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.Include<TProperty>(
        Expression<Func<TAggregate, TProperty>> path)
    {
        // Build the include chain, then return this for fluent chaining
        Include(Expression.Lambda<Func<TAggregate, object>>(
            Expression.Convert(path.Body, typeof(object)), path.Parameters));
        return this;
    }

    IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.ThenInclude<TPreviousProperty, TProperty>(
        Expression<Func<TPreviousProperty, TProperty>> path)
    {
        // Delegate to base ThenInclude and return this
        ThenInclude(Expression.Lambda<Func<object, TProperty>>(
            path.Body, Expression.Parameter(typeof(object), path.Parameters[0].Name)));
        return this;
    }

    // Note: The full class must also implement all abstract members inherited from
    // GraphRepositoryBase → LinqRepositoryBase. Copy the implementation patterns
    // from EFCoreRepository.cs (ObjectSet, ObjectContext, RepositoryQuery, FindQuery,
    // FindCore, SaveAsync, Tracking, all Add/Update/Delete/Find overloads, Include/ThenInclude).
}
```

**Implementation note:** The concrete class is large because `GraphRepositoryBase` has ~25 abstract members. Copy the implementation from `EFCoreRepository.cs` for all inherited abstract members. The IAggregateRepository methods above are the *new* explicit interface implementations. The key difference from `EFCoreRepository` is the `IAggregateRoot<TKey>` constraint and the explicit interface implementations.

- [ ] **Step 2: Add DI registration to EFCorePerisistenceBuilder**

In `Src/RCommon.EfCore/EFCorePerisistenceBuilder.cs`, add this line in the constructor after the existing `IGraphRepository<>` registration:

```csharp
services.AddTransient(typeof(IAggregateRepository<,>), typeof(EFCoreAggregateRepository<,>));
```

You'll need to add `using RCommon.Persistence.Crud;` if not already present.

- [ ] **Step 3: Verify solution builds**

Run: `dotnet build Src/RCommon.sln --no-restore -v minimal`
Expected: Build succeeded, 0 errors. If there are errors from missing abstract member implementations, implement them following `EFCoreRepository.cs` patterns.

---

### Task 3: Dapper Aggregate Repository

**Files:**
- Create: `Src/RCommon.Dapper/Crud/DapperAggregateRepository.cs`
- Modify: `Src/RCommon.Dapper/DapperPersistenceBuilder.cs`

**Context:** Inherits from `SqlRepositoryBase<TAggregate>` (in `Src/RCommon.Persistence/Crud/SqlRepositoryBase.cs`). Constructor matches `DapperRepository<TEntity>`: `IDataStoreFactory`, `ILoggerFactory`, `IEntityEventTracker`, `IOptions<DefaultDataStoreOptions>`, `ITenantIdAccessor`. Uses Dommel extension methods for CRUD. Refer to `Src/RCommon.Dapper/Crud/DapperRepository.cs` for all patterns. **Namespace:** `RCommon.Persistence.Dapper.Crud` (matching `DapperRepository`).

- [ ] **Step 1: Create DapperAggregateRepository**

Follow the same structure as EFCoreAggregateRepository but using Dommel patterns from DapperRepository.cs. Use namespace `RCommon.Persistence.Dapper.Crud`:
- `GetByIdAsync` → `db.GetAsync<TAggregate>(id)`
- `FindAsync` → `db.SelectAsync<TAggregate>(spec.Predicate).FirstOrDefault()`
- `ExistsAsync` → `db.GetAsync<TAggregate>(id) != null`
- `AddAsync` → `db.InsertAsync(aggregate)` + `EventTracker.AddEntity(aggregate)`
- `UpdateAsync` → `db.UpdateAsync(aggregate)` + `EventTracker.AddEntity(aggregate)`
- `DeleteAsync` → soft-delete check + `db.DeleteAsync(aggregate)` + `EventTracker.AddEntity(aggregate)`
- `Include/ThenInclude` → no-op, return `this`

All operations use the `await using (var db = DataStore.GetDbConnection())` try-finally pattern from DapperRepository.

- [ ] **Step 2: Add DI registration to DapperPersistenceBuilder**

In `Src/RCommon.Dapper/DapperPersistenceBuilder.cs` constructor, add:

```csharp
services.AddTransient(typeof(IAggregateRepository<,>), typeof(DapperAggregateRepository<,>));
```

- [ ] **Step 3: Verify solution builds**

Run: `dotnet build Src/RCommon.sln --no-restore -v minimal`

---

### Task 4: Linq2Db Aggregate Repository

**Files:**
- Create: `Src/RCommon.Linq2Db/Crud/Linq2DbAggregateRepository.cs`
- Modify: `Src/RCommon.Linq2Db/Linq2DbPersistenceBuilder.cs`

**Context:** Inherits from `LinqRepositoryBase<TAggregate>`. Constructor matches `Linq2DbRepository<TEntity>`. Uses Linq2Db's `DataConnection` and `ITable<T>`. Refer to `Src/RCommon.Linq2Db/Crud/Linq2DbRepository.cs` for all patterns. **Namespace:** `RCommon.Persistence.Linq2Db.Crud` (matching `Linq2DbRepository`).

- [ ] **Step 1: Create Linq2DbAggregateRepository**

Follow patterns from Linq2DbRepository.cs. Use namespace `RCommon.Persistence.Linq2Db.Crud`:
- `GetByIdAsync` → `Table.FirstOrDefaultAsync(e => e.Id.Equals(id))`
- `FindAsync` → `FilteredRepositoryQuery.Where(spec.Predicate).FirstOrDefaultAsync()`
- `ExistsAsync` → `FilteredRepositoryQuery.AnyAsync(e => e.Id.Equals(id))`
- `AddAsync` → `DataConnection.InsertAsync(aggregate)` + `EventTracker.AddEntity(aggregate)`
- `UpdateAsync` → `DataConnection.UpdateAsync(aggregate)` + `EventTracker.AddEntity(aggregate)`
- `DeleteAsync` → soft-delete check + `DataConnection.DeleteAsync(aggregate)` + `EventTracker.AddEntity(aggregate)`
- `Include` → `RepositoryQuery.LoadWith(path)`, return `this`
- `ThenInclude` → `_includableQueryable.ThenLoad(path)`, return `this`

- [ ] **Step 2: Add DI registration to Linq2DbPersistenceBuilder**

In `Src/RCommon.Linq2Db/Linq2DbPersistenceBuilder.cs` constructor, add:

```csharp
services.AddTransient(typeof(IAggregateRepository<,>), typeof(Linq2DbAggregateRepository<,>));
```

- [ ] **Step 3: Verify solution builds**

Run: `dotnet build Src/RCommon.sln --no-restore -v minimal`

---

### Task 5: IUnitOfWork CommitAsync + Event Dispatch

**Files:**
- Modify: `Src/RCommon.Persistence/Transactions/IUnitOfWork.cs`
- Modify: `Src/RCommon.Persistence/Transactions/UnitOfWork.cs`
- Test: `Tests/RCommon.Persistence.Tests/UnitOfWorkCommitAsyncTests.cs`

**Context:** `IUnitOfWork` is at `Src/RCommon.Persistence/Transactions/IUnitOfWork.cs`. `UnitOfWork` is at `Src/RCommon.Persistence/Transactions/UnitOfWork.cs`. The existing `Commit()` calls `TransactionScope.Complete()`. The new `CommitAsync()` also disposes the scope (actual commit) then dispatches events via `IEntityEventTracker.EmitTransactionalEventsAsync()`. `IEntityEventTracker` is in `Src/RCommon.Entities/IEntityEventTracker.cs`. `UnitOfWorkFactory` at `Src/RCommon.Persistence/Transactions/UnitOfWorkFactory.cs` creates instances via `_serviceProvider.GetService<IUnitOfWork>()`.

- [ ] **Step 1: Write CommitAsync tests**

```csharp
// Tests/RCommon.Persistence.Tests/UnitOfWorkCommitAsyncTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class UnitOfWorkCommitAsyncTests
{
    private readonly Mock<ILogger<UnitOfWork>> _mockLogger;
    private readonly Mock<IGuidGenerator> _mockGuidGenerator;
    private readonly Mock<IOptions<UnitOfWorkSettings>> _mockSettings;
    private readonly UnitOfWorkSettings _settings;

    public UnitOfWorkCommitAsyncTests()
    {
        _mockLogger = new Mock<ILogger<UnitOfWork>>();
        _mockGuidGenerator = new Mock<IGuidGenerator>();
        _mockGuidGenerator.Setup(g => g.Create()).Returns(Guid.NewGuid());
        _settings = new UnitOfWorkSettings
        {
            DefaultIsolation = System.Transactions.IsolationLevel.ReadCommitted,
            AutoCompleteScope = false
        };
        _mockSettings = new Mock<IOptions<UnitOfWorkSettings>>();
        _mockSettings.Setup(s => s.Value).Returns(_settings);
    }

    [Fact]
    public async Task CommitAsync_Without_Tracker_Completes_Successfully()
    {
        using var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);

        await uow.CommitAsync();

        uow.State.Should().Be(UnitOfWorkState.Completed);
    }

    [Fact]
    public async Task CommitAsync_With_Tracker_Dispatches_Events()
    {
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker.Setup(t => t.EmitTransactionalEventsAsync()).ReturnsAsync(true);

        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);

        await uow.CommitAsync();

        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_Logs_Warning_When_Dispatch_Returns_False()
    {
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker.Setup(t => t.EmitTransactionalEventsAsync()).ReturnsAsync(false);

        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);

        await uow.CommitAsync();

        // Verify warning was logged (the LogWarning call)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Commit_Obsolete_Still_Works_Without_Dispatch()
    {
        var mockTracker = new Mock<IEntityEventTracker>();

        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);

        #pragma warning disable CS0618 // Obsolete
        uow.Commit();
        #pragma warning restore CS0618

        uow.State.Should().Be(UnitOfWorkState.Completed);
        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(), Times.Never);
    }

    [Fact]
    public async Task CommitAsync_On_Disposed_UoW_Throws_ObjectDisposedException()
    {
        var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);
        uow.Dispose();

        var act = () => uow.CommitAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CommitAsync_On_Already_Completed_UoW_Throws_UnitOfWorkException()
    {
        using var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);
        await uow.CommitAsync(); // first commit

        var act = () => uow.CommitAsync();
        await act.Should().ThrowAsync<UnitOfWorkException>();
    }

    [Fact]
    public async Task CommitAsync_Then_Dispose_Does_Not_Double_Dispose_TransactionScope()
    {
        // CommitAsync disposes TransactionScope internally; Dispose() must not throw
        var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);

        await uow.CommitAsync();

        var act = () => { uow.Dispose(); };
        act.Should().NotThrow("Dispose after CommitAsync must be safe (no double-dispose)");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~UnitOfWorkCommitAsyncTests" -v minimal`
Expected: FAIL — `CommitAsync` method does not exist yet.

- [ ] **Step 3: Add CommitAsync to IUnitOfWork**

In `Src/RCommon.Persistence/Transactions/IUnitOfWork.cs`, add above the existing `Commit()` method:

```csharp
Task CommitAsync(CancellationToken cancellationToken = default);
```

Mark the existing `Commit()` with `[Obsolete("Use CommitAsync instead for automatic domain event dispatch.")]`. Add `using System.Threading;` and `using System.Threading.Tasks;` if not present.

- [ ] **Step 4: Implement CommitAsync in UnitOfWork**

In `Src/RCommon.Persistence/Transactions/UnitOfWork.cs`:

1. Add field: `private readonly IEntityEventTracker? _eventTracker;` and `private bool _transactionScopeDisposed;`
2. Add `IEntityEventTracker? eventTracker = null` as the last parameter to both constructor overloads. Store it: `_eventTracker = eventTracker;`
3. Add the `using RCommon.Entities;` import.
4. Add the `CommitAsync` method (see spec lines 267-298 for exact implementation).
5. Mark existing `Commit()` with `[Obsolete]` attribute.
6. In `Dispose(bool disposing)`, find the `finally` block (existing line 131) where `_transactionScope.Dispose()` is called. Wrap that call with `if (!_transactionScopeDisposed)`. This prevents double-disposal when `CommitAsync` has already disposed the scope — note the `return` at line 116 is inside a `try`, so the `finally` block still executes.

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~UnitOfWorkCommitAsyncTests" -v minimal`
Expected: All 7 tests PASS.

- [ ] **Step 6: Verify solution builds**

Run: `dotnet build Src/RCommon.sln --no-restore -v minimal`

---

### Task 6: UnitOfWorkBehavior Migration

**Files:**
- Modify: `Src/RCommon.Mediatr/Behaviors/UnitOfWorkBehavior.cs`

**Context:** Both `UnitOfWorkRequestBehavior` and `UnitOfWorkRequestWithResponseBehavior` currently call `unitOfWork.Commit()` synchronously inside an async `Handle` method. Change to `await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)`.

- [ ] **Step 1: Update UnitOfWorkRequestBehavior**

In `Src/RCommon.Mediatr/Behaviors/UnitOfWorkBehavior.cs`, in the `UnitOfWorkRequestBehavior<TRequest, TResponse>.Handle` method, replace:

```csharp
unitOfWork.Commit();
```

with:

```csharp
await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
```

- [ ] **Step 2: Update UnitOfWorkRequestWithResponseBehavior**

Same change in the second class `UnitOfWorkRequestWithResponseBehavior<TRequest, TResponse>.Handle`.

- [ ] **Step 3: Verify solution builds**

Run: `dotnet build Src/RCommon.sln --no-restore -v minimal`

- [ ] **Step 4: Run existing MediatR tests**

Run: `dotnet test Tests/RCommon.Mediatr.Tests/ -v minimal`
Expected: All existing tests PASS (backward compatibility preserved).

---

## Chunk 2: Read-Model Repositories

**Prerequisite:** Before Task 8, add a project reference from `RCommon.Persistence` to `RCommon.Models`. In `Src/RCommon.Persistence/RCommon.Persistence.csproj`, add inside the `<ItemGroup>` with other project references:

```xml
<ProjectReference Include="..\RCommon.Models\RCommon.Models.csproj" />
```

Then run `dotnet restore Src/RCommon.sln` to update the dependency graph.

### Task 7: IPagedResult + PagedResult

**Files:**
- Create: `Src/RCommon.Models/IPagedResult.cs`
- Create: `Src/RCommon.Models/PagedResult.cs`
- Test: `Tests/RCommon.Models.Tests/PagedResultTests.cs`

**Context:** These go in `RCommon.Models` (namespace `RCommon.Models`). `PagedResult` uses `Guard.Against` from `RCommon.Core` — check if `RCommon.Models` references `RCommon.Core`. If not, use a simple `if` check with `throw` instead.

- [ ] **Step 1: Write PagedResult tests**

```csharp
// Tests/RCommon.Models.Tests/PagedResultTests.cs
using System;
using System.Collections.Generic;
using FluentAssertions;
using RCommon.Models;
using Xunit;

namespace RCommon.Models.Tests;

public class PagedResultTests
{
    [Fact]
    public void Constructor_Sets_Properties()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = new PagedResult<string>(items, 10, 1, 5);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public void TotalPages_Rounds_Up()
    {
        var result = new PagedResult<string>(new List<string>(), 11, 1, 5);
        result.TotalPages.Should().Be(3); // ceil(11/5) = 3
    }

    [Fact]
    public void TotalPages_Exact_Division()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 1, 5);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void HasNextPage_True_When_Not_Last_Page()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 1, 5);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_False_On_Last_Page()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 2, 5);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_False_On_First_Page()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 1, 5);
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_True_On_Page_2()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 2, 5);
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Throws_When_PageSize_Zero()
    {
        var act = () => new PagedResult<string>(new List<string>(), 10, 1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_Throws_When_PageSize_Negative()
    {
        var act = () => new PagedResult<string>(new List<string>(), 10, 1, -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Empty_Result_Has_Zero_TotalPages()
    {
        var result = new PagedResult<string>(new List<string>(), 0, 1, 10);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Models.Tests/ --filter "FullyQualifiedName~PagedResultTests" -v minimal`
Expected: FAIL — types don't exist yet.

- [ ] **Step 3: Create IPagedResult**

```csharp
// Src/RCommon.Models/IPagedResult.cs
using System.Collections.Generic;

namespace RCommon.Models;

public interface IPagedResult<T>
{
    IReadOnlyList<T> Items { get; }
    long TotalCount { get; }
    int PageNumber { get; }
    int PageSize { get; }
    int TotalPages { get; }
    bool HasNextPage { get; }
    bool HasPreviousPage { get; }
}
```

- [ ] **Step 4: Create PagedResult**

```csharp
// Src/RCommon.Models/PagedResult.cs
using System;
using System.Collections.Generic;

namespace RCommon.Models;

public class PagedResult<T> : IPagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public long TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;

    public PagedResult(IReadOnlyList<T> items, long totalCount, int pageNumber, int pageSize)
    {
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than zero.");
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Models.Tests/ --filter "FullyQualifiedName~PagedResultTests" -v minimal`
Expected: All 10 tests PASS.

---

### Task 8: IReadModel + IReadModelRepository

**Files:**
- Create: `Src/RCommon.Persistence/IReadModel.cs`
- Create: `Src/RCommon.Persistence/Crud/IReadModelRepository.cs`
- Test: `Tests/RCommon.Persistence.Tests/IReadModelRepositoryTests.cs`

- [ ] **Step 1: Write interface constraint tests**

```csharp
// Tests/RCommon.Persistence.Tests/IReadModelRepositoryTests.cs
using System;
using FluentAssertions;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using Xunit;

namespace RCommon.Persistence.Tests;

public class IReadModelRepositoryTests
{
    [Fact]
    public void Interface_Has_IReadModel_Constraint()
    {
        var type = typeof(IReadModelRepository<>);
        var tReadModel = type.GetGenericArguments()[0];
        var constraints = tReadModel.GetGenericParameterConstraints();

        constraints.Should().Contain(typeof(IReadModel));
    }

    [Fact]
    public void Interface_Inherits_INamedDataSource()
    {
        var type = typeof(IReadModelRepository<>);
        type.GetInterfaces().Should().Contain(typeof(INamedDataSource));
    }

    [Fact]
    public void Interface_Has_Class_Constraint()
    {
        var type = typeof(IReadModelRepository<>);
        var tReadModel = type.GetGenericArguments()[0];
        var attrs = tReadModel.GenericParameterAttributes;

        attrs.HasFlag(System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint)
            .Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~IReadModelRepositoryTests" -v minimal`

- [ ] **Step 3: Create IReadModel marker**

```csharp
// Src/RCommon.Persistence/IReadModel.cs
namespace RCommon.Persistence;

/// <summary>
/// Marker interface for read-model/projection types used in CQRS query-side repositories.
/// Read models are optimized for querying and do not participate in domain event tracking.
/// </summary>
public interface IReadModel { }
```

- [ ] **Step 4: Create IReadModelRepository**

```csharp
// Src/RCommon.Persistence/Crud/IReadModelRepository.cs
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using RCommon;
using RCommon.Models;

namespace RCommon.Persistence.Crud;

public interface IReadModelRepository<TReadModel> : INamedDataSource
    where TReadModel : class, IReadModel
{
    Task<TReadModel?> FindAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TReadModel>> FindAllAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<IPagedResult<TReadModel>> GetPagedAsync(
        IPagedSpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    IReadModelRepository<TReadModel> Include<TProperty>(
        Expression<Func<TReadModel, TProperty>> path);
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~IReadModelRepositoryTests" -v minimal`
Expected: All 3 tests PASS.

---

### Task 9: EFCore Read-Model Repository + DI

**Files:**
- Create: `Src/RCommon.EfCore/Crud/EFCoreReadModelRepository.cs`
- Modify: `Src/RCommon.EfCore/EFCorePerisistenceBuilder.cs`

**Context:** Uses **composition** (not inheritance from LinqRepositoryBase) because `IReadModel` does not extend `IBusinessEntity`. Wraps `DbContext` + `DbSet<T>` directly. Resolves data store via `IDataStoreFactory`. **Namespace:** `RCommon.Persistence.EFCore.Crud` (matching `EFCoreRepository`).

- [ ] **Step 1: Create EFCoreReadModelRepository**

The class wraps `RCommonDbContext` and `DbSet<TReadModel>`. It implements `IReadModelRepository<TReadModel>`. It uses `IDataStoreFactory` for data store resolution. Read models typically don't use soft-delete/tenant filters.

Key implementation:
- Constructor: `IDataStoreFactory dataStoreFactory, ILoggerFactory loggerFactory, IOptions<DefaultDataStoreOptions> defaultDataStoreOptions`
- `FindAsync` → `DbSet.Where(spec.Predicate).FirstOrDefaultAsync()`
- `FindAllAsync` → `DbSet.Where(spec.Predicate).ToListAsync()`
- `GetPagedAsync` → query with `Skip`/`Take` + `CountAsync` wrapped in `PagedResult<T>`
- `GetCountAsync` → `DbSet.Where(spec.Predicate).LongCountAsync()`
- `AnyAsync` → `DbSet.Where(spec.Predicate).AnyAsync()`
- `Include` → `DbSet.Include(path)`, return `this`

- [ ] **Step 2: Add DI registration**

In `Src/RCommon.EfCore/EFCorePerisistenceBuilder.cs` constructor, add:

```csharp
services.AddTransient(typeof(IReadModelRepository<>), typeof(EFCoreReadModelRepository<>));
```

- [ ] **Step 3: Verify solution builds**

Run: `dotnet build Src/RCommon.sln -v minimal`

**Note on tests:** Concrete read-model repository implementations require integration tests with real ORM contexts (in-memory DbContext, etc.), which belong in the per-ORM integration test projects. The spec testing strategy (Part 3, items 2-3) calls for `FindAsync`, `FindAllAsync`, `GetPagedAsync`, `GetCountAsync`, and `AnyAsync` tests per ORM. These integration tests should be added to `Tests/RCommon.EfCore.Tests/` when integration test infrastructure is available. For the initial implementation, the interface constraint tests (Task 8) and PagedResult unit tests (Task 7) provide the core coverage.

---

### Task 10: Dapper + Linq2Db Read-Model Repositories + DI

**Files:**
- Create: `Src/RCommon.Dapper/Crud/DapperReadModelRepository.cs`
- Create: `Src/RCommon.Linq2Db/Crud/Linq2DbReadModelRepository.cs`
- Modify: `Src/RCommon.Dapper/DapperPersistenceBuilder.cs`
- Modify: `Src/RCommon.Linq2Db/Linq2DbPersistenceBuilder.cs`

- [ ] **Step 1: Create DapperReadModelRepository**

Uses composition wrapping `IDbConnection` via Dommel. Same query pattern as `DapperRepository` but without event tracking or write operations. **Namespace:** `RCommon.Persistence.Dapper.Crud`.

- [ ] **Step 2: Create Linq2DbReadModelRepository**

Uses composition wrapping `IDataContext.GetTable<T>()`. Same query pattern as `Linq2DbRepository` but without event tracking or write operations. **Namespace:** `RCommon.Persistence.Linq2Db.Crud`.

- [ ] **Step 3: Add DI registrations**

In `DapperPersistenceBuilder` constructor:
```csharp
services.AddTransient(typeof(IReadModelRepository<>), typeof(DapperReadModelRepository<>));
```

In `Linq2DbPersistenceBuilder` constructor:
```csharp
services.AddTransient(typeof(IReadModelRepository<>), typeof(Linq2DbReadModelRepository<>));
```

- [ ] **Step 4: Verify solution builds**

Run: `dotnet build Src/RCommon.sln -v minimal`

**Note on tests:** Same as Task 9 — concrete Dapper/Linq2Db read-model repository tests require integration test infrastructure and should be added to `Tests/RCommon.Dapper.Tests/` and `Tests/RCommon.Linq2Db.Tests/` respectively.

---

## Chunk 3: State Machines + Sagas

### Task 11: State Machine Interfaces

**Files:**
- Create: `Src/RCommon.Core/StateMachines/IStateMachine.cs`
- Create: `Src/RCommon.Core/StateMachines/IStateMachineConfigurator.cs`
- Create: `Src/RCommon.Core/StateMachines/IStateConfigurator.cs`
- Test: `Tests/RCommon.Core.Tests/StateMachineInterfaceTests.cs`

**Context:** These are pure interfaces in `RCommon.Core/StateMachines/` with namespace `RCommon.StateMachines` (RCommon.Core strips `.Core` from namespace via csproj). Constraints are `where TState : struct, Enum` and `where TTrigger : struct, Enum`.

- [ ] **Step 1: Write interface tests**

```csharp
// Tests/RCommon.Core.Tests/StateMachineInterfaceTests.cs
using System;
using System.Linq;
using FluentAssertions;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.Core.Tests;

public class StateMachineInterfaceTests
{
    [Fact]
    public void IStateMachine_Has_Struct_And_Enum_Constraints()
    {
        var type = typeof(IStateMachine<,>);
        var tState = type.GetGenericArguments()[0];
        var tTrigger = type.GetGenericArguments()[1];

        tState.GenericParameterAttributes.HasFlag(
            System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint)
            .Should().BeTrue("TState must be struct");
        tState.GetGenericParameterConstraints().Should().Contain(typeof(Enum));

        tTrigger.GenericParameterAttributes.HasFlag(
            System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint)
            .Should().BeTrue("TTrigger must be struct");
        tTrigger.GetGenericParameterConstraints().Should().Contain(typeof(Enum));
    }

    [Fact]
    public void IStateMachine_Has_Required_Members()
    {
        var type = typeof(IStateMachine<,>);
        type.GetProperty("CurrentState").Should().NotBeNull();
        type.GetProperty("PermittedTriggers").Should().NotBeNull();
        type.GetMethod("CanFire").Should().NotBeNull();
        type.GetMethods().Where(m => m.Name == "FireAsync").Should().HaveCountGreaterOrEqualTo(2,
            "should have FireAsync and FireAsync<TData> overloads");
    }

    [Fact]
    public void IStateMachineConfigurator_Has_ForState_And_Build()
    {
        var type = typeof(IStateMachineConfigurator<,>);
        type.GetMethod("ForState").Should().NotBeNull();
        type.GetMethod("Build").Should().NotBeNull();
    }

    [Fact]
    public void IStateConfigurator_Has_Required_Members()
    {
        var type = typeof(IStateConfigurator<,>);
        type.GetMethod("Permit").Should().NotBeNull();
        type.GetMethod("OnEntry").Should().NotBeNull();
        type.GetMethod("OnExit").Should().NotBeNull();
        type.GetMethod("PermitIf").Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Core.Tests/ --filter "FullyQualifiedName~StateMachineInterfaceTests" -v minimal`

- [ ] **Step 3: Create the three interface files**

Create `IStateMachine.cs`, `IStateMachineConfigurator.cs`, and `IStateConfigurator.cs` in `Src/RCommon.Core/StateMachines/` with the exact definitions from the spec (lines 545-576). Namespace: `RCommon.StateMachines`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Core.Tests/ --filter "FullyQualifiedName~StateMachineInterfaceTests" -v minimal`
Expected: All 4 tests PASS.

---

### Task 12: Saga Infrastructure (SagaState, ISaga, ISagaStore, SagaOrchestrator)

**Files:**
- Create: `Src/RCommon.Persistence/Sagas/SagaState.cs`
- Create: `Src/RCommon.Persistence/Sagas/ISaga.cs`
- Create: `Src/RCommon.Persistence/Sagas/ISagaStore.cs`
- Create: `Src/RCommon.Persistence/Sagas/SagaOrchestrator.cs`
- Test: `Tests/RCommon.Persistence.Tests/SagaOrchestratorTests.cs`

**Context:** All saga types live in `Src/RCommon.Persistence/Sagas/` with namespace `RCommon.Persistence.Sagas`. `SagaOrchestrator` references `IStateMachineConfigurator` and `IStateMachine` from `RCommon.StateMachines` (in RCommon.Core, which RCommon.Persistence already references). `ISaga.HandleAsync` constrains `TEvent` to `ISerializableEvent` from `RCommon.Models`.

- [ ] **Step 1: Write SagaOrchestrator tests**

```csharp
// Tests/RCommon.Persistence.Tests/SagaOrchestratorTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RCommon.Models.Events;
using RCommon.Persistence.Sagas;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.Persistence.Tests;

// Test enums
public enum TestSagaStep { Initial, StepOne, StepTwo, Completed }
public enum TestSagaTrigger { GoToOne, GoToTwo, Complete }

// Test saga state
public class TestSagaData : SagaState<Guid>
{
    public string? Payload { get; set; }
}

// Test event
public record TestSagaEvent(Guid CorrelationId) : ISerializableEvent;

// Concrete test saga
public class TestSaga : SagaOrchestrator<TestSagaData, Guid, TestSagaStep, TestSagaTrigger>
{
    public TestSaga(
        ISagaStore<TestSagaData, Guid> store,
        IStateMachineConfigurator<TestSagaStep, TestSagaTrigger> configurator)
        : base(store, configurator) { }

    protected override TestSagaStep InitialState => TestSagaStep.Initial;

    protected override void ConfigureStateMachine(
        IStateMachineConfigurator<TestSagaStep, TestSagaTrigger> configurator)
    {
        configurator.ForState(TestSagaStep.Initial)
            .Permit(TestSagaTrigger.GoToOne, TestSagaStep.StepOne);
        configurator.ForState(TestSagaStep.StepOne)
            .Permit(TestSagaTrigger.GoToTwo, TestSagaStep.StepTwo);
    }

    protected override TestSagaTrigger MapEventToTrigger<TEvent>(TEvent @event)
    {
        return TestSagaTrigger.GoToOne;
    }

    public override Task CompensateAsync(TestSagaData state, CancellationToken ct)
    {
        state.IsFaulted = true;
        state.FaultReason = "Compensated";
        return Task.CompletedTask;
    }
}

public class SagaOrchestratorTests
{
    [Fact]
    public void SagaState_Has_Required_Properties()
    {
        var state = new TestSagaData
        {
            Id = Guid.NewGuid(),
            CorrelationId = "order-123",
            StartedAt = DateTimeOffset.UtcNow,
            CurrentStep = "Initial",
            Version = 1
        };

        state.Id.Should().NotBeEmpty();
        state.CorrelationId.Should().Be("order-123");
        state.IsCompleted.Should().BeFalse();
        state.IsFaulted.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_With_Null_CurrentStep_Uses_InitialState()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(It.IsAny<TestSagaStep>()))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(It.IsAny<TestSagaTrigger>())).Returns(true);
        mockMachine.Setup(m => m.CurrentState).Returns(TestSagaStep.StepOne);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = null! };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state, CancellationToken.None);

        // Should have built with InitialState since CurrentStep was null
        mockConfigurator.Verify(c => c.Build(TestSagaStep.Initial), Times.AtLeastOnce);
        state.CurrentStep.Should().Be("StepOne");
        mockStore.Verify(s => s.SaveAsync(state, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Invalid_Trigger_Is_Ignored()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(It.IsAny<TestSagaStep>()))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(It.IsAny<TestSagaTrigger>())).Returns(false);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state, CancellationToken.None);

        // CanFire returned false, so FireAsync and SaveAsync should NOT be called
        mockMachine.Verify(m => m.FireAsync(It.IsAny<TestSagaTrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        mockStore.Verify(s => s.SaveAsync(It.IsAny<TestSagaData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_With_Known_State_Transitions_Correctly()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(TestSagaStep.Initial))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(TestSagaTrigger.GoToOne)).Returns(true);
        mockMachine.Setup(m => m.CurrentState).Returns(TestSagaStep.StepOne);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state, CancellationToken.None);

        // Build should be called with the current state (Initial), not just from EnsureConfigured
        mockConfigurator.Verify(c => c.Build(TestSagaStep.Initial), Times.AtLeastOnce);
        state.CurrentStep.Should().Be("StepOne");
        mockStore.Verify(s => s.SaveAsync(state, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Called_Twice_Configures_StateMachine_Once()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockStateConfig = new Mock<IStateConfigurator<TestSagaStep, TestSagaTrigger>>();
        var mockMachine = new Mock<IStateMachine<TestSagaStep, TestSagaTrigger>>();

        mockConfigurator.Setup(c => c.ForState(It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockStateConfig.Setup(s => s.Permit(It.IsAny<TestSagaTrigger>(), It.IsAny<TestSagaStep>()))
            .Returns(mockStateConfig.Object);
        mockConfigurator.Setup(c => c.Build(It.IsAny<TestSagaStep>()))
            .Returns(mockMachine.Object);
        mockMachine.Setup(m => m.CanFire(It.IsAny<TestSagaTrigger>())).Returns(true);
        mockMachine.Setup(m => m.CurrentState).Returns(TestSagaStep.StepOne);

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state1 = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };
        var state2 = new TestSagaData { Id = Guid.NewGuid(), CurrentStep = "Initial" };

        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state1, CancellationToken.None);
        await saga.HandleAsync(new TestSagaEvent(Guid.NewGuid()), state2, CancellationToken.None);

        // ConfigureStateMachine calls ForState — should only happen once (lazy init)
        // The TestSaga configures 2 states (Initial, StepOne), so ForState is called exactly 2 times total
        mockConfigurator.Verify(c => c.ForState(It.IsAny<TestSagaStep>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CompensateAsync_Sets_Fault_State()
    {
        var mockStore = new Mock<ISagaStore<TestSagaData, Guid>>();
        var mockConfigurator = new Mock<IStateMachineConfigurator<TestSagaStep, TestSagaTrigger>>();

        var saga = new TestSaga(mockStore.Object, mockConfigurator.Object);
        var state = new TestSagaData { Id = Guid.NewGuid() };

        await saga.CompensateAsync(state, CancellationToken.None);

        state.IsFaulted.Should().BeTrue();
        state.FaultReason.Should().Be("Compensated");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~SagaOrchestratorTests" -v minimal`

- [ ] **Step 3: Create SagaState.cs**

```csharp
// Src/RCommon.Persistence/Sagas/SagaState.cs
using System;

namespace RCommon.Persistence.Sagas;

public abstract class SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string CurrentStep { get; set; } = default!;
    public bool IsCompleted { get; set; }
    public bool IsFaulted { get; set; }
    public string? FaultReason { get; set; }
    public int Version { get; set; }
}
```

- [ ] **Step 4: Create ISaga.cs**

```csharp
// Src/RCommon.Persistence/Sagas/ISaga.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Models.Events;

namespace RCommon.Persistence.Sagas;

public interface ISaga<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    Task HandleAsync<TEvent>(TEvent @event, TState state, CancellationToken ct = default)
        where TEvent : ISerializableEvent;
    Task CompensateAsync(TState state, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create ISagaStore.cs**

```csharp
// Src/RCommon.Persistence/Sagas/ISagaStore.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Sagas;

public interface ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
    Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task SaveAsync(TState state, CancellationToken ct = default);
    Task DeleteAsync(TState state, CancellationToken ct = default);
}
```

- [ ] **Step 6: Create SagaOrchestrator.cs**

Use the exact implementation from the spec (lines 632-710). Namespace: `RCommon.Persistence.Sagas`. References `RCommon.StateMachines` for `IStateMachineConfigurator` and `IStateMachine`.

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~SagaOrchestratorTests" -v minimal`
Expected: All 7 tests PASS.

---

### Task 13: InMemorySagaStore

**Files:**
- Create: `Src/RCommon.Persistence/Sagas/InMemorySagaStore.cs`
- Test: `Tests/RCommon.Persistence.Tests/InMemorySagaStoreTests.cs`

- [ ] **Step 1: Write InMemorySagaStore tests**

```csharp
// Tests/RCommon.Persistence.Tests/InMemorySagaStoreTests.cs
using System;
using System.Threading.Tasks;
using FluentAssertions;
using RCommon.Persistence.Sagas;
using Xunit;

namespace RCommon.Persistence.Tests;

public class TestSagaState : SagaState<Guid>
{
    public string? Data { get; set; }
}

public class InMemorySagaStoreTests
{
    [Fact]
    public async Task SaveAsync_And_GetByIdAsync_RoundTrips()
    {
        var store = new InMemorySagaStore<TestSagaState, Guid>();
        var state = new TestSagaState { Id = Guid.NewGuid(), CorrelationId = "c1", Data = "test" };

        await store.SaveAsync(state);

        var loaded = await store.GetByIdAsync(state.Id);
        loaded.Should().BeSameAs(state);
    }

    [Fact]
    public async Task FindByCorrelationIdAsync_Returns_Matching_State()
    {
        var store = new InMemorySagaStore<TestSagaState, Guid>();
        var state = new TestSagaState { Id = Guid.NewGuid(), CorrelationId = "order-456" };

        await store.SaveAsync(state);

        var found = await store.FindByCorrelationIdAsync("order-456");
        found.Should().BeSameAs(state);
    }

    [Fact]
    public async Task FindByCorrelationIdAsync_Returns_Null_When_Not_Found()
    {
        var store = new InMemorySagaStore<TestSagaState, Guid>();

        var found = await store.FindByCorrelationIdAsync("nonexistent");
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Removes_State()
    {
        var store = new InMemorySagaStore<TestSagaState, Guid>();
        var state = new TestSagaState { Id = Guid.NewGuid(), CorrelationId = "c1" };

        await store.SaveAsync(state);
        await store.DeleteAsync(state);

        var loaded = await store.GetByIdAsync(state.Id);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_Updates_Existing_State()
    {
        var store = new InMemorySagaStore<TestSagaState, Guid>();
        var state = new TestSagaState { Id = Guid.NewGuid(), CorrelationId = "c1", Data = "v1" };

        await store.SaveAsync(state);
        state.Data = "v2";
        await store.SaveAsync(state);

        var loaded = await store.GetByIdAsync(state.Id);
        loaded!.Data.Should().Be("v2");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~InMemorySagaStoreTests" -v minimal`

- [ ] **Step 3: Create InMemorySagaStore**

```csharp
// Src/RCommon.Persistence/Sagas/InMemorySagaStore.cs
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Sagas;

public class InMemorySagaStore<TState, TKey> : ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, TState> _store = new();

    public Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var state);
        return Task.FromResult(state);
    }

    public Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        var state = _store.Values.FirstOrDefault(s => s.CorrelationId == correlationId);
        return Task.FromResult(state);
    }

    public Task SaveAsync(TState state, CancellationToken ct = default)
    {
        _store.AddOrUpdate(state.Id, state, (_, _) => state);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TState state, CancellationToken ct = default)
    {
        _store.TryRemove(state.Id, out _);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~InMemorySagaStoreTests" -v minimal`
Expected: All 5 tests PASS.

**Note:** `InMemorySagaStore` does NOT implement optimistic concurrency checking (version-based). The `ConcurrentDictionary.AddOrUpdate` always succeeds regardless of `Version`. Optimistic concurrency is the responsibility of ORM saga stores (EFCore uses EF concurrency tokens, Dapper/Linq2Db use explicit version checks). The in-memory store is intended for development/testing only.

---

### Task 14: ORM Saga Stores + DI Registration

**Files:**
- Create: `Src/RCommon.EfCore/Sagas/EFCoreSagaStore.cs`
- Create: `Src/RCommon.Dapper/Sagas/DapperSagaStore.cs`
- Create: `Src/RCommon.Linq2Db/Sagas/Linq2DbSagaStore.cs`
- Modify: `Src/RCommon.EfCore/EFCorePerisistenceBuilder.cs`
- Modify: `Src/RCommon.Dapper/DapperPersistenceBuilder.cs`
- Modify: `Src/RCommon.Linq2Db/Linq2DbPersistenceBuilder.cs`

**Context:** Each ORM saga store implements `ISagaStore<TState, TKey>` using its ORM's data access patterns. EFCore uses `DbContext.Set<TState>()`, Dapper uses Dommel extensions, Linq2Db uses `DataConnection.GetTable<TState>()`. Register as Scoped. **Namespaces:** `RCommon.Persistence.EFCore.Sagas`, `RCommon.Persistence.Dapper.Sagas`, `RCommon.Persistence.Linq2Db.Sagas` (following the existing ORM namespace conventions).

**Note:** The default `InMemorySagaStore` registration should be added in the core persistence DI configuration so that saga stores work without an ORM. If there is a `DefaultPersistenceBuilder` or similar, add: `services.AddScoped(typeof(ISagaStore<,>), typeof(InMemorySagaStore<,>));`. The ORM builders then override this default with their specific implementations.

- [ ] **Step 1: Create EFCoreSagaStore**

Uses `IDataStoreFactory` to resolve `RCommonDbContext`. Implements `ISagaStore` via `DbContext.Set<TState>()` queries. `FindByCorrelationIdAsync` uses `FirstOrDefaultAsync(s => s.CorrelationId == correlationId)`. `SaveAsync` uses `AddAsync` or `Update` based on whether entity is tracked. `GetByIdAsync` uses `FindAsync(id)`. `DeleteAsync` uses `Remove(state)` + `SaveChangesAsync()`.

- [ ] **Step 2: Create DapperSagaStore**

Uses Dommel extensions. `GetByIdAsync` → `db.GetAsync<TState>(id)`. `FindByCorrelationIdAsync` → `db.SelectAsync<TState>(s => s.CorrelationId == correlationId).FirstOrDefault()`. `SaveAsync` → `db.UpdateAsync(state)` (or `InsertAsync` for new). `DeleteAsync` → `db.DeleteAsync(state)`.

- [ ] **Step 3: Create Linq2DbSagaStore**

Uses Linq2Db `DataConnection`. `GetByIdAsync` → `table.FirstOrDefaultAsync(s => s.Id.Equals(id))`. `FindByCorrelationIdAsync` → `table.FirstOrDefaultAsync(s => s.CorrelationId == correlationId)`. `SaveAsync` → `InsertOrReplaceAsync`. `DeleteAsync` → `DeleteAsync`.

- [ ] **Step 4: Add DI registrations to all three builders**

In each builder constructor, add (after the `InMemorySagaStore` default registration if applicable):

```csharp
// EFCorePerisistenceBuilder
services.AddScoped(typeof(ISagaStore<,>), typeof(EFCoreSagaStore<,>));

// DapperPersistenceBuilder
services.AddScoped(typeof(ISagaStore<,>), typeof(DapperSagaStore<,>));

// Linq2DbPersistenceBuilder
services.AddScoped(typeof(ISagaStore<,>), typeof(Linq2DbSagaStore<,>));
```

- [ ] **Step 5: Verify solution builds**

Run: `dotnet build Src/RCommon.sln -v minimal`
Expected: Build succeeded, 0 errors.

**Note on tests:** ORM saga store implementations require integration tests with real ORM contexts. These should be added to `Tests/RCommon.EfCore.Tests/`, `Tests/RCommon.Dapper.Tests/`, and `Tests/RCommon.Linq2Db.Tests/` respectively when integration test infrastructure is available. The spec testing strategy (Part 4, item 3) calls for `FindByCorrelationIdAsync`, `SaveAsync`, and concurrent-save-with-stale-version tests per ORM.

---

### Task 15: Final Verification

- [ ] **Step 1: Full solution build**

Run: `dotnet build Src/RCommon.sln -v minimal`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Run all tests**

Run: `dotnet test Src/RCommon.sln -v minimal`
Expected: All tests pass, including new tests and all existing tests (backward compatibility).

- [ ] **Step 3: Run new tests only**

```bash
dotnet test Tests/RCommon.Persistence.Tests/ --filter "FullyQualifiedName~IAggregateRepositoryTests|FullyQualifiedName~UnitOfWorkCommitAsyncTests|FullyQualifiedName~IReadModelRepositoryTests|FullyQualifiedName~SagaOrchestratorTests|FullyQualifiedName~InMemorySagaStoreTests" -v normal
dotnet test Tests/RCommon.Models.Tests/ --filter "FullyQualifiedName~PagedResultTests" -v normal
dotnet test Tests/RCommon.Core.Tests/ --filter "FullyQualifiedName~StateMachineInterfaceTests" -v normal
```

Expected: All new tests pass.
