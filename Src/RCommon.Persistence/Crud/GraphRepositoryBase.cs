
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Persistence.Transactions;

namespace RCommon.Persistence.Crud
{
    ///<summary>
    /// A base class for implementors of <see cref="IGraphRepository{TEntity}"/> that provides
    /// graph-based (change-tracked) repository functionality on top of <see cref="LinqRepositoryBase{TEntity}"/>.
    ///</summary>
    ///<typeparam name="TEntity">The entity type, which must be a class implementing <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// Concrete implementations (e.g., EF Core repositories) should inherit from this class and provide
    /// the actual data access logic including change tracking behavior via the <see cref="Tracking"/> property.
    /// </remarks>
    public abstract class GraphRepositoryBase<TEntity> : LinqRepositoryBase<TEntity>, IGraphRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphRepositoryBase{TEntity}"/> class.
        /// </summary>
        /// <param name="dataStoreFactory">The factory used to resolve named data stores.</param>
        /// <param name="eventTracker">The entity event tracker for publishing domain events.</param>
        /// <param name="defaultDataStoreOptions">Options specifying the default data store name.</param>
        public GraphRepositoryBase(IDataStoreFactory dataStoreFactory,
            IEntityEventTracker eventTracker, IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
            :base(dataStoreFactory, eventTracker, defaultDataStoreOptions)
        {

        }

        /// <inheritdoc />
        public abstract bool Tracking { get; set; }
    }
}
