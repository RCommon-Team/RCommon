using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Crud;

namespace RCommon
{
    /// <summary>
    /// Implementation of <see cref="IEFCorePersistenceBuilder"/> for Entity Framework.
    /// </summary>
    public class EFCorePerisistenceBuilder : IEFCorePersistenceBuilder
    {
        private readonly IServiceCollection _services;

        public EFCorePerisistenceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // EF Core Repository
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(ILinqRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IGraphRepository<>), typeof(EFCoreRepository<>));
        }


        public IEFCorePersistenceBuilder AddDbContext<TDbContext>(string dataStoreName, Action<DbContextOptionsBuilder>? options = null)
            where TDbContext : RCommonDbContext
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");
            
            if (!StaticDataStore.DataStores.TryAdd(dataStoreName, typeof(TDbContext)))
            {
                throw new UnsupportedDataStoreException($"The StaticDataStore refused to add the new DataStore name: {dataStoreName} of type: {typeof(TDbContext).AssemblyQualifiedName}");
            }
            this._services.AddDbContext<TDbContext>(options, ServiceLifetime.Scoped); 

            return this;
        }

        public IPersistenceBuilder SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
