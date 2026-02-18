using System;
using System.Security.Claims;

namespace RCommon.Security.Users
{
    /// <summary>
    /// Represents the currently authenticated user, providing access to identity properties and claims.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// Gets the unique identifier of the current user, or <c>null</c> if no user is authenticated.
        /// </summary>
        Guid? Id { get; }

        /// <summary>
        /// Gets a value indicating whether the current user is authenticated (i.e., <see cref="Id"/> is not <c>null</c>).
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the distinct set of role names assigned to the current user.
        /// </summary>
        string[] Roles { get; }

        /// <summary>
        /// Gets the tenant identifier of the current user, or <c>null</c> if no tenant claim is present.
        /// </summary>
        string? TenantId { get; }

        /// <summary>
        /// Finds the first claim matching the specified <paramref name="claimType"/>.
        /// </summary>
        /// <param name="claimType">The claim type URI to search for.</param>
        /// <returns>The matching <see cref="Claim"/>, or <c>null</c> if not found.</returns>
        Claim? FindClaim(string claimType);

        /// <summary>
        /// Finds all claims matching the specified <paramref name="claimType"/>.
        /// </summary>
        /// <param name="claimType">The claim type URI to search for.</param>
        /// <returns>An array of matching claims, or an empty array if none are found.</returns>
        Claim[] FindClaims(string claimType);

        /// <summary>
        /// Gets all claims associated with the current user.
        /// </summary>
        /// <returns>An array of all claims, or an empty array if the user has no claims.</returns>
        Claim[] GetAllClaims();
    }
}
