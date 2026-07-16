using System.Runtime.CompilerServices;
using LinqToDB.Mapping;

namespace RCommon.Linq2Db.Tests;

/// <summary>
/// Configures the fluent mapping GeneratedKeyWritebackTests.Widget needs as soon as this test assembly
/// loads, for the same reason RCommon.Linq2Db's own ModuleInitializer registers
/// RCommonFrameworkPropertyMetadataReader at module-load time rather than lazily: LinqToDB caches
/// provider-level schema state (e.g. built the first time SQLite is used) internally, so a registration
/// that runs lazily inside a test constructor can lose a race against whichever test happens to touch
/// LinqToDB first -- observed in practice as order-dependent "no such table: Widget" flakiness.
/// </summary>
internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        new FluentMappingBuilder(MappingSchema.Default)
            .Entity<GeneratedKeyWritebackTests.Widget>()
            .HasTableName("Widgets")
            .Property(w => w.Id).IsPrimaryKey().IsIdentity()
            .Build();
    }
}
