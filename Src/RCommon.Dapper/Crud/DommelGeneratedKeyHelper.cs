using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dommel;

namespace RCommon.Persistence.Dapper.Crud
{
    /// <summary>
    /// Wraps Dommel's <see cref="DommelMapper.InsertAsync{TEntity}"/> so that a database-generated key
    /// (e.g. an auto-increment <c>int</c> identity column) is written back onto the entity instance.
    /// </summary>
    /// <remarks>
    /// Dommel's <c>InsertAsync</c> returns the generated key as <c>Task&lt;object&gt;</c> but does not
    /// mutate the entity passed to it. Without this helper, an entity with a database-generated key keeps
    /// its default value (e.g. <c>0</c>) after <c>AddAsync</c> returns, even though the row was inserted
    /// correctly -- there is no exception or warning, since Dommel has no reason to suspect the caller
    /// wanted the value back.
    /// </remarks>
    internal static class DommelGeneratedKeyHelper
    {
        /// <summary>
        /// Inserts <paramref name="entity"/> via Dommel and assigns the database-generated key (if any)
        /// back onto the entity. Entities with a client-supplied key (e.g. a <see cref="Guid"/>) are
        /// inserted unchanged -- Dommel does not report a generated value for them, so no assignment occurs.
        /// </summary>
        public static async Task InsertAndAssignGeneratedKeyAsync<TEntity>(IDbConnection connection, TEntity entity, CancellationToken token)
            where TEntity : class
        {
            var generatedValue = await connection.InsertAsync(entity, cancellationToken: token).ConfigureAwait(false);
            AssignGeneratedKey(entity, generatedValue);
        }

        private static void AssignGeneratedKey<TEntity>(TEntity entity, object? generatedValue)
            where TEntity : class
        {
            if (generatedValue == null)
            {
                return;
            }

            var generatedKeyProperty = Resolvers.KeyProperties(typeof(TEntity)).FirstOrDefault(k => k.IsGenerated);
            if (generatedKeyProperty == null)
            {
                return;
            }

            var targetType = Nullable.GetUnderlyingType(generatedKeyProperty.Property.PropertyType) ?? generatedKeyProperty.Property.PropertyType;
            var convertedValue = Convert.ChangeType(generatedValue, targetType);
            generatedKeyProperty.Property.SetValue(entity, convertedValue);
        }
    }
}
