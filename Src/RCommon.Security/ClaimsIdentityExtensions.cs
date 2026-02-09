using RCommon.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security
{
    /// <summary>
    /// Extension methods for <see cref="ClaimsPrincipal"/>, <see cref="IIdentity"/>, and <see cref="ClaimsIdentity"/>
    /// that simplify extracting well-known claim values and managing claims collections.
    /// </summary>
    public static class ClaimsIdentityExtensions
    {
        /// <summary>
        /// Extracts the user identifier from the principal's claims as a <see cref="Guid"/>.
        /// </summary>
        /// <param name="principal">The claims principal to search.</param>
        /// <returns>The parsed user ID, or <c>null</c> if the claim is missing or not a valid GUID.</returns>
        public static Guid? FindUserId(this ClaimsPrincipal principal)
        {
            Guard.IsNotNull(principal, nameof(principal));

            var userIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.UserId);
            if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (Guid.TryParse(userIdOrNull.Value, out Guid guid))
            {
                return guid;
            }

            return null;
        }

        /// <summary>
        /// Extracts the user identifier from the identity's claims as a <see cref="Guid"/>.
        /// </summary>
        /// <param name="identity">The identity to search. Must be castable to <see cref="ClaimsIdentity"/>.</param>
        /// <returns>The parsed user ID, or <c>null</c> if the claim is missing or not a valid GUID.</returns>
        public static Guid? FindUserId(this IIdentity identity)
        {
            Guard.IsNotNull(identity, nameof(identity));

            var claimsIdentity = identity as ClaimsIdentity;

            var userIdOrNull = claimsIdentity?.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.UserId);
            if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (Guid.TryParse(userIdOrNull.Value, out var guid))
            {
                return guid;
            }

            return null;
        }

        /// <summary>
        /// Extracts the tenant identifier from the principal's claims as a <see cref="Guid"/>.
        /// </summary>
        /// <param name="principal">The claims principal to search.</param>
        /// <returns>The parsed tenant ID, or <c>null</c> if the claim is missing or not a valid GUID.</returns>
        public static Guid? FindTenantId(this ClaimsPrincipal principal)
        {
            Guard.IsNotNull(principal, nameof(principal));

            var tenantIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.TenantId);
            if (tenantIdOrNull == null || tenantIdOrNull.Value.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (Guid.TryParse(tenantIdOrNull.Value, out var guid))
            {
                return guid;
            }

            return null;
        }

        /// <summary>
        /// Extracts the tenant identifier from the identity's claims as a <see cref="Guid"/>.
        /// </summary>
        /// <param name="identity">The identity to search. Must be castable to <see cref="ClaimsIdentity"/>.</param>
        /// <returns>The parsed tenant ID, or <c>null</c> if the claim is missing or not a valid GUID.</returns>
        public static Guid? FindTenantId(this IIdentity identity)
        {
            Guard.IsNotNull(identity, nameof(identity));

            var claimsIdentity = identity as ClaimsIdentity;

            var tenantIdOrNull = claimsIdentity?.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.TenantId);
            if (tenantIdOrNull == null || tenantIdOrNull.Value.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (Guid.TryParse(tenantIdOrNull.Value, out var guid))
            {
                return guid;
            }

            return null;
        }

        /// <summary>
        /// Extracts the client identifier from the principal's claims.
        /// </summary>
        /// <param name="principal">The claims principal to search.</param>
        /// <returns>The client ID string, or <c>null</c> if the claim is missing or empty.</returns>
        public static string? FindClientId(this ClaimsPrincipal principal)
        {
            Guard.IsNotNull(principal, nameof(principal));

            var clientIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.ClientId);
            if (clientIdOrNull == null || clientIdOrNull.Value.IsNullOrWhiteSpace())
            {
                return null;
            }

            return clientIdOrNull.Value;
        }

        /// <summary>
        /// Extracts the client identifier from the identity's claims.
        /// </summary>
        /// <param name="identity">The identity to search. Must be castable to <see cref="ClaimsIdentity"/>.</param>
        /// <returns>The client ID string, or <c>null</c> if the claim is missing or empty.</returns>
        public static string? FindClientId(this IIdentity identity)
        {
            Guard.IsNotNull(identity, nameof(identity));

            var claimsIdentity = identity as ClaimsIdentity;

            var clientIdOrNull = claimsIdentity?.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.ClientId);
            if (clientIdOrNull == null || clientIdOrNull.Value.IsNullOrWhiteSpace())
            {
                return null;
            }

            return clientIdOrNull.Value;
        }



        /// <summary>
        /// Adds a claim to the identity only if no claim with the same type already exists (case-insensitive comparison).
        /// </summary>
        /// <param name="claimsIdentity">The identity to add the claim to.</param>
        /// <param name="claim">The claim to add.</param>
        /// <returns>The same <see cref="ClaimsIdentity"/> instance for fluent chaining.</returns>
        public static ClaimsIdentity AddIfNotContains(this ClaimsIdentity claimsIdentity, Claim claim)
        {
            Guard.IsNotNull(claimsIdentity, nameof(claimsIdentity));

            if (!claimsIdentity.Claims.Any(x => string.Equals(x.Type, claim.Type, StringComparison.OrdinalIgnoreCase)))
            {
                claimsIdentity.AddClaim(claim);
            }

            return claimsIdentity;
        }

        /// <summary>
        /// Removes all existing claims of the same type and adds the new <paramref name="claim"/>.
        /// </summary>
        /// <param name="claimsIdentity">The identity to modify.</param>
        /// <param name="claim">The claim to set, replacing any existing claims of the same type.</param>
        /// <returns>The same <see cref="ClaimsIdentity"/> instance for fluent chaining.</returns>
        public static ClaimsIdentity AddOrReplace(this ClaimsIdentity claimsIdentity, Claim claim)
        {
            Guard.IsNotNull(claimsIdentity, nameof(claimsIdentity));

            // Remove all claims matching the type before adding the replacement.
            foreach (var x in claimsIdentity.FindAll(claim.Type).ToList())
            {
                claimsIdentity.RemoveClaim(x);
            }

            claimsIdentity.AddClaim(claim);

            return claimsIdentity;
        }

        /// <summary>
        /// Adds a <see cref="ClaimsIdentity"/> to the principal only if no identity with the same
        /// <see cref="ClaimsIdentity.AuthenticationType"/> already exists (case-insensitive comparison).
        /// </summary>
        /// <param name="principal">The principal to add the identity to.</param>
        /// <param name="identity">The identity to add.</param>
        /// <returns>The same <see cref="ClaimsPrincipal"/> instance for fluent chaining.</returns>
        public static ClaimsPrincipal AddIdentityIfNotContains(this ClaimsPrincipal principal, ClaimsIdentity identity)
        {
            Guard.IsNotNull(principal, nameof(principal));

            if (!principal.Identities.Any(x => string.Equals(x.AuthenticationType, identity.AuthenticationType, StringComparison.OrdinalIgnoreCase)))
            {
                principal.AddIdentity(identity);
            }

            return principal;
        }
    }
}
