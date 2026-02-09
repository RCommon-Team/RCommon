using RCommon.Persistence.Sql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Persistence;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence.Dapper.Crud;
using RCommon.Persistence.Crud;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RCommon
{
    /// <summary>
    /// Implementation of <see cref="IDapperBuilder"/> that configures Dapper-based
    /// persistence services in the dependency injection container.
    /// </summary>
    /// <remarks>
    /// Upon construction, this builder registers <see cref="DapperRepository{TEntity}"/> as the default
    /// implementation for <see cref="ISqlMapperRepository{TEntity}"/>, <see cref="IWriteOnlyRepository{TEntity}"/>,
    /// and <see cref="IReadOnlyRepository{TEntity}"/>.
    /// </remarks>
    public class DapperPersistenceBuilder : IDapperBuilder
    {
        private readonly IServiceCollection _services;
        private List<string> _dbContextTypes = new List<string>();


        /// <summary>
        /// Initializes a new instance of <see cref="DapperPersistenceBuilder"/> and registers
        /// Dapper repository services in the provided service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        public DapperPersistenceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // Dapper Repository
            services.AddTransient(typeof(ISqlMapperRepository<>), typeof(DapperRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(DapperRepository<>));
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(DapperRepository<>));

        }

        /// <inheritdoc />
        public IServiceCollection Services => _services;

        /// <summary>
        /// Registers a database connection type with the specified data store name and connection options.
        /// </summary>
        /// <typeparam name="TDbConnection">The type of the database connection. Must derive from <see cref="RDbConnection"/>.</typeparam>
        /// <param name="dataStoreName">A unique name identifying this data store for resolution via <see cref="IDataStoreFactory"/>.</param>
        /// <param name="options">An action to configure the <see cref="RDbConnectionOptions"/> (e.g., connection string).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="UnsupportedDataStoreException">Thrown when <paramref name="dataStoreName"/> is null or empty.</exception>
        /// <exception cref="RDbConnectionException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
        public IDapperBuilder AddDbConnection<TDbConnection>(string dataStoreName, Action<RDbConnectionOptions> options)
            where TDbConnection : RDbConnection
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");
            Guard.Against<RDbConnectionException>(options == null, "You must configure the options for the RDbConnection for it to be useful");

            // Resolve the assembly-qualified type name to register the concrete connection type
            var dbContext = typeof(TDbConnection).AssemblyQualifiedName!;

            this._services.TryAddTransient<IDataStoreFactory, DataStoreFactory>();
            this._services.TryAddTransient(Type.GetType(dbContext)!);
            this._services.Configure<DataStoreFactoryOptions>(o => o.Register<RDbConnection, TDbConnection>(dataStoreName));
            this._services.Configure(options!);

            return this;
        }

        /// <summary>
        /// Sets the default data store used when no explicit data store name is specified.
        /// </summary>
        /// <param name="options">An action to configure <see cref="DefaultDataStoreOptions"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public IPersistenceBuilder SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
