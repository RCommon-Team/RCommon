using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Defines the fluent builder interface for configuring Entity Framework Core persistence in RCommon.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="IPersistenceBuilder"/> to add EF Core-specific configuration such as
    /// registering <see cref="RCommonDbContext"/> instances with named data stores.
    /// </remarks>
    public interface IEFCorePersistenceBuilder : IPersistenceBuilder
    {
        /// <summary>
        /// Registers a <see cref="RCommonDbContext"/>-derived DbContext with the specified data store name and options.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the DbContext to register. Must derive from <see cref="RCommonDbContext"/>.</typeparam>
        /// <param name="dataStoreName">A unique name identifying this data store for resolution via <see cref="IDataStoreFactory"/>.</param>
        /// <param name="options">An optional action to configure the <see cref="DbContextOptionsBuilder"/> for this context.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        IEFCorePersistenceBuilder AddDbContext<TDbContext>(string dataStoreName, Action<DbContextOptionsBuilder>? options) where TDbContext : RCommonDbContext;
    }
}
