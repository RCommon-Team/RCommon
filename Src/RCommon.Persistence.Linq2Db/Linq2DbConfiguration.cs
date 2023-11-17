using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.AspNet;
using LinqToDB.Mapping;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Persistence.Repositories;

namespace RCommon.Persistence.Linq2Db
{
    public class Linq2DbConfiguration : ILinq2DbConfiguration
    {

        private readonly IServiceCollection _services;

        public Linq2DbConfiguration(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // Linq2Db Repository
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(Linq2DbRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(Linq2DbRepository<>));
            services.AddTransient(typeof(ILinqRepository<>), typeof(Linq2DbRepository<>));
        }


        public ILinq2DbConfiguration AddDataConnection<TDataConnection>(string dataStoreName, Func<IServiceProvider, DataOptions, DataOptions> options)
            where TDataConnection : RCommonDataConnection
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");
            Guard.Against<UnsupportedDataStoreException>(options == null, "You must set options to a value in order for them to be useful");

            if (!StaticDataStore.DataStores.TryAdd(dataStoreName, typeof(TDataConnection)))
            {
                throw new UnsupportedDataStoreException($"The StaticDataStore refused to add the new DataStore name: {dataStoreName} of type: {typeof(TDataConnection).AssemblyQualifiedName}");
            }

            //this._services.Configure(options);
            _services.AddLinqToDBContext<TDataConnection>(options);
            //_services.AddSingleton<DataOptions>(options);
            //_services.AddScoped<TDataConnection>();
            return this;
        }

        public IObjectAccessConfiguration SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
