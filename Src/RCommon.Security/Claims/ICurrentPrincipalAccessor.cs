using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Provides access to the current <see cref="ClaimsPrincipal"/> and allows temporarily replacing it within a scoped context.
    /// </summary>
    public interface ICurrentPrincipalAccessor
    {
        /// <summary>
        /// Gets the current <see cref="ClaimsPrincipal"/>, or <c>null</c> if none is available.
        /// </summary>
        ClaimsPrincipal? Principal { get; }

        /// <summary>
        /// Temporarily replaces the current principal with the specified <paramref name="principal"/>.
        /// </summary>
        /// <param name="principal">The new principal to set as current.</param>
        /// <returns>An <see cref="IDisposable"/> that restores the previous principal when disposed.</returns>
        IDisposable Change(ClaimsPrincipal principal);
    }
}
