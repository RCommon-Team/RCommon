using RCommon.Entities;

namespace Examples.MultiTenancy.Finbuckle;

public class Product : BusinessEntity<Guid>, IMultiTenant
{
    public Product() : base(Guid.NewGuid())
    {
    }

    public string Name { get; set; } = string.Empty;

    // Populated automatically by the repository from the current tenant context.
    public string? TenantId { get; set; }
}
