using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Convenience extension methods for <see cref="ICurrentPrincipalAccessor"/> that allow changing the
    /// current principal from a single <see cref="Claim"/>, a collection of claims, or a <see cref="ClaimsIdentity"/>.
    /// </summary>
    public static class CurrentPrincipalAccessorExtensions
    {
        /// <summary>
        /// Temporarily replaces the current principal with one containing the specified <paramref name="claim"/>.
        /// </summary>
        /// <param name="currentPrincipalAccessor">The principal accessor to change.</param>
        /// <param name="claim">The claim to include in the new principal.</param>
        /// <returns>An <see cref="IDisposable"/> that restores the previous principal on disposal.</returns>
        public static IDisposable Change(this ICurrentPrincipalAccessor currentPrincipalAccessor, Claim claim)
        {
            return currentPrincipalAccessor.Change(new[] { claim });
        }

        /// <summary>
        /// Temporarily replaces the current principal with one built from the specified <paramref name="claims"/>.
        /// </summary>
        /// <param name="currentPrincipalAccessor">The principal accessor to change.</param>
        /// <param name="claims">The claims to include in the new principal's identity.</param>
        /// <returns>An <see cref="IDisposable"/> that restores the previous principal on disposal.</returns>
        public static IDisposable Change(this ICurrentPrincipalAccessor currentPrincipalAccessor, IEnumerable<Claim> claims)
        {
            return currentPrincipalAccessor.Change(new ClaimsIdentity(claims));
        }

        /// <summary>
        /// Temporarily replaces the current principal with one wrapping the specified <paramref name="claimsIdentity"/>.
        /// </summary>
        /// <param name="currentPrincipalAccessor">The principal accessor to change.</param>
        /// <param name="claimsIdentity">The identity to wrap in a new <see cref="ClaimsPrincipal"/>.</param>
        /// <returns>An <see cref="IDisposable"/> that restores the previous principal on disposal.</returns>
        public static IDisposable Change(this ICurrentPrincipalAccessor currentPrincipalAccessor, ClaimsIdentity claimsIdentity)
        {
            return currentPrincipalAccessor.Change(new ClaimsPrincipal(claimsIdentity));
        }
    }
}
