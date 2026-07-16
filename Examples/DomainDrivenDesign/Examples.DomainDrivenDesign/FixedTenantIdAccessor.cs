using RCommon.Security.Claims;

namespace Examples.DomainDrivenDesign;

/// <summary>
/// A minimal ITenantIdAccessor for this recipe -- a real application would resolve the current tenant
/// from claims or Finbuckle (see multi-tenancy/overview.mdx and Examples.MultiTenancy.Finbuckle).
/// This recipe only demonstrates that AddAsync stamps TenantId automatically once any
/// ITenantIdAccessor resolves a non-empty value.
/// </summary>
public class FixedTenantIdAccessor : ITenantIdAccessor
{
    public string? GetTenantId() => "acme";
}
