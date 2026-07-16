using System.Runtime.CompilerServices;
using LinqToDB.Mapping;

namespace RCommon.Persistence.Linq2Db.Crud
{
    /// <summary>
    /// Registers <see cref="RCommonFrameworkPropertyMetadataReader"/> on <see cref="MappingSchema.Default"/>
    /// as soon as this assembly loads.
    /// </summary>
    /// <remarks>
    /// This must happen before any LinqToDB operation resolves an entity descriptor for any entity type --
    /// LinqToDB caches resolution results (including provider-level schemas built the first time a
    /// provider like SQLite is used) internally, so a later registration (e.g. from inside
    /// <c>Linq2DbPersistenceBuilder</c>'s constructor) can lose a race against whichever code path
    /// happens to touch LinqToDB first (observed in practice as order-dependent test flakiness). A module
    /// initializer is guaranteed by the runtime to run once, before any other code in this assembly.
    /// </remarks>
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            MappingSchema.Default.AddMetadataReader(new RCommonFrameworkPropertyMetadataReader());
        }
    }
}
