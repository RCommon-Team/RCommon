using RCommon.Entities;
using RCommon.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// A repository that supports graph-based (change-tracked) operations on entities of type <typeparamref name="TEntity"/>,
    /// extending <see cref="ILinqRepository{TEntity}"/> with change tracking control.
    /// </summary>
    /// <typeparam name="TEntity">The entity type, which must be a class implementing <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// Typically used with ORM providers like Entity Framework Core that support automatic change tracking.
    /// </remarks>
    public interface IGraphRepository<TEntity> : ILinqRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        /// <summary>
        /// Gets or sets whether entity change tracking is enabled for this repository.
        /// </summary>
        /// <remarks>
        /// When set to <c>false</c>, queries return detached entities for better read performance.
        /// </remarks>
        public bool Tracking { get; set; }
    }
}
