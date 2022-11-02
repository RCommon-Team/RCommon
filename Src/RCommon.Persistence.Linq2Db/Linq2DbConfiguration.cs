using LinqToDB.Configuration;
using LinqToDB.Mapping;
using Microsoft.Extensions.DependencyInjection;
using RCommon.DataServices;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db
{
    public class Linq2DbConfiguration : ILinq2DbConfiguration
    {

        private readonly IServiceCollection _services;

        public Linq2DbConfiguration(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // EF Core Repository
            //services.AddTransient(typeof(IReadOnlyRepository<>), typeof(EFCoreRepository<>));
            //services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(EFCoreRepository<>));
            //services.AddTransient(typeof(ILinqRepository<>), typeof(EFCoreRepository<>));
            //services.AddTransient(typeof(IEagerFetchingRepository<>), typeof(EFCoreRepository<>));
        }


        public ILinq2DbConfiguration AddDataConnection<TDataConnection>(string dataStoreName, Action<LinqToDBConnectionOptionsBuilder> options)
            where TDataConnection : RCommonDataConnection
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");
            Guard.Against<UnsupportedDataStoreException>(options == null, "You must set options to a value in order for them to be useful");

            if (!StaticDataStore.DataStores.TryAdd(dataStoreName, typeof(TDataConnection)))
            {
                throw new UnsupportedDataStoreException($"The StaticDataStore refused to add the new DataStore name: {dataStoreName} of type: {typeof(TDataConnection).AssemblyQualifiedName}");
            }

            var builder = new LinqToDBConnectionOptionsBuilder();
            options(builder);
            this._services.Configure<LinqToDBConnectionOptions>(x => { builder.Build(); });
            return this;
        }

        public ILinq2DbConfiguration AddFluentMappings(Action<FluentMappingBuilder> options)
        {
            // IMPORTANT: configure mapping schema instance only once
            // and use it with all your connections that need those mappings
            // Never create new mapping schema for each connection as
            // it will seriously harm performance
            var mappingSchema = new MappingSchema();
            var builder = mappingSchema.GetFluentMappingBuilder();
            options(builder);
            return this;
        }

        public IObjectAccessConfiguration SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
