# Spike Findings: Broker-Outbox Coordination (Recipe 2b)

- **Date:** 2026-07-22
- **Purpose:** Determine whether RCommon can wrap a broker's NATIVE transactional outbox such that a broker `Publish` issued INSIDE RCommon's `UnitOfWork` `System.Transactions.TransactionScope` stages atomically — business state + the broker's outbox row commit together — and a UoW rollback leaves neither.
- **Recipe-2b question (spec AC-15):** Can the broker's native outbox seam enlist in RCommon's ambient `TransactionScope`, making broker outbox rows part of the same atomic transaction as the business entity write?
- **Harness:** Podman-hosted Postgres via Testcontainers. Both spikes assert honestly (`assert-actual` + `// SPIKE FINDING:` comments) and are GREEN in CI. Test files: `MassTransitOutboxCoordinationSpikeTests.cs`, `WolverineOutboxCoordinationSpikeTests.cs`.

---

## Summary Table

| Broker        | Version   | Atomic commit? | Rollback clean? | Recipe 2b verdict |
|---------------|-----------|----------------|-----------------|-------------------|
| MassTransit   | 8.5.9     | YES            | YES             | VIABLE — use recipe 2b |
| WolverineFx   | 5.39.1    | NO             | N/A (mechanism) | NO-GO — use recipe 2a |

---

## MassTransit 8.5.9 — PASS (recipe 2b viable)

### Wiring

```csharp
services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<SpikeDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox(); // Publish stages to OutboxMessage, not the broker
    });
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});
```

`SpikeDbContext` maps MassTransit's outbox entities via `modelBuilder.AddTransactionalOutboxEntities()`. The bus is deliberately **not started** (no `BusOutboxDeliveryService` sweeper) so the staged `OutboxMessage` row is stable for assertion.

### Observed counts

| Scenario | Business rows (widgets) | MT `OutboxMessage` rows |
|---|---|---|
| Committed UoW | 1 | 1 |
| Rolled-back UoW | 0 | 0 |

### Mechanism

With `UseBusOutbox()`, the scoped `IPublishEndpoint` does not send to the broker; instead it stages the published message and, via a `SavingChanges` interceptor registered on the **same scoped** `SpikeDbContext`, writes an `OutboxMessage` row during that `DbContext`'s `SaveChangesAsync`. Because that `SaveChangesAsync` executes inside RCommon's ambient `TransactionScope` (created with `TransactionScopeOption.Required` + `TransactionScopeAsyncFlowOption.Enabled`), Npgsql's connection auto-enlists in the ambient `System.Transactions` transaction. The business `INSERT` and the outbox `INSERT` therefore share one transactional unit. The `BusOutboxDeliveryService` sweeper is never started, so staging is proven independent of delivery and there is no race that could delete the row before the assertion reads it.

**Caveat:** atomicity relies on the business write and the `Publish` sharing the **same scoped `DbContext`** whose `SaveChangesAsync` runs inside the ambient `TransactionScope`. This is exactly how RCommon's EF repositories operate, making the seam reliable in the expected usage pattern.

### Verdict

Recipe 2b is **VIABLE** for MassTransit. RCommon will wrap `AddEntityFrameworkOutbox` + `UseBusOutbox()` as `UseBrokerOutbox(o => o.OnDataStore("..."))`.

---

## WolverineFx 5.39.1 — NO-GO (fall back to recipe 2a)

### Wiring attempted

```csharp
opts.PersistMessagesWithPostgresql(connectionString);     // WolverineFx.Postgresql 5.39.1
opts.Durability.Mode = DurabilityMode.Serverless;        // no background delivery agent
opts.UseEntityFrameworkCoreTransactions();
opts.PublishMessage<SpikeIntegrationEvent>()
    .ToLocalQueue("spike")
    .UseDurableInbox();
```

`SpikeDbContext` maps Wolverine's envelope storage via `modelBuilder.MapWolverineEnvelopeStorage()`. Schema provisioned via `AddResourceSetupOnStartup()` (`JasperFx.Resources`). Envelope table: `"wolverine"."wolverine_outgoing_envelopes"`. A Control test (identical persist path, no RCommon UoW) validated the harness.

### Two independent reasons for NO-GO

**Reason 1 — No persist-without-flush seam on the public API.**

WolverineFx 5.39.1's `IDbContextOutbox` exposes only:
- `SaveChangesAndFlushMessagesAsync()` — persist + immediate flush/deliver (inline delivery drains the outgoing row)
- `FlushOutgoingMessagesAsync()`

A bare `ctx.SaveChangesAsync()` writes **no** envelope row. The MassTransit "stage durably now, deliver later via a sweeper the test never starts" pattern is simply not available on this API. There is no durable-staging-without-flush seam.

**Reason 2 — The envelope write uses Wolverine's own transaction, not the ambient scope.**

Decompiling `Wolverine.EntityFrameworkCore.Internals.EfCoreEnvelopeTransaction` (WolverineFx 5.39.1):

```csharp
// PersistOutgoingAsync (decompiled)
if (DbContext.Database.CurrentTransaction == null)
    await DbContext.Database.BeginTransactionAsync();

// ... write envelope rows ...

// CommitAsync (decompiled)
await DbContext.Database.CurrentTransaction.CommitAsync();
```

Opening an explicit `DbContext.Database.BeginTransactionAsync()` **suppresses** EF Core's ambient `System.Transactions` auto-enlistment. Wolverine then commits **that** `DbTransaction` itself. The envelope write therefore commits and rolls back on Wolverine's own `DbTransaction`, independently of RCommon's ambient `TransactionScope`.

### Observed counts

The `wolverine_outgoing_envelopes` table reads `0` in all scenarios, because `SaveChangesAndFlushMessagesAsync` always flushes inline — the row is delivered and drained before the assertion. The envelope table is therefore not a useful positive observation point. The reliable signal is the business-row (widgets) count:

| Scenario | Business rows (widgets) | Wolverine outgoing envelopes |
|---|---|---|
| Control (no UoW) | 1 | 0 (flush drains inline) |
| Committed RCommon UoW | 1 | 0 |
| Rolled-back RCommon UoW | 0 | 0 |

**Authoritative evidence:** the decompiled `PersistOutgoingAsync`/`CommitAsync` mechanism above, corroborated by the Control test showing the same observed counts — which proves the outgoing table is not a reliable positive signal (it is always 0 because the flush path drains it), so the go/no-go rests on the mechanism, not on row counts.

### Consequence

Wolverine cannot serve as a "publish inside RCommon's ambient `TransactionScope` → atomic durable staging" wrapper. For Wolverine, use **recipe 2a** (broker as a producer sitting behind RCommon's OWN transactional outbox). RCommon's outbox row is written by RCommon's own EF store inside the UoW scope, and a separate processor later publishes via Wolverine — no coupling between Wolverine's internal transaction and the UoW scope.

### Verdict

Recipe 2b is **NO-GO** for WolverineFx. Fall back to **recipe 2a**.

---

## Go/No-Go Conclusion

| Broker | Go/No-Go | Decision |
|---|---|---|
| MassTransit 8.5.9 | **GO** | Recipe 2b viable; RCommon will wrap `AddEntityFrameworkOutbox` + `UseBusOutbox()` as `UseBrokerOutbox(...)` |
| WolverineFx 5.39.1 | **NO-GO** | Recipe 2b not viable; use recipe 2a (RCommon outbox → Wolverine relay) |

This result **gates Phase 4** (MassTransit/Wolverine wrappers + recipes 2a/2b): MassTransit recipe 2b proceeds as designed; Wolverine recipe 2b is dropped and only recipe 2a is implemented for Wolverine.

---

## Caveats / Follow-ups

1. **MassTransit atomicity relies on a shared scoped `DbContext`.** The seam works because the business write and the `Publish` call share the same scoped `DbContext` whose `SaveChangesAsync` runs inside the ambient `TransactionScope`. If the broker publish were to use a different `DbContext` instance (e.g. a factory-created one), the auto-enlistment guarantee would not apply. RCommon's EF repository pattern uses the same scoped `DbContext`, so the seam is reliable in normal usage — but this dependency must be documented and the wrapper must enforce it.

2. **Intermittent Podman named-pipe container-start timeouts.** On the development machine, occasional Testcontainers container-start timeouts were observed (Podman named-pipe socket latency). CI may need a container-start retry policy or increased timeout. The Testcontainers ↔ Podman socket wiring should be validated on CI runners early (see §6 testing notes in the design spec).
