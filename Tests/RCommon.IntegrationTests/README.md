# RCommon.IntegrationTests

Container-backed integration tests for RCommon. Each test fixture uses
[Testcontainers-for-.NET](https://dotnet.testcontainers.org/) to spin up real
Postgres and RabbitMQ containers for the duration of the test run, then tears
them down via `IAsyncLifetime.DisposeAsync`.

This project also hosts the **Phase 0 broker-outbox coordination spike** tests,
which exercise the end-to-end outbox pipeline across MassTransit/Outbox and
Wolverine/Outbox using live containers.

## CI vs fast unit tests

Every test class in this project is decorated with:

```csharp
[Trait("Category", "Integration")]
```

The fast unit-test CI job excludes these tests with:

```bash
dotnet test ... --filter "Category!=Integration"
```

Integration tests are run in a separate CI job that provisions a container
runtime.

---

## Prerequisites

### 1. Container runtime

Testcontainers-for-.NET speaks the Docker API. This project targets
**Podman** as the container runtime. Podman exposes a Docker-compatible socket,
and you point Testcontainers at it via the `DOCKER_HOST` environment variable.

> Testcontainers also works with a standard Docker Desktop installation if you
> have that instead; set `DOCKER_HOST` to the Docker socket path accordingly.

#### Windows

```powershell
podman machine init          # first time only
podman machine start
```

The Docker-compatible endpoint on Windows is a named pipe. The dev machine for
this repo uses the default machine name `podman-machine-default`, so:

```
DOCKER_HOST=npipe:////./pipe/podman-machine-default
```

Confirm the machine is running:

```powershell
podman machine list
```

#### macOS

```bash
podman machine init   # first time only
podman machine start
```

Find the socket path:

```bash
podman machine inspect | grep -i socket
```

Then set:

```bash
export DOCKER_HOST=unix:///path/to/podman-machine.sock
```

#### Linux (rootless)

Enable the Podman socket:

```bash
systemctl --user enable --now podman.socket
export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
```

### 2. Ryuk (resource reaper)

Testcontainers' Ryuk side-car cleans up containers and networks after a test
run. Ryuk requires a Docker API connection with elevated socket permissions that
rootless Podman may not satisfy, causing fixture startup failures.

Set `TESTCONTAINERS_RYUK_DISABLED=true` to disable Ryuk:

**Trade-off:** with Ryuk disabled, container cleanup is handled entirely by
fixture disposal (`IAsyncLifetime.DisposeAsync`). If a test run is killed
mid-flight (Ctrl-C, OOM, crash), containers may be left running and you will
need to clean them up manually:

```bash
podman ps -a               # list containers
podman rm -f <id>          # remove a specific container
```

Keep Ryuk enabled when the runtime supports it (e.g. rootful Docker, Podman
with rootful socket). Prefer `TESTCONTAINERS_RYUK_DISABLED=true` only when
the socket permissions require it.

---

## Running the tests

### PowerShell (Windows)

```powershell
$env:DOCKER_HOST = "npipe:////./pipe/podman-machine-default"
$env:TESTCONTAINERS_RYUK_DISABLED = "true"
dotnet test Tests/RCommon.IntegrationTests
```

### bash (Linux / macOS)

```bash
export DOCKER_HOST="unix:///run/user/$(id -u)/podman/podman.sock"
export TESTCONTAINERS_RYUK_DISABLED="true"
dotnet test Tests/RCommon.IntegrationTests
```

### Running a subset

```bash
dotnet test Tests/RCommon.IntegrationTests --filter "FullyQualifiedName~HarnessSmokeTests"
```

Any valid `dotnet test --filter` expression works; see
[Microsoft docs](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests)
for the full filter syntax.

---

## Troubleshooting

**"Could not start the ... test container"**

This almost always means the container runtime is not reachable. Check:

1. The Podman machine is running:
   ```powershell
   podman machine list
   ```
   The machine should show **Currently running** in the output.

2. `DOCKER_HOST` is set in the current shell session to the correct socket path
   or named pipe for your OS (see Prerequisites above).

3. Podman can pull images:
   ```bash
   podman pull docker.io/library/postgres:16-alpine
   ```
   If this fails, check network access and Podman machine health.
