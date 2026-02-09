using LinqToDB;
using LinqToDB.Configuration;

namespace RCommon.Persistence.Linq2Db
{
    /// <summary>
    /// Defines the fluent builder interface for configuring Linq2Db-based persistence in RCommon.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="IPersistenceBuilder"/> to add Linq2Db-specific configuration such as
    /// registering <see cref="RCommonDataConnection"/>-derived data connections with named data stores.
    /// </remarks>
    public interface ILinq2DbPersistenceBuilder: IPersistenceBuilder
    {
        /// <summary>
        /// Registers a Linq2Db data connection type with the specified data store name and configuration options.
        /// </summary>
        /// <typeparam name="TDataConnection">The type of the data connection. Must derive from <see cref="RCommonDataConnection"/>.</typeparam>
        /// <param name="dataStoreName">A unique name identifying this data store for resolution via <see cref="IDataStoreFactory"/>.</param>
        /// <param name="options">A factory function that receives the <see cref="IServiceProvider"/> and existing <see cref="DataOptions"/>, returning configured <see cref="DataOptions"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        ILinq2DbPersistenceBuilder AddDataConnection<TDataConnection>(string dataStoreName, Func<IServiceProvider, DataOptions, DataOptions> options) where TDataConnection : RCommonDataConnection;
    }
}
