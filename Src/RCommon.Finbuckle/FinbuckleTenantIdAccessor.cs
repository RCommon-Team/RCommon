using Finbuckle.MultiTenant.Abstractions;
using RCommon.Security.Claims;
using System;

namespace RCommon.Finbuckle
{
    /// <summary>
    /// Adapts Finbuckle's <see cref="IMultiTenantContextAccessor{TTenantInfo}"/> to RCommon's
    /// <see cref="ITenantIdAccessor"/> interface, bridging the tenant resolution mechanism.
    /// </summary>
    /// <typeparam name="TTenantInfo">The tenant information type used by Finbuckle.</typeparam>
    /// <remarks>
    /// No <c>new()</c> constraint on net10.0 -- see the remarks on <see cref="IFinbuckleMultiTenantBuilder{TTenantInfo}"/>.
    /// The net8.0/net9.0 builds reference an older Finbuckle.MultiTenant version whose own
    /// <see cref="IMultiTenantContextAccessor{TTenantInfo}"/> still requires <c>new()</c>, so the
    /// constraint must stay conditional per target framework.
    /// </remarks>
#if NET10_0
    public class FinbuckleTenantIdAccessor<TTenantInfo> : ITenantIdAccessor
        where TTenantInfo : class, ITenantInfo
#else
    public class FinbuckleTenantIdAccessor<TTenantInfo> : ITenantIdAccessor
        where TTenantInfo : class, ITenantInfo, new()
#endif
    {
        private readonly IMultiTenantContextAccessor<TTenantInfo> _contextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="FinbuckleTenantIdAccessor{TTenantInfo}"/>.
        /// </summary>
        /// <param name="contextAccessor">The Finbuckle multi-tenant context accessor.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="contextAccessor"/> is <c>null</c>.</exception>
        public FinbuckleTenantIdAccessor(IMultiTenantContextAccessor<TTenantInfo> contextAccessor)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        /// <inheritdoc />
        public string? GetTenantId()
        {
            return _contextAccessor.MultiTenantContext?.TenantInfo?.Id;
        }
    }
}
