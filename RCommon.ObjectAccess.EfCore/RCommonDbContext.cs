using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore
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
            this.SaveChanges();
        }
    }
}
