using System;

namespace RCommon.Security.Users
{
    /// <summary>
    /// Convenience extension methods for <see cref="ICurrentUser"/> that simplify claim value retrieval
    /// and provide strongly-typed access to user identity properties.
    /// </summary>
    public static class CurrentUserExtensions
    {
        /// <summary>
        /// Finds a claim of the specified type and returns its string value.
        /// </summary>
        /// <param name="currentUser">The current user instance.</param>
        /// <param name="claimType">The claim type URI to search for.</param>
        /// <returns>The claim value as a string, or <c>null</c> if the claim is not found.</returns>
        public static string? FindClaimValue(this ICurrentUser currentUser, string claimType)
        {
            return currentUser.FindClaim(claimType)?.Value;
        }

        /// <summary>
        /// Gets the current user's ID, asserting that it is not <c>null</c>.
        /// </summary>
        /// <param name="currentUser">The current user instance.</param>
        /// <returns>The user's string identifier.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ICurrentUser.Id"/> is <c>null</c>.</exception>
        public static string GetId(this ICurrentUser currentUser)
        {
            return currentUser.Id
                ?? throw new InvalidOperationException("The current user ID is null. Ensure the user is authenticated and has a NameIdentifier claim.");
        }
    }
}
