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
    /// <summary>
    /// Provides extension methods for <see cref="IServiceCollection"/> to bootstrap RCommon services,
    /// register hosted services, and provide diagnostic output of service registrations.
    /// </summary>
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

        /// <summary>
        /// Registers <typeparamref name="T"/> as an <see cref="IHostedService"/> singleton if the type implements the interface.
        /// </summary>
        /// <typeparam name="T">The service type to check and potentially register as a hosted service.</typeparam>
        /// <param name="services">The service collection to register with.</param>
        public static void AddHostedServiceIfSupported<T>(this IServiceCollection services)
            where T : class
        {
            if (typeof(T).GetInterfaces().Contains(typeof(IHostedService)))
            {
                services.TryAddSingleton(sp => (sp.GetRequiredService<T>() as IHostedService)!);
            }
        }

        /// <summary>
        /// Creates a snapshot collection of all <see cref="ServiceDescriptor"/> instances currently registered in the service collection.
        /// </summary>
        /// <param name="services">The service collection to extract descriptors from.</param>
        /// <returns>A collection of <see cref="ServiceDescriptor"/> instances.</returns>
        public static ICollection<ServiceDescriptor> GenerateServiceDescriptors(this IServiceCollection services)
        {
            ICollection<ServiceDescriptor> returnItems = new List<ServiceDescriptor>(services);
            return returnItems;
        }

        /// <summary>
        /// Generates a formatted string listing all service descriptors in the service collection, ordered by service type name.
        /// Useful for diagnostics and debugging IoC registrations.
        /// </summary>
        /// <param name="services">The service collection to describe.</param>
        /// <returns>A multi-line string describing each service descriptor.</returns>
        public static string GenerateServiceDescriptorsString(this IServiceCollection services)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable<ServiceDescriptor> sds = GenerateServiceDescriptors(services).AsEnumerable()
                .OrderBy(o => o.ServiceType.FullName ?? string.Empty);
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

        /// <summary>
        /// Generates a formatted string listing duplicate service registrations in the service collection.
        /// Identifies registrations where the same service type, lifetime, and implementation type appear more than once.
        /// </summary>
        /// <param name="services">The service collection to check for duplicates.</param>
        /// <returns>A multi-line string describing each duplicate registration, or empty if none found.</returns>
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

        /// <summary>
        /// Logs all service descriptors and any duplicate registrations to both the logger and the console.
        /// Duplicates are logged at warning level.
        /// </summary>
        /// <typeparam name="T">The type used to create the logger category.</typeparam>
        /// <param name="services">The service collection to log.</param>
        /// <param name="loggerFactory">The logger factory used to create the logger.</param>
        public static void LogServiceDescriptors<T>(this IServiceCollection services, ILoggerFactory loggerFactory)
        {
            string iocDebugging = services.GenerateServiceDescriptorsString();
            Func<object, Exception?, string> logMsgStringFunc = (a, b) => iocDebugging;
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
                Func<object, Exception?, string> logMsgStringDuplicatesFunc = (a, b) => iocPossibleDuplicates;
                logger.Log(
                    LogLevel.Warning,
                    ushort.MaxValue,
                    string.Empty,
                    null,
                    logMsgStringDuplicatesFunc);
                Console.WriteLine(iocPossibleDuplicates);
            }
        }

        /// <summary>
        /// Internal data holder for tracking duplicate IoC registrations during diagnostic analysis.
        /// </summary>
        [DebuggerDisplay("ServiceTypeFullName='{ServiceTypeFullName}', Lifetime='{Lifetime}', ImplementationTypeFullName='{ImplementationTypeFullName}', DuplicateCount='{DuplicateCount}'")]
        private sealed class DuplicateIocRegistrationHolder
        {
            /// <summary>Gets or sets the full name of the service type.</summary>
            public string? ServiceTypeFullName { get; set; }

            /// <summary>Gets or sets the service lifetime.</summary>
            public ServiceLifetime Lifetime { get; set; }

            /// <summary>Gets or sets the full name of the implementation type.</summary>
            public string? ImplementationTypeFullName { get; set; }

            /// <summary>Gets or sets the number of duplicate registrations found.</summary>
            public int DuplicateCount { get; set; }
        }
    }
}
