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
    public static class SecurityConfigurationExtensions
    {

        public static IRCommonConfiguration WithClaimsAndPrincipalAccessor(this IRCommonConfiguration config)
        {
            config.Services.AddTransient<ICurrentPrincipalAccessor, ThreadCurrentPrincipalAccessor>();
            config.Services.AddTransient<ICurrentClient, CurrentClient>();
            config.Services.AddTransient<ICurrentUser, CurrentUser>();
            return config;
        }
    }
}
