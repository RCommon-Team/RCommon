using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Crud;
using RCommon.Security.Claims;

namespace RCommon
{
    /// <summary>
    /// Implementation of <see cref="IEFCorePersistenceBuilder"/> that configures Entity Framework Core
    /// persistence services in the dependency injection container.
    /// </summary>
    /// <remarks>
    /// Upon construction, this builder registers <see cref="EFCoreRepository{TEntity}"/> as the default
    /// implementation for <see cref="IReadOnlyRepository{TEntity}"/>, <see cref="IWriteOnlyRepository{TEntity}"/>,
    /// <see cref="ILinqRepository{TEntity}"/>, and <see cref="IGraphRepository{TEntity}"/>.
    /// </remarks>
    public class EFCorePerisistenceBuilder : IEFCorePersistenceBuilder
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of <see cref="EFCorePerisistenceBuilder"/> and registers
        /// EF Core repository services in the provided service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services with.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        public EFCorePerisistenceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // Default tenant accessor (no-op); overridden when multitenancy is configured
            services.TryAddTransient<ITenantIdAccessor, NullTenantIdAccessor>();

            // EF Core Repository
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(ILinqRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IGraphRepository<>), typeof(EFCoreRepository<>));
        }

        /// <inheritdoc />
        public IServiceCollection Services => _services;

        /// <summary>
        /// Registers a <see cref="RCommonDbContext"/>-derived DbContext with the specified data store name and options.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the DbContext to register. Must derive from <see cref="RCommonDbContext"/>.</typeparam>
        /// <param name="dataStoreName">A unique name identifying this data store for resolution via <see cref="IDataStoreFactory"/>.</param>
        /// <param name="options">An optional action to configure the <see cref="DbContextOptionsBuilder"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="UnsupportedDataStoreException">Thrown when <paramref name="dataStoreName"/> is null or empty.</exception>
        public IEFCorePersistenceBuilder AddDbContext<TDbContext>(string dataStoreName, Action<DbContextOptionsBuilder>? options = null)
            where TDbContext : RCommonDbContext
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");

            // Register the factory, map the concrete DbContext type to the data store name, and add the DbContext with scoped lifetime
            _services.TryAddTransient<IDataStoreFactory, DataStoreFactory>();
            _services.Configure<DataStoreFactoryOptions>(options => options.Register<RCommonDbContext, TDbContext>(dataStoreName));
            _services.AddDbContext<TDbContext>(options, ServiceLifetime.Scoped);

            return this;
        }

        /// <summary>
        /// Sets the default data store used when no explicit data store name is specified.
        /// </summary>
        /// <param name="options">An action to configure <see cref="DefaultDataStoreOptions"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public IPersistenceBuilder SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            _services.Configure(options);
            return this;
        }
    }
}
