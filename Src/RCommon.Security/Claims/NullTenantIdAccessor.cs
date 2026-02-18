namespace RCommon.Security.Claims
{
    /// <summary>
    /// Default implementation of <see cref="ITenantIdAccessor"/> that always returns <c>null</c>.
    /// Registered automatically when persistence is configured. When multitenancy is not enabled,
    /// this ensures all tenant-related filtering is bypassed without requiring conditional logic.
    /// </summary>
    public class NullTenantIdAccessor : ITenantIdAccessor
    {
        /// <inheritdoc />
        public string? GetTenantId() => null;
    }
}
