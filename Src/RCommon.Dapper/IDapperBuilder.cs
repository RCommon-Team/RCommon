using RCommon.Persistence.Sql;
using System;

namespace RCommon
{
    /// <summary>
    /// Defines the fluent builder interface for configuring Dapper-based persistence in RCommon.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="IPersistenceBuilder"/> to add Dapper-specific configuration such as
    /// registering <see cref="RDbConnection"/>-derived database connections with named data stores.
    /// </remarks>
    public interface IDapperBuilder : IPersistenceBuilder
    {
        /// <summary>
        /// Registers a database connection type with the specified data store name and connection options.
        /// </summary>
        /// <typeparam name="TDbConnection">The type of the database connection. Must derive from <see cref="RDbConnection"/>.</typeparam>
        /// <param name="dataStoreName">A unique name identifying this data store for resolution via <see cref="IDataStoreFactory"/>.</param>
        /// <param name="options">An action to configure the <see cref="RDbConnectionOptions"/> (e.g., connection string).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        IDapperBuilder AddDbConnection<TDbConnection>(string dataStoreName, Action<RDbConnectionOptions> options) where TDbConnection : RDbConnection;
    }
}
