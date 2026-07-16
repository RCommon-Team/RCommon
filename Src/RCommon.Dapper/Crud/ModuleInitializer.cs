using System.Runtime.CompilerServices;
using Dommel;

namespace RCommon.Persistence.Dapper.Crud
{
    /// <summary>
    /// Registers <see cref="RCommonKeyPropertyResolver"/> with Dommel as soon as this assembly loads.
    /// </summary>
    /// <remarks>
    /// This must happen before any Dommel operation resolves key properties for any entity type --
    /// Dommel caches resolution results internally, so a later registration (e.g. from inside
    /// <c>DapperPersistenceBuilder</c>'s constructor) can lose a race against whichever code path
    /// happens to touch Dommel first (observed in practice as order-dependent test flakiness). A module
    /// initializer is guaranteed by the runtime to run once, before any other code in this assembly.
    /// </remarks>
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            DommelMapper.SetKeyPropertyResolver(new RCommonKeyPropertyResolver(new DefaultKeyPropertyResolver()));
        }
    }
}
