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
using DapperExtensions.Mapper;

namespace RCommon.Persistence.Dapper
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
            
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

            // Dapper Repository
            this.ContainerAdapter.AddGeneric(typeof(ISqlMapperRepository<>), typeof(DapperRepository<>));

            // Registered DbContexts
            foreach (var dbContext in _dbContextTypes)
            {
                this.ContainerAdapter.AddTransient(Type.GetType(dbContext), Type.GetType(dbContext));
            }

        }


        public IDapperConfiguration UsingDbConnection<TDbConnection>()
            where TDbConnection : IRDbConnection
        {
            var dbContext = typeof(TDbConnection).AssemblyQualifiedName;
            _dbContextTypes.Add(dbContext);

            return this;
        }

        /// <summary>
        /// This class mapper assumes that your database table names are singular (Ex: Car table instead of Cars)
        /// </summary>
        /// <remarks>Follow configuration guidance here: https://github.com/tmsmith/Dapper-Extensions/wiki/AutoClassMapper </remarks>
        /// <returns>Chained Dapper Configuration</returns>
        public IDapperConfiguration WithSingularizedClassMapper()
        {
            DapperExtensions.DapperExtensions.DefaultMapper = typeof(AutoClassMapper<>);
            return this;
        }

        /// <summary>
        /// This class mapper assumes that your database table names are plural (Ex: Cars table instead of Car)
        /// </summary>
        /// <remarks>Follow configuration guidance here: https://github.com/tmsmith/Dapper-Extensions/wiki/AutoClassMapper#pluralizedautoclassmapper </remarks>
        /// <returns>Chained Dapper Configuration</returns>
        public IDapperConfiguration WithPluralizedClassMapper()
        {
            DapperExtensions.DapperExtensions.DefaultMapper = typeof(PluralizedAutoClassMapper<>);
            return this;
        }

        /// <summary>
        /// Allows you to define your own class mapper. Must implement <see cref="IClassMapper"/>.
        /// </summary>
        /// <param name="type">Type of Class Mapper</param>
        /// <remarks>Follow configuration guidance here: https://github.com/tmsmith/Dapper-Extensions/wiki/AutoClassMapper#customized-pluralizedautoclassmapper </remarks>
        /// <returns>Chained Dapper Configuration</returns>
        public IDapperConfiguration WithCustomClassMapper(Type type)
        {
            Guard.Implements<IClassMapper>(type, "The type specified as the Dapper Class Mapper does not implement IClassMapper");

            DapperExtensions.DapperExtensions.DefaultMapper = type;
            return this;
        }
    }
}
