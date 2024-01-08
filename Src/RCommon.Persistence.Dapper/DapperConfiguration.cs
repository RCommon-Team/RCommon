using Microsoft.Data.SqlClient;
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

namespace RCommon
{
    public class DapperConfiguration : IDapperConfiguration
    {
        private readonly IServiceCollection _services;
        private List<string> _dbContextTypes = new List<string>();


        public DapperConfiguration(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // Dapper Repository
            services.AddTransient(typeof(ISqlMapperRepository<>), typeof(DapperRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(DapperRepository<>));
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(DapperRepository<>));
            
        }


        public IDapperConfiguration AddDbConnection<TDbConnection>(string dataStoreName, Action<RDbConnectionOptions> options)
            where TDbConnection : IRDbConnection
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");
            Guard.Against<RDbConnectionException>(options == null, "You must configure the options for the RDbConnection for it to be useful");

            if (!StaticDataStore.DataStores.TryAdd(dataStoreName, typeof(TDbConnection)))
            {
                throw new UnsupportedDataStoreException($"The StaticDataStore refused to add the new DataStore name: {dataStoreName} of type: {typeof(TDbConnection).AssemblyQualifiedName}");
            }

            var dbContext = typeof(TDbConnection).AssemblyQualifiedName;
            this._services.AddTransient(Type.GetType(dbContext), Type.GetType(dbContext));
            this._services.Configure(options);

            return this;
        }

        public IPersistenceConfiguration SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
