using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Dommel;

namespace RCommon.Persistence.Dapper.Crud
{
    /// <summary>
    /// Wraps Dommel's default <see cref="IKeyPropertyResolver"/> so that only numeric key properties
    /// (the types a database identity/serial column can actually generate) are treated as
    /// database-generated. Non-numeric keys (e.g. <see cref="Guid"/> or <see cref="string"/>) are
    /// corrected to <see cref="DatabaseGeneratedOption.None"/> unless the property already carries an
    /// explicit <see cref="DatabaseGeneratedAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Dommel's default resolver treats any single property named "Id" (or "{TypeName}Id") as
    /// database-generated, regardless of its type. For a numeric key this happens to be a reasonable
    /// default (matches auto-increment/identity columns), but for a <see cref="Guid"/> or
    /// <see cref="string"/> key -- both fully supported by <c>BusinessEntity&lt;TKey&gt;</c> and used
    /// throughout RCommon's own docs and examples -- it means Dommel excludes the key column from the
    /// generated INSERT statement entirely, silently writing a <c>NULL</c> (or provider-default) value
    /// for the primary key instead of the value the caller set. This resolver corrects that for the
    /// common RCommon case without requiring every consumer entity to be individually annotated.
    /// </remarks>
    internal sealed class RCommonKeyPropertyResolver : IKeyPropertyResolver
    {
        private static readonly HashSet<Type> IdentityCompatibleTypes = new()
        {
            typeof(int), typeof(long), typeof(short), typeof(byte),
            typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte),
            typeof(decimal)
        };

        private readonly IKeyPropertyResolver _inner;

        public RCommonKeyPropertyResolver(IKeyPropertyResolver inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public ColumnPropertyInfo[] ResolveKeyProperties(Type type)
        {
            var keyProperties = _inner.ResolveKeyProperties(type);

            for (var i = 0; i < keyProperties.Length; i++)
            {
                var keyProperty = keyProperties[i];
                if (!keyProperty.IsGenerated)
                {
                    continue;
                }

                var propertyType = Nullable.GetUnderlyingType(keyProperty.Property.PropertyType) ?? keyProperty.Property.PropertyType;
                if (IdentityCompatibleTypes.Contains(propertyType))
                {
                    continue;
                }

                // Respect an explicit [DatabaseGenerated] attribute -- the consumer deliberately opted in.
                if (keyProperty.Property.GetCustomAttributes(typeof(DatabaseGeneratedAttribute), inherit: true).Any())
                {
                    continue;
                }

                keyProperties[i] = new ColumnPropertyInfo(keyProperty.Property, DatabaseGeneratedOption.None);
            }

            return keyProperties;
        }
    }
}
