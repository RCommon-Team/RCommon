using Microsoft.Extensions.DependencyInjection;
using RCommon.Security.Claims;
using RCommon.Security.Clients;
using RCommon.Security.Users;
using RCommon.Web.Security;

namespace RCommon
{
    /// <summary>
    /// Extension methods for <see cref="IRCommonBuilder"/> that register web-specific security services
    /// using the HTTP context to access the current <see cref="System.Security.Claims.ClaimsPrincipal"/>.
    /// </summary>
    public static class WebConfigurationExtensions
    {
        /// <summary>
        /// Registers claims and principal accessor services for ASP.NET Core web applications.
        /// Uses <see cref="HttpContextCurrentPrincipalAccessor"/> to resolve the current user from
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/> instead of <see cref="System.Threading.Thread.CurrentPrincipal"/>.
        /// </summary>
        /// <param name="config">The RCommon builder to configure.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> instance for fluent chaining.</returns>
        /// <remarks>
        /// Use this instead of <see cref="SecurityConfigurationExtensions.WithClaimsAndPrincipalAccessor"/>
        /// in ASP.NET Core applications. This method also ensures <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>
        /// is registered in the service collection.
        /// </remarks>
        public static IRCommonBuilder WithClaimsAndPrincipalAccessorForWeb(this IRCommonBuilder config)
        {
            config.Services.AddHttpContextAccessor();
            config.Services.AddTransient<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();
            config.Services.AddTransient<ICurrentClient, CurrentClient>();
            config.Services.AddTransient<ICurrentUser, CurrentUser>();
            config.Services.AddTransient<ITenantIdAccessor, ClaimsTenantIdAccessor>();
            return config;
        }
    }
}
