using RCommon.Configuration;
using RCommon.DependencyInjection;
using RCommon.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // EF Core Repository
            containerAdapter.AddGeneric(typeof(IAsyncCrudRepository<>), typeof(DapperRepository<>));

            // Registered DbContexts
            foreach (var dbContext in _dbContextTypes)
            {
                containerAdapter.AddTransient(Type.GetType(dbContext), Type.GetType(dbContext));
            }


        }


        public IObjectAccessConfiguration UsingDbContext<TDbContext>()
            where TDbContext : RCommonDbContext
        {
            var dbContext = typeof(TDbContext).AssemblyQualifiedName;
            _dbContextTypes.Add(dbContext);

            //_dbContextType = Activator.CreateInstance(typeof(TDbContext));

            return this;
        }
    }
}
