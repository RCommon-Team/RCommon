using HR.LeaveManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Persistence
{
    public abstract class AuditableDbContext : RCommonDbContext
    {
        public AuditableDbContext(DbContextOptions options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var entry in base.ChangeTracker.Entries<BaseDomainEntity>()
                .Where(q => q.State == EntityState.Added || q.State == EntityState.Modified))
            {
                entry.Entity.DateLastModified = DateTime.Now;
                entry.Entity.LastModifiedBy = "System";

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = DateTime.Now;
                    entry.Entity.CreatedBy = "System";
                }
            }

            
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override void PersistChanges()
        {
            base.PersistChanges();
        }
    }
}
