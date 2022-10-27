using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Entry point for bootstrapping RCommon. 
        /// </summary>
        /// <param name="services">Dependency Injection services that serve as our interface for injecting additional services.</param>
        /// <returns>RCommon configuration interface for additional chaining.</returns>
        public static IRCommonConfiguration AddRCommon(this IServiceCollection services)
        {
            var config = new RCommonConfiguration(services);
            config.Configure();
            return config;
        }

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
