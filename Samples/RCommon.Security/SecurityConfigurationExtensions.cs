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

        public static IRCommonConfiguration WithClaimsAndPrincipalAccessor(this IRCommonConfiguration config)
        {
            config.ContainerAdapter.Services.AddTransient<ICurrentPrincipalAccessor, ThreadCurrentPrincipalAccessor>();
            config.ContainerAdapter.Services.AddTransient<ICurrentClient, CurrentClient>();
            config.ContainerAdapter.Services.AddTransient<ICurrentUser, CurrentUser>();
            return config;
        }
    }
}
