using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
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



        public void PersistChanges()
        {
            this.SaveChanges();
        }

        public async Task PersistChangesAsync()
        {
            await this.SaveChangesAsync();
        }
    }
}
