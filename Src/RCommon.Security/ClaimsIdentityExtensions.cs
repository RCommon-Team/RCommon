using RCommon.Extensions;
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
    public static class ClaimsIdentityExtensions
    {
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

        public static string FindClientId(this ClaimsPrincipal principal)
        {
            Guard.IsNotNull(principal, nameof(principal));

            var clientIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.ClientId);
            if (clientIdOrNull == null || clientIdOrNull.Value.IsNullOrWhiteSpace())
            {
                return null;
            }

            return clientIdOrNull.Value;
        }

        public static string FindClientId(this IIdentity identity)
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

        

        public static ClaimsIdentity AddIfNotContains(this ClaimsIdentity claimsIdentity, Claim claim)
        {
            Guard.IsNotNull(claimsIdentity, nameof(claimsIdentity));

            if (!claimsIdentity.Claims.Any(x => string.Equals(x.Type, claim.Type, StringComparison.OrdinalIgnoreCase)))
            {
                claimsIdentity.AddClaim(claim);
            }

            return claimsIdentity;
        }

        public static ClaimsIdentity AddOrReplace(this ClaimsIdentity claimsIdentity, Claim claim)
        {
            Guard.IsNotNull(claimsIdentity, nameof(claimsIdentity));

            foreach (var x in claimsIdentity.FindAll(claim.Type).ToList())
            {
                claimsIdentity.RemoveClaim(x);
            }

            claimsIdentity.AddClaim(claim);

            return claimsIdentity;
        }

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
