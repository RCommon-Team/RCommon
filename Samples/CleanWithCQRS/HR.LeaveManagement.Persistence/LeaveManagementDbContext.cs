using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RCommon;
using RCommon.BusinessEntities;
using RCommon.Security.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Persistence
{
    public class LeaveManagementDbContext : AuditableDbContext
    {

        public LeaveManagementDbContext(DbContextOptions<LeaveManagementDbContext> options, ICurrentUser currentUser, ISystemTime systemTime, 
            IChangeTracker changeTracker, IMediator mediator)
            : base(options, currentUser, systemTime, changeTracker, mediator)
        {
        }

        public LeaveManagementDbContext(DbContextOptions<LeaveManagementDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeaveManagementDbContext).Assembly);
        }

        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveAllocation> LeaveAllocations { get; set; }
    }
}
