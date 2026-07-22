using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using RCommon.Entities;
using RCommon.Core.Threading;
using RCommon.Persistence;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    /// <summary>
    /// Abstract base class for all EF Core DbContexts used within the RCommon persistence layer.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IDataStore"/> to provide a uniform abstraction over data stores,
    /// allowing the <see cref="IDataStoreFactory"/> to resolve named DbContext instances.
    /// Derive from this class instead of <see cref="DbContext"/> directly when using RCommon.
    /// </remarks>
    public abstract class RCommonDbContext : DbContext, IDataStore
    {

        /// <summary>
        /// Initializes a new instance of <see cref="RCommonDbContext"/> with the specified options.
        /// </summary>
        /// <param name="options">The <see cref="DbContextOptions"/> used to configure this context.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
        private readonly DbContextOptions _options;

        public RCommonDbContext(DbContextOptions options)
            : base(options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
        }



        /// <summary>
        /// Configures the model. When this context's datastore is registered as owning an outbox
        /// (via <see cref="Outbox.DbContextOptionsBuilderExtensions.UseOutboxDataStore"/> and present in
        /// <see cref="IOutboxDataStoreRegistry.Registrations"/>), the <c>OutboxMessage</c> entity is mapped
        /// automatically. The mapping is idempotent: if a derived context also calls
        /// <c>modelBuilder.AddOutboxMessages()</c> manually, the entity is not mapped twice.
        /// </summary>
        /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var extension = _options.FindExtension<OutboxDataStoreOptionsExtension>();

            if (extension is not null
                && extension.OwnsOutbox()
                && modelBuilder.Model.FindEntityType(typeof(OutboxMessage)) is null)
            {
                modelBuilder.AddOutboxMessages();
            }
        }

        /// <summary>
        /// Gets the underlying <see cref="DbConnection"/> for this context.
        /// </summary>
        /// <returns>The <see cref="DbConnection"/> associated with the current database.</returns>
        public DbConnection GetDbConnection()
        {
            return base.Database.GetDbConnection();
        }
    }
}
