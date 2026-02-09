using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Mapping;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Persistence.Linq2Db.Crud;
using RCommon.Persistence.Crud;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LinqToDB.Extensions.DependencyInjection;

namespace RCommon.Persistence.Linq2Db
{
    /// <summary>
    /// Implementation of <see cref="ILinq2DbPersistenceBuilder"/> that configures Linq2Db-based
    /// persistence services in the dependency injection container.
    /// </summary>
    /// <remarks>
    /// Upon construction, this builder registers <see cref="Linq2DbRepository{TEntity}"/> as the default
    /// implementation for <see cref="IReadOnlyRepository{TEntity}"/>, <see cref="IWriteOnlyRepository{TEntity}"/>,
    /// and <see cref="ILinqRepository{TEntity}"/>.
    /// </remarks>
    public class Linq2DbPersistenceBuilder : ILinq2DbPersistenceBuilder
    {

        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of <see cref="Linq2DbPersistenceBuilder"/> and registers
        /// Linq2Db repository services in the provided service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        public Linq2DbPersistenceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // Linq2Db Repository
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(Linq2DbRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(Linq2DbRepository<>));
            services.AddTransient(typeof(ILinqRepository<>), typeof(Linq2DbRepository<>));
        }

        /// <inheritdoc />
        public IServiceCollection Services => _services;

        /// <summary>
        /// Registers a Linq2Db data connection type with the specified data store name and configuration options.
        /// </summary>
        /// <typeparam name="TDataConnection">The type of the data connection. Must derive from <see cref="RCommonDataConnection"/>.</typeparam>
        /// <param name="dataStoreName">A unique name identifying this data store for resolution via <see cref="IDataStoreFactory"/>.</param>
        /// <param name="options">A factory function that receives the <see cref="IServiceProvider"/> and existing <see cref="DataOptions"/>, returning configured <see cref="DataOptions"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="UnsupportedDataStoreException">Thrown when <paramref name="dataStoreName"/> is null or empty, or when <paramref name="options"/> is <c>null</c>.</exception>
        public ILinq2DbPersistenceBuilder AddDataConnection<TDataConnection>(string dataStoreName, Func<IServiceProvider, DataOptions, DataOptions> options)
            where TDataConnection : RCommonDataConnection
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");
            Guard.Against<UnsupportedDataStoreException>(options == null, "You must set options to a value in order for them to be useful");

            // Register the factory, map the concrete DataConnection type to the data store name, and add the Linq2Db context
            this._services.TryAddTransient<IDataStoreFactory, DataStoreFactory>();
            this._services.Configure<DataStoreFactoryOptions>(options => options.Register<RCommonDataConnection, TDataConnection>(dataStoreName));
            this._services.AddLinqToDBContext<TDataConnection>(options!);
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
