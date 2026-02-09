using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// An <see cref="ICurrentPrincipalAccessor"/> implementation that retrieves the default principal
    /// from <see cref="Thread.CurrentPrincipal"/>.
    /// </summary>
    /// <remarks>
    /// This is the default accessor registered by <see cref="RCommon.SecurityConfigurationExtensions.WithClaimsAndPrincipalAccessor"/>.
    /// In ASP.NET Core scenarios, consider using an HTTP-context-based accessor instead.
    /// </remarks>
    public class ThreadCurrentPrincipalAccessor : CurrentPrincipalAccessorBase
    {
        /// <inheritdoc />
        protected override ClaimsPrincipal? GetClaimsPrincipal()
        {
            return Thread.CurrentPrincipal as ClaimsPrincipal;
        }
    }
}
