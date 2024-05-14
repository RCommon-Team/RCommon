using HR.LeaveManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;
using RCommon;
using RCommon.Entities;
using RCommon.Persistence.EFCore;
using RCommon.Security.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Persistence
{
    public abstract class AuditableDbContext : RCommonDbContext
    {
        private readonly ICurrentUser _currentUser;
        private readonly ISystemTime _systemTime;

        public AuditableDbContext(DbContextOptions options, ICurrentUser currentUser, ISystemTime systemTime, 
            IEntityEventTracker eventTracker) 
            : base(options, eventTracker)
        {
            _currentUser = currentUser;
            this._systemTime = systemTime;
        }

        public AuditableDbContext(DbContextOptions options)
            : base(options)
        {

        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var entry in base.ChangeTracker.Entries<BaseDomainEntity>()
                .Where(q => q.State == EntityState.Added || q.State == EntityState.Modified))
            {
                string userId = (_currentUser == null || _currentUser.Id == null ? "System" : _currentUser.Id.ToString());

                entry.Entity.DateLastModified = _systemTime.Now;
                entry.Entity.LastModifiedBy = userId;

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = _systemTime.Now;
                    entry.Entity.CreatedBy = userId;
                }
            }

            
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override async Task PersistChangesAsync()
        {
            await base.PersistChangesAsync();
        }
    }
}
