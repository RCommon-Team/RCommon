using RCommon.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Security.Users;
using RCommon.Security.Claims;
using RCommon.Security.Clients;

namespace RCommon.Security
{
    public static class SecurityConfigurationExtensions
    {

        public static IRCommonConfiguration WithSecurity<T>(this IRCommonConfiguration config, Action<T> actions)
            where T : IRCommonConfiguration
        {
            config.ContainerAdapter.Services.AddTransient<ICurrentPrincipalAccessor, ThreadCurrentPrincipalAccessor>();
            config.ContainerAdapter.Services.AddTransient<ICurrentClient, CurrentClient>();
            config.ContainerAdapter.Services.AddTransient<ICurrentUser, CurrentUser>();
            return config;
        }
    }
}
