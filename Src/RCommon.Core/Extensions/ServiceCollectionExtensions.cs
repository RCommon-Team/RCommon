using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void AddHostedServiceIfSupported<T>(this IServiceCollection services)
            where T : class
        {
            if (typeof(T).GetInterfaces().Contains(typeof(IHostedService)))
            {
                services.TryAddSingleton(sp => (sp.GetRequiredService<T>() as IHostedService)!);
            }
        }
    }
}
