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
    public class FinbuckleMultiTenantBuilder<TTenantInfo> : IFinbuckleMultiTenantBuilder<TTenantInfo>
        where TTenantInfo : class, ITenantInfo, new()
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FinbuckleMultiTenantBuilder{TTenantInfo}"/>.
        /// </summary>
        /// <param name="services">The service collection for registering multitenancy services.</param>
        public FinbuckleMultiTenantBuilder(IServiceCollection services)
        {
            Services = services ?? throw new System.ArgumentNullException(nameof(services));

            // Replace NullTenantIdAccessor with the Finbuckle implementation
            Services.AddTransient<ITenantIdAccessor, FinbuckleTenantIdAccessor<TTenantInfo>>();
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
