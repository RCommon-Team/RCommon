using RCommon.Security.Claims;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Finds a claim of the specified type and converts its value to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target value type to convert the claim value to.</typeparam>
        /// <param name="currentUser">The current user instance.</param>
        /// <param name="claimType">The claim type URI to search for.</param>
        /// <returns>The converted claim value, or <c>default</c> if the claim is not found.</returns>
        /// <remarks>Uses <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> with <see cref="CultureInfo.InvariantCulture"/>.</remarks>
        public static T FindClaimValue<T>(this ICurrentUser currentUser, string claimType)
            where T : struct
        {
            var value = currentUser.FindClaimValue(claimType);
            if (value == null)
            {
                return default;
            }
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the current user's ID, asserting that it is not <c>null</c>.
        /// </summary>
        /// <param name="currentUser">The current user instance.</param>
        /// <returns>The user's <see cref="Guid"/> identifier.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ICurrentUser.Id"/> is <c>null</c>.</exception>
        public static Guid GetId(this ICurrentUser currentUser)
        {
            Debug.Assert(currentUser.Id != null, "currentUser.Id != null");

            return currentUser.Id.Value;
        }

        
    }
}
