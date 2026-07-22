# Event Handling — Cost Considerations

## Related Spec

`./event-handling.md`

## Resources Required

RCommon is an open-source library; there is no per-customer runtime cost to RCommon itself. Costs are development-time and CI:

- **CI compute for integration tests.** New Podman/Testcontainers-based integration tests spin up Postgres and RabbitMQ containers per run. This adds runtime and resource use to CI jobs and requires a Podman-capable runner (rootless socket / `DOCKER_HOST`).
- **Developer time.** A multi-phase 3.2.0 effort (pipeline reorder, per-datastore outbox, fluent API, transport wrappers, five recipe examples, docs/migration guide).
- **No new licenses.** MassTransit, Wolverine, Postgres, RabbitMQ, and Testcontainers are open-source/free for this use.

## Budget Status

Not a funded/budgeted initiative in the financial sense — maintainer-driven OSS work. The only recurring cost is incremental CI minutes for the container-based integration suite.

## 3-Year Cost Projections

- **CI minutes** grow with test-suite size and PR volume; the container-based integration tests are the main driver. Mitigations: gate the heavy integration suite to run on main/PR-to-main (not every push) or nightly, keeping fast unit tests on every push.
- **No data-accumulation or per-tenant runtime cost** accrues to RCommon; downstream applications bear their own database/broker costs, unchanged by this design (the outbox reuses the application's existing database).
- **Maintenance cost** scales modestly with the number of supported transports/TFMs (MassTransit/Wolverine version drift across net8/9/10), consistent with the existing multi-target burden.
