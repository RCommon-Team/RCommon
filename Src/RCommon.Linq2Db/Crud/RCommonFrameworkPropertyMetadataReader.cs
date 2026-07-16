using System;
using System.Reflection;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using RCommon.Entities;

namespace RCommon.Persistence.Linq2Db.Crud
{
    /// <summary>
    /// Tells LinqToDB's mapping schema to exclude the framework-internal properties declared on
    /// <see cref="BusinessEntity"/> and <see cref="AggregateRoot{TKey}"/> from column mapping.
    /// </summary>
    /// <remarks>
    /// Unlike EF Core, LinqToDB does not recognize
    /// <see cref="System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute"/> -- the attribute
    /// <see cref="BusinessEntity"/> already carries on <c>LocalEvents</c> and <c>AllowEventTracking</c>
    /// (and <see cref="AggregateRoot{TKey}"/> on <c>DomainEvents</c>) means nothing to it. Without this
    /// reader, LinqToDB maps every public property to a column by default, so any entity deriving from
    /// <c>BusinessEntity</c>/<c>BusinessEntity&lt;TKey&gt;</c> -- RCommon's own recommended base class --
    /// fails every real INSERT/SELECT against Linq2Db with a "no such column" error, since no real schema
    /// has columns for these bookkeeping properties. This reader injects a <see cref="NotColumnAttribute"/>
    /// for exactly those properties, matched by declaring type so it applies to every derived entity
    /// automatically without per-entity configuration.
    /// </remarks>
    internal sealed class RCommonFrameworkPropertyMetadataReader : IMetadataReader
    {
        private static readonly MappingAttribute[] NotColumn = { new NotColumnAttribute() };
        private static readonly MappingAttribute[] None = Array.Empty<MappingAttribute>();

        public MappingAttribute[] GetAttributes(Type type) => None;

        public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
        {
            return IsFrameworkProperty(memberInfo) ? NotColumn : None;
        }

        public MemberInfo[] GetDynamicColumns(Type type) => Array.Empty<MemberInfo>();

        public string GetObjectID() => ".RCommonFrameworkPropertyMetadataReader.";

        private static bool IsFrameworkProperty(MemberInfo memberInfo)
        {
            if (memberInfo.DeclaringType == typeof(BusinessEntity))
            {
                return memberInfo.Name == nameof(BusinessEntity.LocalEvents)
                    || memberInfo.Name == nameof(BusinessEntity.AllowEventTracking);
            }

            if (memberInfo.Name == "DomainEvents"
                && memberInfo.DeclaringType is { IsGenericType: true } declaringType
                && declaringType.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }

            return false;
        }
    }
}
