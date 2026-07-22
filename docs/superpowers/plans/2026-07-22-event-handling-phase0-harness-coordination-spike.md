# Event Handling 3.2.0 — Phase 0: Integration Harness + Broker-Coordination Spike — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up a real-database/broker integration-test harness on Podman (Testcontainers: Postgres + RabbitMQ) and use it to prove or disprove the recipe-2b broker-outbox coordination invariant (spec AC-15) *before* any production code is built on it.

**Architecture:** A new `Tests/RCommon.IntegrationTests` project holds xUnit collection fixtures that start Postgres and RabbitMQ containers via Testcontainers-for-.NET (configured to talk to Podman's socket). A harness smoke test proves the Podman wiring. Two *spike* tests wire MassTransit's and Wolverine's native EF Core outboxes directly (raw APIs — no RCommon wrapper yet) inside a RCommon `UnitOfWork` `TransactionScope`, and assert that business state + broker-outbox rows commit atomically and that a rollback leaves neither. The spike's outcome is recorded and gates the recipe-2b design in later phases (fallback: recipe 2a).

**Tech Stack:** .NET 10, xUnit 2.9.3, AwesomeAssertions 7.2.1 (FluentAssertions namespace), Testcontainers 4.x (`Testcontainers.PostgreSql`, `Testcontainers.RabbitMq`), Npgsql EF Core provider (EF10-compatible), EF Core 10.0.8, MassTransit 8.5.9 (+ `.EntityFrameworkCore`), WolverineFx 5.39.1 (+ `.EntityFrameworkCore`), Podman as the container runtime.

**Spec:** `docs/specs/event-handling/event-handling.md` (AC-11, AC-14, AC-15). **Design:** `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md` (§5). **Branch:** `feature/event-handling-outbox-recipes`.

---

## Prerequisite: Podman + Testcontainers

Testcontainers-for-.NET speaks the Docker API; Podman exposes a compatible socket. Before running any task below, the machine/CI runner must have the Podman socket enabled and `DOCKER_HOST` pointing at it. Document this in the project README (Task 2). Typical local setup:

- **Linux (rootless):** `systemctl --user enable --now podman.socket` → `export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock`
- **Windows/macOS:** `podman machine init && podman machine start`; use the machine's socket path for `DOCKER_HOST`.
- Ryuk (Testcontainers' resource reaper) often needs disabling under rootless Podman: set `TESTCONTAINERS_RYUK_DISABLED=true` (document the trade-off: containers must be cleaned by fixture disposal). Prefer keeping Ryuk on if the runner supports it.

> These are environment facts, not code. The tasks assume `DOCKER_HOST` is set; a fixture-level guard (Task 3) fails loud with a clear message if no container runtime is reachable.

## File structure

- Create: `Tests/RCommon.IntegrationTests/RCommon.IntegrationTests.csproj` — the integration test project (net10.0).
- Create: `Tests/RCommon.IntegrationTests/README.md` — Podman/Testcontainers setup notes.
- Create: `Tests/RCommon.IntegrationTests/Fixtures/PostgreSqlFixture.cs` — Postgres container collection fixture.
- Create: `Tests/RCommon.IntegrationTests/Fixtures/RabbitMqFixture.cs` — RabbitMQ container collection fixture.
- Create: `Tests/RCommon.IntegrationTests/Fixtures/IntegrationCollections.cs` — xUnit `[CollectionDefinition]`s.
- Create: `Tests/RCommon.IntegrationTests/HarnessSmokeTests.cs` — proves Podman + Postgres + EF Core wiring.
- Create: `Tests/RCommon.IntegrationTests/Spikes/MassTransitOutboxCoordinationSpikeTests.cs` — recipe-2b spike (MassTransit).
- Create: `Tests/RCommon.IntegrationTests/Spikes/WolverineOutboxCoordinationSpikeTests.cs` — recipe-2b spike (Wolverine).
- Create: `Tests/RCommon.IntegrationTests/Spikes/SPIKE-FINDINGS.md` — recorded outcome + go/no-go for recipe 2b.
- Modify: `Src/RCommon.sln` — add the new project.
- Modify: `.github/workflows/build-dotnet8.yml` — add an integration-test job (Podman) — Task 9.

> Note: `Tests/Directory.Build.props` already provides xUnit, AwesomeAssertions, Moq, Microsoft.NET.Test.Sdk, coverlet, and net10.0. The new csproj only adds Testcontainers, Npgsql, and the broker packages, plus project references.

> **CRITICAL — CI trait convention.** The new project is added to `Src/RCommon.sln`, and the existing **fast** CI job runs `dotnet test Src/RCommon.sln … --filter "Category!=Integration"` on a runner with **no container runtime**. Per the repo's established convention (see `Tests/RCommon.Azure.Blobs.Tests/AzureBlobStorageServiceTests.cs`), **every** container-dependent test class in this project MUST carry a class-level `[Trait("Category", "Integration")]` so the fast job excludes it. Omitting the trait makes the fast job discover and run these container tests on a Podman-less runner and go red on the very PR that introduces them. Every test class below (`HarnessSmokeTests`, `MassTransitOutboxCoordinationSpikeTests`, `WolverineOutboxCoordinationSpikeTests`) carries this trait. (`CollectCoverage=false` only affects coverage collection, not test discovery — it is not the exclusion mechanism.)

---

### Task 1: Create the integration test project

**Files:**
- Create: `Tests/RCommon.IntegrationTests/RCommon.IntegrationTests.csproj`
- Modify: `Src/RCommon.sln`

- [ ] **Step 1: Write the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Exclude from the default fast unit-test coverage sweep; these are heavy container tests. -->
    <CollectCoverage>false</CollectCoverage>
  </PropertyGroup>

  <ItemGroup>
    <!-- Container runtime harness (talks to Podman via DOCKER_HOST). Confirm latest 4.x. -->
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.6.0" />
    <PackageReference Include="Testcontainers.RabbitMq" Version="4.6.0" />

    <!-- Postgres EF Core provider compatible with EF Core 10.0.8. Confirm the exact EF10-compatible version. -->
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />

    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.8" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.8" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.8" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Src\RCommon.EfCore\RCommon.EFCore.csproj" />
    <ProjectReference Include="..\..\Src\RCommon.MassTransit\RCommon.MassTransit.csproj" />
    <ProjectReference Include="..\..\Src\RCommon.MassTransit.Outbox\RCommon.MassTransit.Outbox.csproj" />
    <ProjectReference Include="..\..\Src\RCommon.Wolverine\RCommon.Wolverine.csproj" />
    <ProjectReference Include="..\..\Src\RCommon.Wolverine.Outbox\RCommon.Wolverine.Outbox.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add the project to the solution**

Run: `dotnet sln Src/RCommon.sln add Tests/RCommon.IntegrationTests/RCommon.IntegrationTests.csproj`
Expected: "Project ... added to the solution."

- [ ] **Step 3: Restore/build to verify references resolve**

Run: `dotnet build Tests/RCommon.IntegrationTests/RCommon.IntegrationTests.csproj`
Expected: Build succeeded, 0 errors. (If Npgsql/Testcontainers versions don't resolve against EF 10, adjust to the nearest compatible versions and note it.)

- [ ] **Step 4: Commit**

```bash
git add Tests/RCommon.IntegrationTests/RCommon.IntegrationTests.csproj Src/RCommon.sln
git commit -m "test(integration): scaffold RCommon.IntegrationTests project"
```

---

### Task 2: Podman/Testcontainers README

**Files:**
- Create: `Tests/RCommon.IntegrationTests/README.md`

- [ ] **Step 1: Write the README** documenting the Podman prerequisite (socket enable, `DOCKER_HOST`, optional `TESTCONTAINERS_RYUK_DISABLED`), that these tests are excluded from the fast unit run, and how to run them: `dotnet test Tests/RCommon.IntegrationTests`. Include the exact Linux/Windows/macOS socket setup from the Prerequisite section above.

- [ ] **Step 2: Commit**

```bash
git add Tests/RCommon.IntegrationTests/README.md
git commit -m "docs(integration): Podman/Testcontainers setup notes"
```

---

### Task 3: Postgres collection fixture

**Files:**
- Create: `Tests/RCommon.IntegrationTests/Fixtures/PostgreSqlFixture.cs`
- Create: `Tests/RCommon.IntegrationTests/Fixtures/IntegrationCollections.cs`

- [ ] **Step 1: Write the fixture**

```csharp
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace RCommon.IntegrationTests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Fail loud with an actionable message if no container runtime is reachable.
        try
        {
            await _container.StartAsync();
        }
        catch (System.Exception ex)
        {
            throw new System.InvalidOperationException(
                "Could not start the Postgres test container. Ensure Podman is running and DOCKER_HOST " +
                "points at the Podman socket (see Tests/RCommon.IntegrationTests/README.md).", ex);
        }
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

- [ ] **Step 2: Write the collection definition**

```csharp
using Xunit;

namespace RCommon.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql";
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build Tests/RCommon.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Tests/RCommon.IntegrationTests/Fixtures/PostgreSqlFixture.cs Tests/RCommon.IntegrationTests/Fixtures/IntegrationCollections.cs
git commit -m "test(integration): Postgres Testcontainers fixture"
```

---

### Task 4: Harness smoke test (proves Podman + Postgres + EF Core)

**Files:**
- Create: `Tests/RCommon.IntegrationTests/HarnessSmokeTests.cs`

- [ ] **Step 1: Write the failing test**

A minimal EF Core `DbContext` (declared in the test file) against the container, proving migrate/insert/read works end to end. This is the "does the harness work at all" gate.

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RCommon.IntegrationTests.Fixtures;
using Xunit;

namespace RCommon.IntegrationTests;

[Trait("Category", "Integration")]   // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgreSqlCollection.Name)]
public class HarnessSmokeTests
{
    private readonly PostgreSqlFixture _pg;
    public HarnessSmokeTests(PostgreSqlFixture pg) => _pg = pg;

    private sealed class Widget { public int Id { get; set; } public string Name { get; set; } = ""; }

    private sealed class SmokeContext(DbContextOptions<SmokeContext> o) : DbContext(o)
    {
        public DbSet<Widget> Widgets => Set<Widget>();
    }

    [Fact]
    public async Task Postgres_container_round_trips_a_row()
    {
        var options = new DbContextOptionsBuilder<SmokeContext>()
            .UseNpgsql(_pg.ConnectionString).Options;

        await using var ctx = new SmokeContext(options);
        await ctx.Database.EnsureCreatedAsync();

        ctx.Widgets.Add(new Widget { Name = "hello" });
        await ctx.SaveChangesAsync();

        var count = await ctx.Widgets.CountAsync(w => w.Name == "hello");
        count.Should().Be(1);
    }
}
```

- [ ] **Step 2: Run it and verify it PASSES against a running Podman**

Run: `dotnet test Tests/RCommon.IntegrationTests --filter "FullyQualifiedName~HarnessSmokeTests"`
Expected: PASS. (This is an infrastructure smoke test, not TDD-red — its job is to confirm the harness. If it fails to start the container, fix the Podman/`DOCKER_HOST` setup, not the test.)

- [ ] **Step 3: Commit**

```bash
git add Tests/RCommon.IntegrationTests/HarnessSmokeTests.cs
git commit -m "test(integration): harness smoke test on Podman+Postgres"
```

---

### Task 5: RabbitMQ collection fixture

**Files:**
- Create: `Tests/RCommon.IntegrationTests/Fixtures/RabbitMqFixture.cs`
- Modify: `Tests/RCommon.IntegrationTests/Fixtures/IntegrationCollections.cs` (add a `RabbitMqCollection`, and a combined `PostgresAndRabbitMqCollection` for the spike tests that need both)

- [ ] **Step 1: Write the RabbitMQ fixture** (mirror `PostgreSqlFixture`, using `RabbitMqBuilder().WithImage("rabbitmq:3-management-alpine")`, exposing `ConnectionString` / host+port; same fail-loud guard).

- [ ] **Step 2: Add collection definitions** — a `RabbitMqCollection`, plus a fixture that a single collection can expose both containers to the spike tests (either a combined fixture that composes both, or two `ICollectionFixture<>` on one collection class).

- [ ] **Step 3: Build**

Run: `dotnet build Tests/RCommon.IntegrationTests`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Tests/RCommon.IntegrationTests/Fixtures/
git commit -m "test(integration): RabbitMQ Testcontainers fixture"
```

---

### Task 6: MassTransit outbox coordination spike

**Goal of this task:** determine empirically whether a `Publish` issued *inside* RCommon's `UnitOfWork` `TransactionScope` stages atomically into MassTransit's EF Core outbox tables — i.e. business state + MT outbox rows commit together, and a rollback leaves neither.

**Files:**
- Create: `Tests/RCommon.IntegrationTests/Spikes/MassTransitOutboxCoordinationSpikeTests.cs`

> **Before writing the wiring:** confirm the exact MassTransit 8.5.9 EF Core outbox configuration API via context7 (`AddEntityFrameworkOutbox<TDbContext>` + `UsePostgres()` + `UseBusOutbox()`, and how the DbContext must expose the MT outbox `DbSet`s / model config `AddTransactionalOutboxEntities`). The shape below is the expected API; verify method names/signatures at that version.

- [ ] **Step 1: Write the atomic-commit spike test**

A `DbContext` that includes both a business entity and MassTransit's outbox entities (via MT's model-configuration extension). Wire MassTransit with the EF outbox + bus outbox against the Postgres container. Open a RCommon `UnitOfWork` (real `TransactionScope`), write a business row, `Publish` an integration event, then commit. Assert **both** the business row and a MassTransit outbox row exist after commit.

```csharp
// Skeleton — exact MT config confirmed via context7 for 8.5.9.
[Trait("Category", "Integration")]   // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgresAndRabbitMqCollection.Name)]
public class MassTransitOutboxCoordinationSpikeTests
{
    // ctor takes the Postgres + RabbitMQ fixtures.

    [Fact]
    public async Task Publish_inside_RCommon_UnitOfWork_stages_atomically_into_MassTransit_outbox()
    {
        // Arrange: services.AddDbContext<SpikeDbContext>(UseNpgsql(pg));
        //          services.AddMassTransit(x => x.AddEntityFrameworkOutbox<SpikeDbContext>(o => { o.UsePostgres(); o.UseBusOutbox(); }); x.UsingInMemory(...));
        //          AddRCommon().WithPersistence<EFCorePersistenceBuilder>(...)  // real UnitOfWork/TransactionScope
        // Act: using (var uow = uowFactory.Create()) { repo.Add(businessRow); await bus.Publish(evt); await uow.CommitAsync(); }
        // Assert:
        //   businessRows.Count().Should().Be(1);
        //   massTransitOutboxRows (OutboxMessage table).Count().Should().Be(1);
    }
}
```

- [ ] **Step 2: Run it; record the result**

Run: `dotnet test Tests/RCommon.IntegrationTests --filter "FullyQualifiedName~MassTransitOutboxCoordinationSpikeTests"`
Expected: **This is a spike — either outcome is informative.** PASS ⇒ the seam encloses atomically (recipe 2b viable for MassTransit). FAIL ⇒ capture the failure mode (e.g., MT outbox uses its own transaction/DbContext SaveChanges that does not enlist in RCommon's `TransactionScope`).

- [ ] **Step 3: Write the rollback spike test**

Same wiring, but throw before `CommitAsync` (or roll the UoW back). Assert **neither** the business row nor any MassTransit outbox row exists.

- [ ] **Step 4: Run it; record the result**

Run the rollback test. Record PASS/FAIL and the observed behavior.

- [ ] **Step 5: Commit**

```bash
git add Tests/RCommon.IntegrationTests/Spikes/MassTransitOutboxCoordinationSpikeTests.cs
git commit -m "test(spike): MassTransit EF outbox coordination under RCommon UnitOfWork"
```

---

### Task 7: Wolverine outbox coordination spike

**Files:**
- Create: `Tests/RCommon.IntegrationTests/Spikes/WolverineOutboxCoordinationSpikeTests.cs`

> **Before wiring:** confirm the exact WolverineFx 5.39.1 EF Core outbox API via context7 (`UseEntityFrameworkCoreTransactions()`, `AddDbContextWithWolverineIntegration<TDbContext>()` / `IDbContextOutbox`, and how Wolverine enlists in an ambient transaction vs. owning its own message context). The known friction is that Wolverine prefers to own the transaction/message context.

- [ ] **Step 1: Write the atomic-commit spike test** — analogous to Task 6 Step 1, using Wolverine's durable EF outbox against Postgres; `PublishAsync` inside the RCommon UoW; assert business row + a Wolverine outgoing-envelope row both exist after commit. The test class MUST carry class-level `[Trait("Category", "Integration")]` (see the CRITICAL note in File Structure).

- [ ] **Step 2: Run it; record the result** (spike — either outcome informative).

- [ ] **Step 3: Write the rollback spike test** — assert neither row exists after a rolled-back UoW.

- [ ] **Step 4: Run it; record the result.**

- [ ] **Step 5: Commit**

```bash
git add Tests/RCommon.IntegrationTests/Spikes/WolverineOutboxCoordinationSpikeTests.cs
git commit -m "test(spike): Wolverine EF outbox coordination under RCommon UnitOfWork"
```

---

### Task 8: Record spike findings + recipe-2b go/no-go

**Files:**
- Create: `Tests/RCommon.IntegrationTests/Spikes/SPIKE-FINDINGS.md`
- Modify: `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md` (§5 — annotate the verification outcome)

- [ ] **Step 1: Write findings** — for each broker: did atomic-commit hold? did rollback leave nothing? If atomic, note the exact wiring that worked (this becomes the basis for the Phase 4 `UseBrokerOutbox` wrapper). If not atomic, note the failure mode and confirm the fallback: recipe 2a (broker as a producer behind RCommon's own outbox), which has no such coupling.

- [ ] **Step 2: Update §5 of the design doc** with a one-paragraph "Spike outcome" note (use the **actual** current date, not a placeholder) recording the go/no-go for recipe 2b per broker.

- [ ] **Step 3: Commit**

```bash
git add Tests/RCommon.IntegrationTests/Spikes/SPIKE-FINDINGS.md docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md
git commit -m "docs(spike): record broker-outbox coordination findings + recipe-2b go/no-go"
```

---

### Task 9: CI — run integration tests on Podman

**Files:**
- Modify: `.github/workflows/build-dotnet8.yml`

- [ ] **Step 1: Add an integration-test job** that provisions Podman on the runner (enable the user socket, export `DOCKER_HOST`; note the GitHub-hosted `ubuntu-latest` runner ships Docker, not a preconfigured rootless Podman socket, so this step must install/enable Podman and may hit the documented Ryuk-under-Podman friction — set `TESTCONTAINERS_RYUK_DISABLED=true` if needed). Run the suite via the shared trait convention: `dotnet test Src/RCommon.sln --filter "Category=Integration"` (the inclusion half of the fast job's `Category!=Integration` exclusion), or target the project directly: `dotnet test Tests/RCommon.IntegrationTests`. Gate the job to run on PRs to `main` and pushes to `main` (not every push) to control CI minutes, per the cost doc. Keep the existing fast unit-test job unchanged.

- [ ] **Step 2: Verify the workflow YAML parses** (lint locally or push a draft PR to observe the run).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/build-dotnet8.yml
git commit -m "ci: run integration tests on Podman for PRs to main"
```

---

## Exit criteria (Phase 0 done when)

- [ ] `RCommon.IntegrationTests` builds and the harness smoke test passes against a Podman-hosted Postgres.
- [ ] Both broker coordination spikes (MassTransit + Wolverine) have run, with commit + rollback outcomes recorded in `SPIKE-FINDINGS.md`.
- [ ] A go/no-go for recipe 2b per broker is documented; if no-go for either, the recipe-2a fallback is confirmed for that broker (satisfies the AC-15 gate before Phase 4).
- [ ] CI runs the integration suite on Podman for PRs to main.

## Notes for the executor

- **This phase is a spike + infrastructure**, not feature code — the coordination tests are allowed to "fail" as a legitimate, recorded outcome (they de-risk a design decision). Everything else (fixtures, smoke test, CI) is normal TDD-quality code.
- **Confirm library APIs via context7** (MassTransit 8.5.9, WolverineFx 5.39.1, Npgsql EF provider for EF10) before finalizing the spike wiring — these APIs drift across versions and the plan's skeletons reflect expected, not verified, shapes.
- Follow @superpowers:test-driven-development for the harness/fixture code where a red-green cycle applies.
