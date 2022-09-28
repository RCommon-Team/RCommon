using Microsoft.Data.SqlClient;
using RCommon.Configuration;
using RCommon.DataServices.Sql;
using RCommon.DependencyInjection;
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
    public class DapperConfiguration : RCommonConfiguration, IDapperConfiguration
    {
        private List<string> _dbContextTypes = new List<string>();


        public DapperConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

        }


        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance that allows
        /// registering components.</param>
        public override void Configure()
        {
            // Dapper Repository
            this.ContainerAdapter.AddGeneric(typeof(ISqlMapperRepository<>), typeof(DapperRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IWriteOnlyRepository<>), typeof(DapperRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IReadOnlyRepository<>), typeof(DapperRepository<>));

        }


        public IDapperConfiguration AddDbConnection<TDbConnection>(Action<RDbConnectionOptions> options)
            where TDbConnection : IRDbConnection
        {
            Guard.Against<RDbConnectionException>(options == null, "You must configure the options for the RDbConnection for it to be useful");
            var dbContext = typeof(TDbConnection).AssemblyQualifiedName;
            this.ContainerAdapter.AddTransient(Type.GetType(dbContext), Type.GetType(dbContext));
            this.ContainerAdapter.Services.Configure(options);

            // See: https://blog.bitscry.com/2020/06/01/creating-a-dbconnectionfactory/
            // We don't need to register the Db factory but leaving this just in case.
            // var settings = new RDbConnectionOptions();
            //options(settings);
            //DbProviderFactories.RegisterFactory(settings.Name, settings.DbFactory);

            return this;
        }

        public IObjectAccessConfiguration SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this.ContainerAdapter.Services.Configure(options);
            return this;
        }
    }
}
