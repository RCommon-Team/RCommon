using RCommon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// Extends <see cref="IQueryable{T}"/> and <see cref="IReadOnlyRepository{TEntity}"/> with support
    /// for chained eager loading of related navigation properties.
    /// </summary>
    /// <typeparam name="TEntity">The entity type, which must implement <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// This interface enables fluent <c>Include(...).ThenInclude(...)</c> chains similar to Entity Framework Core.
    /// </remarks>
    public interface IEagerLoadableQueryable<TEntity> : IQueryable<TEntity>, IReadOnlyRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        /// <summary>
        /// Eagerly loads a nested navigation property following a prior <c>Include</c> call.
        /// </summary>
        /// <typeparam name="TPreviousProperty">The type of the previously included property.</typeparam>
        /// <typeparam name="TProperty">The type of the nested property to include.</typeparam>
        /// <param name="path">An expression specifying the nested navigation property to include.</param>
        /// <returns>An <see cref="IEagerLoadableQueryable{TEntity}"/> for further chaining.</returns>
        IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path);
    }
}
