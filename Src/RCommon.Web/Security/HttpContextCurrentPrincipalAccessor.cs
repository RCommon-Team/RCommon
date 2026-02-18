using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RCommon.Security.Claims;

namespace RCommon.Web.Security
{
    /// <summary>
    /// An <see cref="ICurrentPrincipalAccessor"/> implementation that retrieves the default principal
    /// from the current HTTP context via <see cref="IHttpContextAccessor"/>.
    /// </summary>
    /// <remarks>
    /// Use this accessor in ASP.NET Core applications where the authenticated user is available on
    /// <see cref="HttpContext.User"/>. Register it by calling
    /// <see cref="WebConfigurationExtensions.WithClaimsAndPrincipalAccessorForWeb"/> instead of
    /// <see cref="SecurityConfigurationExtensions.WithClaimsAndPrincipalAccessor"/>.
    /// </remarks>
    public class HttpContextCurrentPrincipalAccessor : CurrentPrincipalAccessorBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextCurrentPrincipalAccessor"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor used to retrieve the current request context.</param>
        public HttpContextCurrentPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new System.ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc />
        protected override ClaimsPrincipal? GetClaimsPrincipal()
        {
            return _httpContextAccessor.HttpContext?.User;
        }
    }
}
