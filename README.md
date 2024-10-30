# RCommon

## Overview
RCommon is a cohesive set of libraries with abstractions for widely used implementations of design patterns, and architectural patterns which are common (see what we did there?) to many applications used today. The primary goals of this toolset are:
1. Future proofing applications against changing architectural or infrastructure needs.
2. Solve common problems under the presentation layer. Presentation frameworks are something else entirely. We try to keep everything nice under the hood. Cross cutting concerns, persistence strategies, transaction management, event handling, and messaging is where we want to shine.
3. Code testability. We try to limit the "magic" used. Things like dependency injection are used but in a very straightforward manner. Unit tests, and integration tests should be implemented to the highest degree possible. Afterall, we want the applications you build on top of this to work :) 
4. Last but not least - open source using permissive Apache2.0 licensingforever. 

We track bugs, enhancement requests, new feature requests, and general issues on [GitHub Issues](https://github.com/Reactor2Team/RCommon/issues "GitHub Issues") and are very responsive. General "how to" and community support should be managed on [Stack Overflow](https://stackoverflow.com/questions/tagged/rcommon "Stack Overflow"). 

## Patterns & Abstractions Utilized
* Specification
* Mediator
* Command Query Responsbility Segregation (CQRS)
* Validations
* Repository
* Unit Of Work
* Event Sourcing (Coming Soon)
* Event Bus
* Message Bus
* Caching
* Serialization
* Generic Factory
* Guard
* Data Transfer Objects (DTO)

## Pattern Implementations
* Mediator: MediatR
* Repository: Entity Framework Core, Dapper, Linq2Db
* Message Bus: MassTransit, Wolverine
* Email: SMTP, SendGrid
* Validation: FluentValidation
* Caching: MemoryCache, Redis, Valkey
* Serialization: JSON.NET, System.Text.Json

## Documentation
We are maintaining and publishing our documentation at [https://docs.rcommon.com](https://docs.rcommon.com)

## Stats
![Alt](https://repobeats.axiom.co/api/embed/15855081ce579ae0ea03577b9b5a6c2ae882fb7f.svg "Repobeats analytics image")
