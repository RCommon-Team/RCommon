# Dapper / Linq2Db: Key-Handling and Column-Mapping Correctness

**Branch:** bugfix/consumer-feedback-hardening
**Date:** 2026-07-16
**Status:** Implemented (discovered while building Examples.Persistence.Dapper for the docs-consolidation pass; see docs/specs/documentation/docs-consolidation.md Sub-Spec B)
**Breaking Change:** No

## Problem

What started as a single missing-writeback bug turned out to be three separate, compounding correctness problems across the Dapper and Linq2Db providers, all found via an isolated repro against a real SQLite connection (RCommon's existing test suites for both providers are unit tests against mocks, which is presumably why none of these were caught before now):

**1. Dapper never writes back a database-generated key.** `DapperRepository<TEntity>.AddAsync`/`AddRangeAsync` and `DapperAggregateRepository<TAggregate,TKey>.AddAsync`/`AddRangeAsync` call Dommel's `IDbConnection.InsertAsync(entity, ...)`, which returns the generated key as `Task<object>` — but the return value is discarded, and Dommel does not mutate the entity itself. Any code that uses the entity's `Id` immediately after `AddAsync` (the common "create then use the new id" pattern) silently gets the default value (e.g. `0`).

**2. Dommel's default key-generation guess is wrong for non-numeric keys, and actively corrupts data.** Dommel's default `IKeyPropertyResolver` treats *any* single property named `Id` (or `{TypeName}Id`) as database-generated, regardless of its CLR type — confirmed via reflection: an `int`, a `Guid`, and a `string` "Id" property all resolve to `IsGenerated = true`. For a numeric key this happens to be a reasonable default. For a `Guid` or `string` key — both fully supported by `BusinessEntity<TKey>` and used throughout RCommon's own docs/examples — it means Dommel excludes the key column from the generated `INSERT` statement entirely, silently writing `NULL` (or a provider default) for the primary key instead of the value the caller set. This is not a missing convenience; it is silent data corruption for every Guid/string-keyed entity persisted through the Dapper provider today.

**3. Linq2Db does not honor `BusinessEntity`'s `[NotMapped]` attributes at all.** LinqToDB does not recognize `System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute` (that convention is EF Core-specific). Confirmed via `MappingSchema.Default.GetEntityDescriptor(...)`: `BusinessEntity.LocalEvents`, `BusinessEntity.AllowEventTracking`, and `AggregateRoot<TKey>.DomainEvents` are all mapped as ordinary columns by default. Since no real schema has columns for these framework bookkeeping properties, this means **any entity deriving from `BusinessEntity`/`BusinessEntity<TKey>` fails every real `INSERT`/`SELECT` against the Linq2Db provider** with a "no such column" error. This is more severe than problems 1–2: it is a full-stop failure of the provider for RCommon's own recommended entity base class, not a subtle value-correctness gap.

A fourth, smaller item was found and deliberately **not** fixed here: `Linq2DbRepository<TEntity>.FindAsync(object primaryKey)` is an unimplemented stub (throws `NotImplementedException`). It surfaced only as an incidental blocker while writing this spec's own test and is unrelated to key generation or column mapping — tracked as a separate future item, not fixed in this pass.

## Must Have

- **(Problem 1)** New internal helper `DommelGeneratedKeyHelper` in `RCommon.Dapper` that wraps Dommel's `InsertAsync`, resolves the entity's generated key property via `Dommel.Resolvers.KeyProperties(Type)`, and — when a generated key is found — assigns the returned value back onto the entity via reflection (`Convert.ChangeType` to the property's type, unwrapping `Nullable<T>`). `DapperRepository`/`DapperAggregateRepository`'s `AddAsync`/`AddRangeAsync` route through it.
- **(Problem 1, Linq2Db side)** New internal helper `Linq2DbGeneratedKeyHelper` in `RCommon.Linq2Db` that inspects `DataConnection.MappingSchema.GetEntityDescriptor(typeof(TEntity)).Columns` for a column with `IsIdentity == true`; when found, calls `InsertWithIdentityAsync` instead of `InsertAsync` and assigns the returned value back via the column's `MemberInfo`; when not found, calls plain `InsertAsync` unchanged. `Linq2DbRepository`/`Linq2DbAggregateRepository`'s `AddAsync`/`AddRangeAsync` route through it. (LinqToDB requires an explicit identity mapping — attribute or fluent — before `IsIdentity` is ever `true`; unlike Dommel, it has no naming-convention guess to get wrong.)
- **(Problem 2)** New internal `RCommonKeyPropertyResolver` in `RCommon.Dapper`, wrapping Dommel's `DefaultKeyPropertyResolver`: only numeric key types (`int`, `long`, `short`, `byte`, `decimal`, and their unsigned/nullable variants) are left as database-generated; any other type Dommel guessed as generated is corrected to `DatabaseGeneratedOption.None` unless the property already carries an explicit `[DatabaseGenerated]` attribute (the consumer's deliberate opt-in is always respected). Registered once per process via `DommelMapper.SetKeyPropertyResolver(...)` in `DapperPersistenceBuilder`'s constructor, the same global-static customization mechanism the docs already show for `DommelMapper.AddSqlBuilder`.
- **(Problem 3)** New internal `RCommonFrameworkPropertyMetadataReader` in `RCommon.Linq2Db`, implementing LinqToDB's `IMetadataReader`: injects a `NotColumnAttribute` for `BusinessEntity.LocalEvents`, `BusinessEntity.AllowEventTracking`, and `AggregateRoot<TKey>.DomainEvents` (matched by declaring type, so it applies to every derived entity automatically). Registered once per process via `MappingSchema.Default.AddMetadataReader(...)` in `Linq2DbPersistenceBuilder`'s constructor.
- Both generated-key helpers are no-ops (fall back to the original insert-only behavior) for entities with no generated key (e.g., client-supplied `Guid` keys) — no behavior change for the already-working case.

## Must Not Do

- Must not change the public `IWriteOnlyRepository<TEntity>.AddAsync`/`AddRangeAsync` signatures (still `Task`, not `Task<TKey>`) — the fix is entirely about the entity's `Id` property being correctly populated as a side effect, matching how EF Core already behaves.
- Must not affect entities with composite keys beyond correctly no-op'ing (this fix targets the single-generated-key case, which is what `BusinessEntity<TKey>` already assumes throughout the codebase).
- Must not fix `Linq2DbRepository.FindAsync(object primaryKey)`'s `NotImplementedException` here — out of scope for this spec, tracked separately.
- Must not attempt a global, cross-cutting redesign of either provider's mapping conventions — both fixes are narrowly scoped, additive registrations (a wrapped key-property resolver; an added metadata reader), not a rewrite of `DapperRepository`/`Linq2DbRepository`.

## Testing Strategy

- `RCommon.Dapper.Tests/GeneratedKeyWritebackTests.cs`: real SQLite integration tests proving (a) an `int`-keyed entity gets its `Id` populated after `AddAsync`, and (b) a `Guid`-keyed entity's client-supplied key is inserted as-is and left untouched (this second assertion is what actually exercises the `RCommonKeyPropertyResolver` fix — without it, the test throws `InvalidCastException` trying to convert a `last_insert_rowid()` `Int64` into a `Guid`).
- `RCommon.Linq2Db.Tests/GeneratedKeyWritebackTests.cs`: real SQLite integration tests proving the same round-trip for an entity with a fluent-mapped identity column, and that a `Guid`-keyed `BusinessEntity<Guid>` entity inserts and reads back correctly (this exercises the `RCommonFrameworkPropertyMetadataReader` fix — without it, every insert in this test file fails with "no such column: AllowEventTracking").
- Full regression: `dotnet build Src/RCommon.sln`, `dotnet test` for `RCommon.Dapper.Tests` and `RCommon.Linq2Db.Tests`, 0 errors/0 failures.

## File Summary

| File | Action |
|------|--------|
| `Src/RCommon.Dapper/Crud/DommelGeneratedKeyHelper.cs` | Create |
| `Src/RCommon.Dapper/Crud/RCommonKeyPropertyResolver.cs` | Create |
| `Src/RCommon.Dapper/Crud/DapperRepository.cs` | Modify — route `AddAsync`/`AddRangeAsync` through the helper |
| `Src/RCommon.Dapper/Crud/DapperAggregateRepository.cs` | Modify — same |
| `Src/RCommon.Dapper/DapperPersistenceBuilder.cs` | Modify — register `RCommonKeyPropertyResolver` once per process |
| `Src/RCommon.Linq2Db/Crud/Linq2DbGeneratedKeyHelper.cs` | Create |
| `Src/RCommon.Linq2Db/Crud/RCommonFrameworkPropertyMetadataReader.cs` | Create |
| `Src/RCommon.Linq2Db/Crud/Linq2DbRepository.cs` | Modify — route `AddAsync`/`AddRangeAsync` through the helper |
| `Src/RCommon.Linq2Db/Crud/Linq2DbAggregateRepository.cs` | Modify — same |
| `Src/RCommon.Linq2Db/Linq2DbPersistenceBuilder.cs` | Modify — register `RCommonFrameworkPropertyMetadataReader` once per process |
| `Tests/RCommon.Dapper.Tests/GeneratedKeyWritebackTests.cs` | Create |
| `Tests/RCommon.Linq2Db.Tests/GeneratedKeyWritebackTests.cs` | Create |
| `Tests/RCommon.Dapper.Tests/RCommon.Dapper.Tests.csproj` | Modify — add `Microsoft.Data.Sqlite` + pinned `SQLitePCLRaw.lib.e_sqlite3` |
