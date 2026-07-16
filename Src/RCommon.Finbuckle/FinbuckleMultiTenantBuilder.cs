using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.MultiTenancy;
using RCommon.Security.Claims;

namespace RCommon.Finbuckle
{
    /// <summary>
    /// Configures Finbuckle-based multitenancy for RCommon. Registers <see cref="FinbuckleTenantIdAccessor{TTenantInfo}"/>
    /// as the <see cref="ITenantIdAccessor"/> implementation, replacing the default <see cref="NullTenantIdAccessor"/>.
    /// </summary>
    /// <typeparam name="TTenantInfo">The tenant information type used by Finbuckle.</typeparam>
    /// <remarks>
    /// No <c>new()</c> constraint on net10.0 -- see the remarks on <see cref="IFinbuckleMultiTenantBuilder{TTenantInfo}"/>.
    /// The net8.0/net9.0 builds reference an older Finbuckle.MultiTenant version whose own
    /// <see cref="IMultiTenantContextAccessor{TTenantInfo}"/> still requires <c>new()</c>, so the
    /// constraint must stay conditional per target framework.
    /// </remarks>
#if NET10_0
    public class FinbuckleMultiTenantBuilder<TTenantInfo> : IFinbuckleMultiTenantBuilder<TTenantInfo>
        where TTenantInfo : class, ITenantInfo
#else
    public class FinbuckleMultiTenantBuilder<TTenantInfo> : IFinbuckleMultiTenantBuilder<TTenantInfo>
        where TTenantInfo : class, ITenantInfo, new()
#endif
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FinbuckleMultiTenantBuilder{TTenantInfo}"/>.
        /// </summary>
        /// <param name="services">The service collection for registering multitenancy services.</param>
        public FinbuckleMultiTenantBuilder(IServiceCollection services)
        {
            Services = services ?? throw new System.ArgumentNullException(nameof(services));

            // Replace NullTenantIdAccessor with the Finbuckle implementation, wrapped so that
            // TenantScope.Bypass() suspends its resolution for the scope's lifetime.
            Services.AddTransient<FinbuckleTenantIdAccessor<TTenantInfo>>();
            Services.AddTransient<ITenantIdAccessor>(sp =>
                new TenantScopeAwareTenantIdAccessor(sp.GetRequiredService<FinbuckleTenantIdAccessor<TTenantInfo>>()));
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
