# RCommon

Open-source .NET infrastructure library providing battle-tested abstractions for persistence, CQRS, event handling, messaging, caching, and more. Swap providers (EF Core, Dapper, MediatR, MassTransit, Wolverine, Redis) without touching your domain code.

[![NuGet](https://img.shields.io/nuget/v/RCommon.Core?label=NuGet)](https://www.nuget.org/profiles/RCommon)
[![License](https://img.shields.io/github/license/RCommon-Team/RCommon)](LICENSE)

## Overview

RCommon is a cohesive set of libraries with abstractions for widely used design patterns and architectural patterns which are common to many .NET applications. The primary goals are:

1. **Future-proof** applications against changing infrastructure needs — swap providers without rewriting domain code.
2. **Solve common problems** below the presentation layer: persistence, transaction management, event handling, messaging, caching, and cross-cutting concerns.
3. **Code testability** — straightforward dependency injection, minimal magic, high test coverage.
4. **Open source** — Apache 2.0 licensed, forever.

**Targets:** .NET 8, .NET 9, .NET 10

We track bugs, enhancement requests, and feature requests on [GitHub Issues](https://github.com/RCommon-Team/RCommon/issues) and are very responsive. Community support is available on [Stack Overflow](https://stackoverflow.com/questions/tagged/rcommon).

## Abstractions & Implementations

| Abstraction | What It Provides | Implementations |
|---|---|---|
| **Persistence** | Repository pattern (`ILinqRepository`, `IGraphRepository`, `ISqlMapperRepository`, `IAggregateRepository`), Unit of Work, Specifications, Transactional Outbox/Inbox, Sagas | Entity Framework Core, Dapper, Linq2Db |
| **CQRS & Mediator** | Command/Query Bus (`ICommandBus`, `IQueryBus`), Mediator (`IMediatorService`), pipeline behaviors | MediatR, Wolverine |
| **Event Handling** | In-memory event bus, distributed events, transactional outbox pattern | MediatR, MassTransit, Wolverine |
| **Messaging** | Message bus for distributed systems, send/publish semantics | MassTransit, Wolverine |
| **Caching** | Unified read-through cache (`ICacheService`), query-level cache-aside for repositories | MemoryCache, Redis (StackExchange) |
| **Blob Storage** | Container and blob CRUD, upload/download, presigned URLs, metadata, copy/move | Azure Blob Storage, Amazon S3 |
| **Domain-Driven Design** | Entities, Aggregate Roots (with optimistic concurrency), Domain Events, Value Objects, Auditing, Soft Delete | Built into `RCommon.Entities` |
| **Multi-Tenancy** | Tenant resolution and isolation, per-entity tenant markers | Finbuckle.MultiTenant |
| **State Machines** | Finite state machines (`IStateMachine`), saga state machines | Stateless, MassTransit (Automatonymous) |
| **Serialization** | Provider-agnostic JSON serialization (`IJsonSerializer`) | Newtonsoft.Json, System.Text.Json |
| **Validation** | Pluggable validation pipeline (`IValidationService`) | FluentValidation |
| **Email** | Unified email service (`IEmailService`) | SMTP, SendGrid |
| **Security** | Current user/tenant context, principal accessors, claims-based tenant resolution | ASP.NET Core, Swagger/Swashbuckle |

## Getting Started

All configuration flows through a single fluent builder chain:

```csharp
services.AddRCommon()
    .WithPersistence<EFCorePerisistenceBuilder>(ef => ef
        .AddDbContext<MyDbContext>("MyDb", options => ...))
    .WithMediator<MediatRBuilder>(mediator => mediator
        .AddCommand<MyCommand, MyCommandHandler>())
    .WithEventHandling<MassTransitEventHandlingBuilder>(events => events
        .AddProducer<PublishWithMassTransitEventProducer>())
    .WithCaching<MemoryCachingBuilder>()
    .WithSerialization<SystemTextJsonBuilder>();
```

Install only the packages you need:

```bash
dotnet add package RCommon.Core
dotnet add package RCommon.EfCore
dotnet add package RCommon.Mediatr
# ... and so on
```

## Documentation

Full documentation is available at [https://rcommon.com](https://rcommon.com/docs)

## Stats

![Alt](https://repobeats.axiom.co/api/embed/79bab6079995bd0d448b0f69686e7c2c99a15224.svg "Repobeats analytics image")
