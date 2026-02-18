using RCommon.Entities;
using RCommon.Linq;
using System;
using System.Linq.Expressions;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// Provides shared validation and operations for multitenancy functionality across repository implementations.
    /// </summary>
    /// <remarks>
    /// Multitenancy requires that the entity type implements <see cref="IMultiTenant"/>.
    /// If the entity does not implement this interface, all tenant operations are no-ops.
    /// When the current tenant ID is <c>null</c> or empty, filtering is bypassed entirely,
    /// allowing the application to operate without multitenancy configured.
    /// </remarks>
    public static class MultiTenantHelper
    {
        /// <summary>
        /// Returns <c>true</c> if the entity type <typeparamref name="TEntity"/> implements <see cref="IMultiTenant"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to check.</typeparam>
        /// <returns><c>true</c> if <typeparamref name="TEntity"/> implements <see cref="IMultiTenant"/>; otherwise <c>false</c>.</returns>
        public static bool IsMultiTenant<TEntity>()
        {
            return typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity));
        }

        /// <summary>
        /// Validates that the entity type <typeparamref name="TEntity"/> implements <see cref="IMultiTenant"/>.
        /// Call this at the start of any tenant-specific code path to fail fast with a clear error message.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to validate.</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <typeparamref name="TEntity"/> does not implement <see cref="IMultiTenant"/>.
        /// </exception>
        public static void EnsureMultiTenant<TEntity>()
        {
            if (!typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
            {
                throw new InvalidOperationException(
                    $"Entity type '{typeof(TEntity).Name}' does not implement IMultiTenant. " +
                    $"Multitenancy is only supported for entities that implement the IMultiTenant interface.");
            }
        }

        /// <summary>
        /// Sets the <see cref="IMultiTenant.TenantId"/> on the entity if it implements <see cref="IMultiTenant"/>
        /// and the provided <paramref name="tenantId"/> is not <c>null</c> or empty.
        /// </summary>
        /// <param name="entity">The entity to stamp with a tenant ID.</param>
        /// <param name="tenantId">The current tenant ID, or <c>null</c> if no tenant context is available.</param>
        public static void SetTenantIdIfApplicable(object entity, string? tenantId)
        {
            if (entity is IMultiTenant multiTenantEntity && !string.IsNullOrEmpty(tenantId))
            {
                multiTenantEntity.TenantId = tenantId;
            }
        }

        /// <summary>
        /// Returns an expression that filters entities by the specified tenant: <c>e =&gt; e.TenantId == tenantId</c>.
        /// Only call this when <see cref="IsMultiTenant{TEntity}"/> returns <c>true</c>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type, which must implement <see cref="IMultiTenant"/>.</typeparam>
        /// <param name="tenantId">The tenant ID to filter by.</param>
        /// <returns>An expression representing <c>e =&gt; e.TenantId == tenantId</c>.</returns>
        public static Expression<Func<TEntity, bool>> GetTenantFilter<TEntity>(string tenantId)
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(param, nameof(IMultiTenant.TenantId));
            var constant = Expression.Constant(tenantId, typeof(string));
            var equals = Expression.Equal(property, constant);
            return Expression.Lambda<Func<TEntity, bool>>(equals, param);
        }

        /// <summary>
        /// Combines the given expression with a tenant filter using a logical AND.
        /// If <typeparamref name="TEntity"/> does not implement <see cref="IMultiTenant"/>
        /// or <paramref name="tenantId"/> is <c>null</c> or empty, the original expression is returned unchanged.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to filter.</typeparam>
        /// <param name="expression">The user-supplied filter expression.</param>
        /// <param name="tenantId">The current tenant ID, or <c>null</c> if no tenant context is available.</param>
        /// <returns>
        /// The original expression AND-combined with the tenant filter when applicable;
        /// otherwise the original expression unchanged.
        /// </returns>
        public static Expression<Func<TEntity, bool>> CombineWithTenantFilter<TEntity>(
            Expression<Func<TEntity, bool>> expression, string? tenantId)
        {
            if (!IsMultiTenant<TEntity>() || string.IsNullOrEmpty(tenantId))
                return expression;

            return expression.And(GetTenantFilter<TEntity>(tenantId));
        }
    }
}
