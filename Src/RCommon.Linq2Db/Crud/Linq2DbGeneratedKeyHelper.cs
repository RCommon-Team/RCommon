using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;

namespace RCommon.Persistence.Linq2Db.Crud
{
    /// <summary>
    /// Inserts an entity via Linq2Db and writes a database-generated key (e.g. an auto-increment
    /// <c>int</c> identity column) back onto the entity instance.
    /// </summary>
    /// <remarks>
    /// LinqToDB's plain <c>InsertAsync</c> returns the number of affected rows, not the generated key --
    /// retrieving the generated value requires the separate <c>InsertWithIdentityAsync</c> call, and even
    /// that does not mutate the entity passed to it. Without this helper, an entity with a database-generated
    /// key keeps its default value (e.g. <c>0</c>) after <c>AddAsync</c> returns, even though the row was
    /// inserted correctly.
    /// </remarks>
    internal static class Linq2DbGeneratedKeyHelper
    {
        /// <summary>
        /// Inserts <paramref name="entity"/> via Linq2Db. If the entity's mapping declares an identity
        /// column, uses <c>InsertWithIdentityAsync</c> and assigns the generated value back onto the
        /// entity; otherwise falls back to a plain insert, leaving client-supplied keys untouched.
        /// </summary>
        public static async Task InsertAndAssignGeneratedKeyAsync<TEntity>(DataConnection dataConnection, TEntity entity, CancellationToken token)
            where TEntity : notnull
        {
            var entityDescriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(TEntity));
            var identityColumn = entityDescriptor.Columns.FirstOrDefault(c => c.IsIdentity);

            if (identityColumn == null)
            {
                await dataConnection.InsertAsync(entity, token: token).ConfigureAwait(false);
                return;
            }

            var generatedValue = await dataConnection.InsertWithIdentityAsync(entity, token: token).ConfigureAwait(false);
            if (generatedValue == null)
            {
                return;
            }

            switch (identityColumn.MemberInfo)
            {
                case PropertyInfo propertyInfo:
                    var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                    propertyInfo.SetValue(entity, Convert.ChangeType(generatedValue, propertyType));
                    break;
                case FieldInfo fieldInfo:
                    var fieldType = Nullable.GetUnderlyingType(fieldInfo.FieldType) ?? fieldInfo.FieldType;
                    fieldInfo.SetValue(entity, Convert.ChangeType(generatedValue, fieldType));
                    break;
            }
        }
    }
}
