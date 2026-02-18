using RCommon.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Users
{
    /// <summary>
    /// Default implementation of <see cref="ICurrentUser"/> that resolves user information
    /// from the current <see cref="ClaimsPrincipal"/> via <see cref="ICurrentPrincipalAccessor"/>.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        /// <summary>
        /// Shared empty array returned when the principal has no claims, avoiding repeated allocations.
        /// </summary>
        private static readonly Claim[] EmptyClaimsArray = new Claim[0];

        private readonly ICurrentPrincipalAccessor _principalAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUser"/> class.
        /// </summary>
        /// <param name="principalAccessor">The accessor used to retrieve the current claims principal.</param>
        public CurrentUser(ICurrentPrincipalAccessor principalAccessor)
        {
            _principalAccessor = principalAccessor;
        }

        /// <inheritdoc />
        public virtual bool IsAuthenticated => Id.HasValue;

        /// <inheritdoc />
        public virtual Guid? Id => _principalAccessor.Principal?.FindUserId();

        /// <inheritdoc />
        public virtual string? TenantId => _principalAccessor.Principal?.FindTenantId();

        /// <inheritdoc />
        public virtual string[] Roles => FindClaims(ClaimTypesConst.Role).Select(c => c.Value).Distinct().ToArray();

        /// <inheritdoc />
        public virtual Claim? FindClaim(string claimType)
        {
            return _principalAccessor.Principal?.Claims.FirstOrDefault(c => c.Type == claimType);
        }

        /// <inheritdoc />
        public virtual Claim[] FindClaims(string claimType)
        {
            return _principalAccessor.Principal?.Claims.Where(c => c.Type == claimType).ToArray() ?? EmptyClaimsArray;
        }

        /// <inheritdoc />
        public virtual Claim[] GetAllClaims()
        {
            return _principalAccessor.Principal?.Claims.ToArray() ?? EmptyClaimsArray;
        }
    }
}
