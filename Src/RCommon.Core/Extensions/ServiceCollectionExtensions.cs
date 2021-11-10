using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static void AddCommonFactory<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddTransient<TService, TImplementation>();
            services.AddScoped<Func<TService>>(x => () => x.GetService<TService>());
            services.AddScoped<ICommonFactory<TService>, CommonFactory<TService>>();
        }
    }
}
