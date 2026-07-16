# Native CQRS: Opt-In Transaction Wrapping for ICommandBus

**Branch:** bugfix/consumer-feedback-hardening
**Date:** 2026-07-15
**Status:** Approved
**Breaking Change:** No

## Overview

RCommon.Mediatr's pipeline offers an opt-in `AddUnitOfWorkToRequestPipeline()` that automatically wraps every MediatR request in a transaction via an `IPipelineBehavior<,>`. The native, non-MediatR `ICommandBus`/`ICommandHandler<TResult,TCommand>` path has no equivalent: `CommandBus.DispatchCommandAsync`/`ExecuteHandlerAsync` (`Src/RCommon.ApplicationServices/Commands/CommandBus.cs:79-149`) never references `IUnitOfWork`, `IUnitOfWorkFactory`, or `CommitAsync` — a multi-step native command handler must manage its own unit of work explicitly, and nothing in the docs cross-references the MediatR page to say so. This is a real asymmetry between RCommon's two CQRS front-ends, not a missing capability — `IUnitOfWorkFactory`/`IUnitOfWork` (`Src/RCommon.Persistence/Transactions/IUnitOfWork.cs`) is fully general and already injectable by hand into any handler; it just isn't automatically wired for the native bus the way it is for MediatR.

This spec also disposes of a second, unrelated, purely cosmetic item found in the same review: `ICommandHandler<TResult, TCommand>`'s generic parameter order is result-first, unlike MediatR's request-first `IRequestHandler<TRequest,TResponse>`. See "Related, Deferred" below — it is not addressed by this release.

## Personas

- **Library consumer using the native `ICommandBus`, not MediatR** — Writes multi-step command handlers (e.g., create an order and reserve inventory) and wants the same "one call, one transaction" guarantee MediatR consumers get, without hand-rolling `IUnitOfWorkFactory` boilerplate in every handler.
- **Library consumer reading only the MediatR docs page** — Currently at risk of assuming the same automatic-transaction guarantee applies to the native bus, and shipping a non-transactional multi-step command without realizing it. This spec closes that gap with both code and an explicit doc cross-reference.

## Core Requirements

### Must Have

- A new opt-in extension, `AddUnitOfWorkToCommandBus()` on `ICqrsBuilder`, mirroring `AddUnitOfWorkToRequestPipeline()`'s name and opt-in nature. When called, every `ICommandBus.DispatchCommandAsync<TResult>` call is wrapped in an `IUnitOfWork` created via `IUnitOfWorkFactory.Create(TransactionMode.Default)`, committed via `CommitAsync` after the inner dispatch completes successfully.
- Implemented as a bus-level decorator (`UnitOfWorkCommandBus : ICommandBus`), not a per-handler pipeline — the native dispatcher (`CommandBus`) has a single public entry point and no `IPipelineBehavior<,>`-style chaining concept, so decorating the bus itself is the natural fit for this dispatch shape, whereas MediatR's own pipeline-behavior mechanism was the natural fit there. Both achieve the same outcome (automatic commit after a successful dispatch) through the idiom appropriate to each dispatcher's actual architecture.
- Scoped to `ICommandBus` only, not `IQueryBus` — queries are read-only by CQRS convention and don't need a unit of work. This is a deliberate, documented difference from the MediatR behaviors, which wrap both commands and queries indiscriminately (MediatR has no native command/query distinction at the pipeline-behavior level); native RCommon does distinguish the two buses, so the native fix takes advantage of that separation rather than blindly mirroring MediatR's scope.
- `website/docs/cqrs-mediator/command-query-bus.mdx` (native bus docs) gets a new section describing `AddUnitOfWorkToCommandBus()`, and explicitly cross-references the MediatR page's `AddUnitOfWorkToRequestPipeline()` so a reader on either page understands both exist and neither implies the other applies automatically.

### Must Not Do

- Must not change `ICommandBus`, `ICommandHandler<TResult,TCommand>`, or `CommandBus`'s existing public members. The decorator wraps `ICommandBus` from the outside; `CommandBus` itself is untouched.
- Must not make transaction wrapping the default for the native bus. It stays opt-in via `AddUnitOfWorkToCommandBus()`, exactly matching the MediatR feature's own opt-in posture (`AddUnitOfWorkToRequestPipeline()` is not called automatically by `WithMediatR<T>()` either).
- Must not touch `ICommandHandler<TResult, TCommand>`'s generic parameter order in this release (see Related, Deferred).

## Technical Constraints

- Decoration is manual (`ServiceCollectionDescriptorExtensions.Replace`), not via a third-party decoration library — consistent with the rest of RCommon's DI registration style.
- No new package dependencies.

## Resilience

If the wrapped handler throws, the `using`-scoped `IUnitOfWork` is disposed without `CommitAsync` ever being called, so the transaction rolls back — same behavior as any other `IUnitOfWork` consumer today, not a new failure mode introduced by this decorator.

## Observability

Not separately logged by the decorator. `CommandBus` already logs at `Trace`/`Debug` for dispatch; `UnitOfWork.CommitAsync` already logs via its own existing instrumentation. No new logging surface needed.

## Security

Not applicable — no new trust boundary; this only changes whether a transaction is opened around an already-authorized dispatch call.

## Performance & Scalability

One additional `IUnitOfWork` (a `TransactionScope`, per the existing `UnitOfWork` implementation) per command dispatch when opted in — identical cost profile to what MediatR consumers already pay today via `AddUnitOfWorkToRequestPipeline()`. Opt-in, so zero cost for consumers who don't call it.

## Design Detail

### `UnitOfWorkCommandBus`

**Location:** `Src/RCommon.ApplicationServices/Commands/UnitOfWorkCommandBus.cs`

```csharp
public class UnitOfWorkCommandBus : ICommandBus
{
    private readonly ICommandBus _inner;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public UnitOfWorkCommandBus(ICommandBus inner, IUnitOfWorkFactory unitOfWorkFactory)
    {
        _inner = inner;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<TResult> DispatchCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        where TResult : IExecutionResult
    {
        using (var unitOfWork = _unitOfWorkFactory.Create(TransactionMode.Default))
        {
            var result = await _inner.DispatchCommandAsync(command, cancellationToken).ConfigureAwait(false);
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
```

### Registration

**Location:** `Src/RCommon.ApplicationServices/CqrsBuilderExtensions.cs` (new method)

```csharp
/// <summary>
/// Wraps every ICommandBus.DispatchCommandAsync call in an IUnitOfWork, committed automatically
/// after a successful dispatch -- the native-bus equivalent of RCommon.Mediatr's
/// AddUnitOfWorkToRequestPipeline(). Scoped to commands only; IQueryBus is untouched.
/// </summary>
public static void AddUnitOfWorkToCommandBus(this ICqrsBuilder builder)
{
    builder.Services.AddTransient<CommandBus>();
    builder.Services.Replace(ServiceDescriptor.Transient<ICommandBus>(sp =>
        new UnitOfWorkCommandBus(
            sp.GetRequiredService<CommandBus>(),
            sp.GetRequiredService<IUnitOfWorkFactory>())));
}
```

Usage:

```csharp
services.AddRCommon()
    .WithCQRS<CqrsBuilder>(cqrs =>
    {
        cqrs.AddCommandHandlers(typeof(CreateOrderCommand).Assembly);
        cqrs.AddUnitOfWorkToCommandBus();
    })
    .WithUnitOfWork<UnitOfWorkBuilder>(uow => { /* ... */ });
```

## Related, Deferred: `ICommandHandler<TResult, TCommand>` Generic Parameter Order

`ICommandHandler<TResult, TCommand>` (`Src/RCommon.ApplicationServices/Commands/ICommandHandler.cs:21-47`) puts the result type first, command type second — the reverse of MediatR's `IRequestHandler<TRequest,TResponse>` (request-first). This is purely a naming/ordering observation with no functional defect: `CommandBus.BuildCommandDetails` (`CommandBus.cs:204-205`) constructs the closed generic type in the same order the interface declares, so dispatch is internally consistent.

**Decision: leave unchanged in this release.** Reordering the generic parameters would require every existing command handler implementation, across every consumer, to swap its type arguments — a hard breaking change with no functional upside, undertaken purely for muscle-memory ergonomics against a different library's convention. Not worth it on its own. If a 4.x breaking-change window opens for unrelated reasons, revisit then; do not bundle a breaking change into this patch release to fix a cosmetic-only inconsistency.

## Testing Strategy

1. `AddUnitOfWorkToCommandBus()` causes a successful `DispatchCommandAsync` call to invoke `CommitAsync` on the created `IUnitOfWork` exactly once.
2. A handler that throws causes the `IUnitOfWork` to be disposed without `CommitAsync` being called (rollback path).
3. Without calling `AddUnitOfWorkToCommandBus()`, `ICommandBus` resolves to the plain `CommandBus` with no wrapping (regression guard — opt-in stays opt-in).
4. `IQueryBus` is unaffected by `AddUnitOfWorkToCommandBus()` — no `IUnitOfWork` is created for query dispatch.
5. Doc cross-reference: no automated test; verified by review that both `command-query-bus.mdx` and the MediatR page link to each other's transaction-wrapping section.

## File Summary

| File | Action | Location |
|------|--------|----------|
| `UnitOfWorkCommandBus.cs` | Create | `Src/RCommon.ApplicationServices/Commands/` |
| `CqrsBuilderExtensions.cs` | Modify — add `AddUnitOfWorkToCommandBus()` | `Src/RCommon.ApplicationServices/` |
| `command-query-bus.mdx` | Modify — document new method, cross-reference MediatR page | `website/docs/cqrs-mediator/` |
| `mediatr.mdx` | Modify — cross-reference back | `website/docs/cqrs-mediator/` |
| `README.md` | Modify | `Src/RCommon.ApplicationServices/` |
| Test files (per Testing Strategy above) | Create | `Tests/RCommon.ApplicationServices.Tests/` |
