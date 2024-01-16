using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static IRCommonBuilder AddRCommon(this IServiceCollection services)
        {
            var config = new RCommonBuilder(services);
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

        public static ICollection<ServiceDescriptor> GenerateServiceDescriptors(this IServiceCollection services)
        {
            ICollection<ServiceDescriptor> returnItems = new List<ServiceDescriptor>(services);
            return returnItems;
        }

        public static string GenerateServiceDescriptorsString(this IServiceCollection services)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable<ServiceDescriptor> sds = GenerateServiceDescriptors(services).AsEnumerable()
                .OrderBy(o => o.ServiceType.FullName);
            foreach (ServiceDescriptor sd in sds)
            {
                sb.Append($"(ServiceDescriptor):");
                sb.Append($"FullName='{sd.ServiceType.FullName}',");
                sb.Append($"Lifetime='{sd.Lifetime}',");
                sb.Append($"ImplementationType?.FullName='{sd.ImplementationType?.FullName}'");
                sb.Append(System.Environment.NewLine);
            }

            string returnValue = sb.ToString();
            return returnValue;
        }

        public static string GeneratePossibleDuplicatesServiceDescriptorsString(this IServiceCollection services)
        {
            StringBuilder sb = new StringBuilder();

            ICollection<DuplicateIocRegistrationHolder> foundDuplicates =
                (from t in GenerateServiceDescriptors(services)
                 where !string.IsNullOrWhiteSpace(t.ServiceType.FullName)
                       && !string.IsNullOrWhiteSpace(t.ImplementationType?.FullName)
                 group t by new
                 {
                     ServiceTypeFullName = t.ServiceType.FullName,
                     t.Lifetime,
                     ImplementationTypeFullName = t.ImplementationType?.FullName
                 }
                    into grp
                 where grp.Count() > 1
                 select new DuplicateIocRegistrationHolder()
                 {
                     ServiceTypeFullName = grp.Key.ServiceTypeFullName,
                     Lifetime = grp.Key.Lifetime,
                     ImplementationTypeFullName = grp.Key.ImplementationTypeFullName,
                     DuplicateCount = grp.Count()
                 }).ToList();

            foreach (DuplicateIocRegistrationHolder sd in foundDuplicates
                         .OrderBy(o => o.ServiceTypeFullName))
            {
                sb.Append($"(DuplicateIocRegistrationHolderServiceDescriptor):");
                sb.Append($"ServiceTypeFullName='{sd.ServiceTypeFullName}',");
                sb.Append($"Lifetime='{sd.Lifetime}',");
                sb.Append($"ImplementationTypeFullName='{sd.ImplementationTypeFullName}',");
                sb.Append($"DuplicateCount='{sd.DuplicateCount}'");
                sb.Append(System.Environment.NewLine);
            }

            string returnValue = sb.ToString();
            return returnValue;
        }

        public static void LogServiceDescriptors<T>(this IServiceCollection services, ILoggerFactory loggerFactory)
        {
            string iocDebugging = services.GenerateServiceDescriptorsString();
            Func<object, Exception, string> logMsgStringFunc = (a, b) => iocDebugging;
            ILogger<T> logger = loggerFactory.CreateLogger<T>();
            logger.Log(
                LogLevel.Information,
                ushort.MaxValue,
                string.Empty,
                null,
                logMsgStringFunc);
            Console.WriteLine(iocDebugging);

            string iocPossibleDuplicates = GeneratePossibleDuplicatesServiceDescriptorsString(services);
            if (!string.IsNullOrWhiteSpace(iocPossibleDuplicates))
            {
                Func<object, Exception, string> logMsgStringDuplicatesFunc = (a, b) => iocPossibleDuplicates;
                logger.Log(
                    LogLevel.Warning,
                    ushort.MaxValue,
                    string.Empty,
                    null,
                    logMsgStringDuplicatesFunc);
                Console.WriteLine(iocPossibleDuplicates);
            }
        }

        [DebuggerDisplay("ServiceTypeFullName='{ServiceTypeFullName}', Lifetime='{Lifetime}', ImplementationTypeFullName='{ImplementationTypeFullName}', DuplicateCount='{DuplicateCount}'")]
        private sealed class DuplicateIocRegistrationHolder
        {
            public string ServiceTypeFullName { get; set; }

            public ServiceLifetime Lifetime { get; set; }

            public string ImplementationTypeFullName { get; set; }

            public int DuplicateCount { get; set; }
        }
    }
}
