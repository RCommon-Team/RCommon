using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.Entities;
using RCommon.Core.Threading;
using RCommon.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    public abstract class RCommonDbContext : DbContext, IDataStore
    {

        public RCommonDbContext(DbContextOptions options)
            : base(options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

        }



        public DbConnection GetDbConnection()
        {
            return base.Database.GetDbConnection();
        }
    }
}
