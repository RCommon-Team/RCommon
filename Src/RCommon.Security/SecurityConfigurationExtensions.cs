using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Security.Users;
using RCommon.Security.Claims;
using RCommon.Security.Clients;

namespace RCommon
{
    /// <summary>
    /// Extension methods for <see cref="IRCommonBuilder"/> that register security-related services
    /// such as claims-based principal access, current user, and current client abstractions.
    /// </summary>
    public static class SecurityConfigurationExtensions
    {
        /// <summary>
        /// Registers the default claims and principal accessor services into the dependency injection container.
        /// This includes <see cref="ICurrentPrincipalAccessor"/>, <see cref="ICurrentClient"/>, and <see cref="ICurrentUser"/>.
        /// </summary>
        /// <param name="config">The RCommon builder to configure.</param>
        /// <returns>The same <see cref="IRCommonBuilder"/> instance for fluent chaining.</returns>
        public static IRCommonBuilder WithClaimsAndPrincipalAccessor(this IRCommonBuilder config)
        {
            config.Services.AddTransient<ICurrentPrincipalAccessor, ThreadCurrentPrincipalAccessor>();
            config.Services.AddTransient<ICurrentClient, CurrentClient>();
            config.Services.AddTransient<ICurrentUser, CurrentUser>();
            return config;
        }
    }
}
