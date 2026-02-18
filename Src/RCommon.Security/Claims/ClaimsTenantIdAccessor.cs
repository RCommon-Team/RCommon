using System;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Claims-based implementation of <see cref="ITenantIdAccessor"/> that resolves the current
    /// tenant identifier from the authenticated user's claims via <see cref="ICurrentPrincipalAccessor"/>.
    /// </summary>
    /// <remarks>
    /// Registered automatically when <c>WithClaimsAndPrincipalAccessor()</c> is called during configuration.
    /// Uses <see cref="ClaimsIdentityExtensions.FindTenantId(System.Security.Claims.ClaimsPrincipal)"/>
    /// to extract the tenant claim value configured in <see cref="ClaimTypesConst.TenantId"/>.
    /// </remarks>
    public class ClaimsTenantIdAccessor : ITenantIdAccessor
    {
        private readonly ICurrentPrincipalAccessor _principalAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimsTenantIdAccessor"/> class.
        /// </summary>
        /// <param name="principalAccessor">The principal accessor used to retrieve the current claims principal.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="principalAccessor"/> is <c>null</c>.</exception>
        public ClaimsTenantIdAccessor(ICurrentPrincipalAccessor principalAccessor)
        {
            Guard.IsNotNull(principalAccessor, nameof(principalAccessor));
            _principalAccessor = principalAccessor;
        }

        /// <inheritdoc />
        public string? GetTenantId()
        {
            return _principalAccessor.Principal?.FindTenantId();
        }
    }
}
