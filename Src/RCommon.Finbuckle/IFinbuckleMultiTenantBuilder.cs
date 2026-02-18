using Finbuckle.MultiTenant.Abstractions;
using RCommon.MultiTenancy;

namespace RCommon.Finbuckle
{
    /// <summary>
    /// Builder interface for configuring Finbuckle-based multitenancy within the RCommon framework.
    /// </summary>
    /// <typeparam name="TTenantInfo">The tenant information type, which must implement Finbuckle's <see cref="ITenantInfo"/>.</typeparam>
    public interface IFinbuckleMultiTenantBuilder<TTenantInfo> : IMultiTenantBuilder
        where TTenantInfo : class, ITenantInfo, new()
    {
    }
}
