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
    public class DapperPersistenceBuilder : IDapperBuilder
    {
        private readonly IServiceCollection _services;
        private List<string> _dbContextTypes = new List<string>();


        public DapperPersistenceBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // Dapper Repository
            services.AddTransient(typeof(ISqlMapperRepository<>), typeof(DapperRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(DapperRepository<>));
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(DapperRepository<>));
            
        }

        public IServiceCollection Services => _services;

        public IDapperBuilder AddDbConnection<TDbConnection>(string dataStoreName, Action<RDbConnectionOptions> options)
            where TDbConnection : RDbConnection
        {
            Guard.Against<UnsupportedDataStoreException>(dataStoreName.IsNullOrEmpty(), "You must set a name for the Data Store");
            Guard.Against<RDbConnectionException>(options == null, "You must configure the options for the RDbConnection for it to be useful");

            var dbContext = typeof(TDbConnection).AssemblyQualifiedName;

            this._services.TryAddTransient<IDataStoreFactory, DataStoreFactory>();
            this._services.TryAddTransient(Type.GetType(dbContext));
            this._services.Configure<DataStoreFactoryOptions>(options => options.Register<RDbConnection, TDbConnection>(dataStoreName));
            this._services.Configure(options);

            return this;
        }

        public IPersistenceBuilder SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
