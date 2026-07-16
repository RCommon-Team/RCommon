using Finbuckle.MultiTenant.Abstractions;
using RCommon.MultiTenancy;

namespace RCommon.Finbuckle
{
    /// <summary>
    /// Builder interface for configuring Finbuckle-based multitenancy within the RCommon framework.
    /// </summary>
    /// <typeparam name="TTenantInfo">The tenant information type, which must implement Finbuckle's <see cref="ITenantInfo"/>.</typeparam>
    /// <remarks>
    /// No <c>new()</c> constraint is applied -- Finbuckle's own built-in <c>TenantInfo</c> class has
    /// <c>required</c> members as of the currently referenced Finbuckle.MultiTenant version, which C#
    /// does not allow to satisfy a <c>new()</c> constraint. Nothing in this hierarchy constructs a
    /// <typeparamref name="TTenantInfo"/> instance, so the constraint was never load-bearing.
    /// </remarks>
    public interface IFinbuckleMultiTenantBuilder<TTenantInfo> : IMultiTenantBuilder
        where TTenantInfo : class, ITenantInfo
    {
    }
}
