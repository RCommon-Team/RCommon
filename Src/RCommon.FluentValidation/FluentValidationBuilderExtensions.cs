using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices.Validation;
using RCommon.FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public static class FluentValidationBuilderExtensions
    {

        public static void AddValidator<T, TValidator>(this IFluentValidationBuilder builder)
            where TValidator : class, IValidator<T>
            where T : class
        {
            builder.Services.AddScoped<IValidator<T>, TValidator>();
        }

        public static void AddValidatorsFromAssembly(this IFluentValidationBuilder builder, Assembly assembly, 
            ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool> filter = null, 
            bool includeInternalTypes = false)
        {
            builder.Services.AddValidatorsFromAssembly(assembly, lifetime, filter, includeInternalTypes);
        }

        public static void AddValidatorsFromAssemblies(this IFluentValidationBuilder builder, IEnumerable<Assembly> assemblies,
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            Func<AssemblyScanner.AssemblyScanResult, bool> filter = null, bool includeInternalTypes = false)
        {
            builder.Services.AddValidatorsFromAssemblies(assemblies, lifetime, filter, includeInternalTypes);
        }

        public static void AddValidatorsFromAssemblyContaining(this IFluentValidationBuilder builder, Type type,
            ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool> filter = null,
            bool includeInternalTypes = false)
        {
            builder.Services.AddValidatorsFromAssemblyContaining(type, lifetime, filter, includeInternalTypes);
        }
    }
}
