using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;
using Microsoft.Extensions.Logging;
using RCommon.Persistence.Sql;
using RCommon.Entities;
using RCommon.Security.Claims;
using System.Threading;
using RCommon.Collections;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Transactions;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// Abstract base class for SQL-mapped (micro-ORM) repositories that provides common infrastructure
    /// for data store resolution, event tracking, and CRUD operations for entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type, which must be a class implementing <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// Concrete implementations (e.g., Dapper repositories) should inherit from this class and provide
    /// the actual SQL-based data access logic. Uses <see cref="RDbConnection"/> for database connectivity.
    /// </remarks>
    public abstract class SqlRepositoryBase<TEntity> : DisposableResource, ISqlMapperRepository<TEntity>
       where TEntity : class, IBusinessEntity
    {
        private string _dataStoreName = default!;
        private readonly IDataStoreFactory _dataStoreFactory;
        protected readonly ITenantIdAccessor _tenantIdAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlRepositoryBase{TEntity}"/> class.
        /// </summary>
        /// <param name="dataStoreFactory">The factory used to resolve named data stores.</param>
        /// <param name="logger">The logger factory for creating loggers.</param>
        /// <param name="eventTracker">The entity event tracker for publishing domain events.</param>
        /// <param name="defaultDataStoreOptions">Options specifying the default data store name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any of the required parameters is <c>null</c>.
        /// </exception>
        public SqlRepositoryBase(IDataStoreFactory dataStoreFactory,
            ILoggerFactory logger, IEntityEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
            ITenantIdAccessor tenantIdAccessor)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (defaultDataStoreOptions is null)
            {
                throw new ArgumentNullException(nameof(defaultDataStoreOptions));
            }

            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
            EventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
            _tenantIdAccessor = tenantIdAccessor ?? throw new ArgumentNullException(nameof(tenantIdAccessor));

            // Apply default data store name if configured, so repositories work without explicit name assignment
            if (defaultDataStoreOptions != null && defaultDataStoreOptions.Value != null
                && !defaultDataStoreOptions.Value.DefaultDataStoreName.IsNullOrEmpty())
            {
                this.DataStoreName = defaultDataStoreOptions.Value.DefaultDataStoreName;
            }
        }

        /// <inheritdoc />
        public string TableName { get; set; } = default!;

        /// <inheritdoc />
        public abstract Task AddAsync(TEntity entity, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task DeleteAsync(TEntity entity, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task DeleteAsync(TEntity entity, bool isSoftDelete, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(ISpecification<TEntity> specification, bool isSoftDelete, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task UpdateAsync(TEntity entity, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <summary>
        /// Gets the resolved <see cref="RDbConnection"/> data store for this repository,
        /// using the current <see cref="DataStoreName"/> to look up the connection from the factory.
        /// </summary>
        protected internal RDbConnection DataStore
        {
            get
            {
                return this._dataStoreFactory.Resolve<RDbConnection>(this.DataStoreName);
            }
        }

        /// <inheritdoc />
        public string DataStoreName
        {
            get => _dataStoreName;
            set
            {
                _dataStoreName = value;
            }
        }

        /// <summary>
        /// Gets or sets the logger instance for this repository.
        /// </summary>
        public ILogger Logger { get; set; } = default!;

        /// <summary>
        /// Gets the entity event tracker used to track and publish domain events raised by entities.
        /// </summary>
        public IEntityEventTracker EventTracker { get; }
    }

}
