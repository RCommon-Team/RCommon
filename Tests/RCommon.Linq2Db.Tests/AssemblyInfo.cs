using Xunit;

// GeneratedKeyWritebackTests registers a custom LinqToDB IMetadataReader and fluent mapping on the
// process-wide MappingSchema.Default the first time Linq2DbPersistenceBuilder is constructed (see
// docs/specs/persistence/dapper-linq2db-generated-key-writeback.md). Under xUnit's default per-class
// parallelism, other test classes in this assembly construct DataConnection/RCommonDataConnection
// instances concurrently on separate threads, racing on that same global mutable state -- observed as
// deterministic "no such column"/"no such table" failures that disappear once parallelization is off.
// Serialize the assembly's tests to make the shared static state deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
