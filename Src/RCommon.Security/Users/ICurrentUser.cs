using System;
using System.Security.Claims;

namespace RCommon.Security.Users
{
    public interface ICurrentUser
    {
        Guid? Id { get; }
        bool IsAuthenticated { get; }
        string[] Roles { get; }
        Guid? TenantId { get; }

        Claim FindClaim(string claimType);
        Claim[] FindClaims(string claimType);
        Claim[] GetAllClaims();
    }
}
