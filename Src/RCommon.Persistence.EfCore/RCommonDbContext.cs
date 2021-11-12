using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    public abstract class RCommonDbContext : DbContext, IDataStore
    {

        public RCommonDbContext()
            : base()
        {
            
        }

        public IDbConnection GetDbConnection()
        {
            return base.Database.GetDbConnection();
        }

        public async Task PersistChangesAsync(CancellationToken cancellationToken)
        {
            await this.SaveChangesAsync(true, cancellationToken);
        }
    }
}
