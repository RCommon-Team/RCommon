using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.Core.Threading;
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

        public void PersistChanges()
        {
            AsyncHelper.RunSync(() => this.SaveChangesAsync(true));
        }


        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
