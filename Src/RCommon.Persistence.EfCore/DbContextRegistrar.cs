using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    public class DbContextRegistrar
    {

        public DbContextRegistrar()
        {
            this.RegisteredDataSources = new Dictionary<string, string>();
        }

        public void AddDataSource<TDbContext>(string dataSourceName, TDbContext dbContext)
            where TDbContext : RCommonDbContext
        {
            Guard.IsNotNull(dataSourceName, nameof(dataSourceName));
            Guard.IsNotNull(dbContext, nameof(dbContext));

            this.RegisteredDataSources.Add(dataSourceName, dbContext.GetType().AssemblyQualifiedName);
        }

        public Dictionary<string, string> RegisteredDataSources { get;}
    }
}
