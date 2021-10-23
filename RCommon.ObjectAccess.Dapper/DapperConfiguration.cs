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
using DapperSqlMapperExtensions = Dapper.Contrib.Extensions;
using RCommon.Extensions;

namespace RCommon.ObjectAccess.Dapper
{
    public class DapperConfiguration : IObjectAccessConfiguration
    {
        private List<string> _dbContextTypes = new List<string>();

        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance that allows
        /// registering components.</param>
        public void Configure(IContainerAdapter containerAdapter)
        {
            
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

            // Dapper Repository
            containerAdapter.AddGeneric(typeof(ISqlMapperRepository<>), typeof(DapperRepository<>));

            // Registered DbContexts
            foreach (var dbContext in _dbContextTypes)
            {
                containerAdapter.AddTransient(Type.GetType(dbContext), Type.GetType(dbContext));
            }

        }


        public IObjectAccessConfiguration UsingDbConnection<TDbConnection>()
            where TDbConnection : IRDbConnection
        {
            var dbContext = typeof(TDbConnection).AssemblyQualifiedName;
            _dbContextTypes.Add(dbContext);

            return this;
        }
    }
}
