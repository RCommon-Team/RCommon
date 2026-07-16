using Xunit;

// GeneratedKeyWritebackTests registers a custom Dommel IKeyPropertyResolver and SQL builder on
// Dommel's process-wide static state the first time DapperPersistenceBuilder is constructed (see
// docs/specs/persistence/dapper-linq2db-generated-key-writeback.md). Under xUnit's default per-class
// parallelism, other test classes in this assembly could race on that same global mutable state --
// the same category of flakiness fixed for RCommon.Linq2Db.Tests by serializing that assembly.
// Serialize this assembly's tests too, for the same reason.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
