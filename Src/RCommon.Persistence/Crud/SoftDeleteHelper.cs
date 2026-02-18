using RCommon.Entities;
using RCommon.Linq;
using System;
using System.Linq.Expressions;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// Provides shared validation and operations for soft-delete functionality across repository implementations.
    /// </summary>
    /// <remarks>
    /// Soft delete requires that the entity type implements <see cref="ISoftDelete"/>.
    /// If the entity does not implement this interface and soft delete is requested,
    /// an <see cref="InvalidOperationException"/> is thrown. This ensures that callers
    /// cannot accidentally soft-delete an entity that does not have an <c>IsDeleted</c> column.
    /// </remarks>
    public static class SoftDeleteHelper
    {
        /// <summary>
        /// Returns <c>true</c> if the entity type <typeparamref name="TEntity"/> implements <see cref="ISoftDelete"/>.
        /// Used by repository delete methods to automatically choose soft delete when the entity supports it.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to check.</typeparam>
        /// <returns><c>true</c> if <typeparamref name="TEntity"/> implements <see cref="ISoftDelete"/>; otherwise <c>false</c>.</returns>
        public static bool IsSoftDeletable<TEntity>()
        {
            return typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity));
        }

        /// <summary>
        /// Validates that the entity type <typeparamref name="TEntity"/> implements <see cref="ISoftDelete"/>.
        /// Call this at the start of any soft-delete code path to fail fast with a clear error message.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to validate.</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <typeparamref name="TEntity"/> does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public static void EnsureSoftDeletable<TEntity>()
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                throw new InvalidOperationException(
                    $"Entity type '{typeof(TEntity).Name}' does not implement ISoftDelete. " +
                    $"Soft delete is only supported for entities that implement the ISoftDelete interface.");
            }
        }

        /// <summary>
        /// Marks the entity as soft-deleted by setting <see cref="ISoftDelete.IsDeleted"/> to <c>true</c>.
        /// The caller must have already validated that the entity implements <see cref="ISoftDelete"/>
        /// by calling <see cref="EnsureSoftDeletable{TEntity}"/> beforehand.
        /// </summary>
        /// <param name="entity">The entity to mark as deleted. Must implement <see cref="ISoftDelete"/>.</param>
        public static void MarkAsDeleted(object entity)
        {
            ((ISoftDelete)entity).IsDeleted = true;
        }

        /// <summary>
        /// Returns an expression that filters out soft-deleted entities: <c>e =&gt; !e.IsDeleted</c>.
        /// Only call this when <see cref="IsSoftDeletable{TEntity}"/> returns <c>true</c>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type, which must implement <see cref="ISoftDelete"/>.</typeparam>
        /// <returns>An expression representing <c>e =&gt; !e.IsDeleted</c>.</returns>
        public static Expression<Func<TEntity, bool>> GetNotDeletedFilter<TEntity>()
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Not(property);
            return Expression.Lambda<Func<TEntity, bool>>(notDeleted, param);
        }

        /// <summary>
        /// Combines the given expression with a <c>!IsDeleted</c> filter using a logical AND.
        /// If <typeparamref name="TEntity"/> does not implement <see cref="ISoftDelete"/>,
        /// the original expression is returned unchanged.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to filter.</typeparam>
        /// <param name="expression">The user-supplied filter expression.</param>
        /// <returns>
        /// The original expression AND-combined with <c>!IsDeleted</c> when the entity
        /// implements <see cref="ISoftDelete"/>; otherwise the original expression unchanged.
        /// </returns>
        public static Expression<Func<TEntity, bool>> CombineWithNotDeletedFilter<TEntity>(
            Expression<Func<TEntity, bool>> expression)
        {
            if (!IsSoftDeletable<TEntity>())
                return expression;

            return expression.And(GetNotDeletedFilter<TEntity>());
        }
    }
}
