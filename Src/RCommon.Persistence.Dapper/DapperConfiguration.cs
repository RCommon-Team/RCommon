using Microsoft.Data.SqlClient;
using RCommon.DataServices.Sql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Extensions;
using RCommon.Persistence;
using RCommon.Persistence.Dapper;
using Microsoft.Extensions.DependencyInjection;

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


        public IDapperConfiguration AddDbConnection<TDbConnection>(Action<RDbConnectionOptions> options)
            where TDbConnection : IRDbConnection
        {
            Guard.Against<RDbConnectionException>(options == null, "You must configure the options for the RDbConnection for it to be useful");
            var dbContext = typeof(TDbConnection).AssemblyQualifiedName;
            this._services.AddTransient(Type.GetType(dbContext), Type.GetType(dbContext));
            this._services.Configure(options);

            return this;
        }

        public IObjectAccessConfiguration SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
