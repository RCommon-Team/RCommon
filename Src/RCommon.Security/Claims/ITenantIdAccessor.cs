namespace RCommon.Security.Claims
{
    /// <summary>
    /// Provides access to the current tenant identifier at runtime. Repository implementations
    /// use this to automatically filter queries and stamp new entities with the current tenant.
    /// </summary>
    /// <remarks>
    /// The default implementation (<see cref="NullTenantIdAccessor"/>) returns <c>null</c>,
    /// which causes all tenant filtering to be bypassed. Concrete implementations (e.g.,
    /// <see cref="ClaimsTenantIdAccessor"/>, Finbuckle) resolve the tenant from the current
    /// request context.
    /// </remarks>
    public interface ITenantIdAccessor
    {
        /// <summary>
        /// Gets the current tenant identifier, or <c>null</c> if no tenant context is available.
        /// When <c>null</c> or empty, tenant filtering is bypassed entirely.
        /// </summary>
        string? GetTenantId();
    }
}
