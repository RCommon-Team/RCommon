namespace RCommon.Entities
{
    /// <summary>
    /// Marks an entity as belonging to a specific tenant. When multitenancy is configured,
    /// the repository will automatically set <see cref="TenantId"/> on add operations and
    /// filter read operations to only return entities matching the current tenant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an opt-in capability interface. Entities that do not implement this interface
    /// are not tenant-scoped and will not be filtered by tenant. If no <see cref="RCommon.Persistence.Crud.ITenantIdAccessor"/>
    /// is configured (or the accessor returns <c>null</c>), tenant filtering is bypassed entirely,
    /// allowing the application to operate without multitenancy.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong> To enable multitenancy for an entity, implement this interface and
    /// ensure the underlying data store has a corresponding <c>TenantId</c> column (string).
    /// Configure multitenancy during bootstrapping using <c>.WithMultiTenancy&lt;T&gt;()</c>.
    /// </para>
    /// <example>
    /// <code>
    /// public class Customer : BusinessEntity&lt;int&gt;, IMultiTenant
    /// {
    ///     public string Name { get; set; }
    ///     public string? TenantId { get; set; }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public interface IMultiTenant
    {
        /// <summary>
        /// Gets or sets the identifier of the tenant that owns this entity.
        /// When <c>null</c>, the entity is not associated with any tenant.
        /// </summary>
        string? TenantId { get; set; }
    }
}
