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
    /// <summary>
    /// Provides extension methods on <see cref="IFluentValidationBuilder"/> for registering
    /// FluentValidation validators into the DI container.
    /// </summary>
    public static class FluentValidationBuilderExtensions
    {

        /// <summary>
        /// Registers a specific FluentValidation validator for the given type with a scoped lifetime.
        /// </summary>
        /// <typeparam name="T">The type being validated.</typeparam>
        /// <typeparam name="TValidator">The <see cref="IValidator{T}"/> implementation to register.</typeparam>
        /// <param name="builder">The FluentValidation builder instance.</param>
        public static void AddValidator<T, TValidator>(this IFluentValidationBuilder builder)
            where TValidator : class, IValidator<T>
            where T : class
        {
            builder.Services.AddScoped<IValidator<T>, TValidator>();
        }

        /// <summary>
        /// Scans the specified assembly and registers all <see cref="IValidator{T}"/> implementations found.
        /// </summary>
        /// <param name="builder">The FluentValidation builder instance.</param>
        /// <param name="assembly">The assembly to scan for validator types.</param>
        /// <param name="lifetime">The service lifetime for registered validators. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
        /// <param name="filter">An optional filter to include or exclude specific scan results.</param>
        /// <param name="includeInternalTypes">Whether to include internal (non-public) validator types. Defaults to <see langword="false"/>.</param>
        public static void AddValidatorsFromAssembly(this IFluentValidationBuilder builder, Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool>? filter = null,
            bool includeInternalTypes = false)
        {
            builder.Services.AddValidatorsFromAssembly(assembly, lifetime, filter, includeInternalTypes);
        }

        /// <summary>
        /// Scans multiple assemblies and registers all <see cref="IValidator{T}"/> implementations found.
        /// </summary>
        /// <param name="builder">The FluentValidation builder instance.</param>
        /// <param name="assemblies">The assemblies to scan for validator types.</param>
        /// <param name="lifetime">The service lifetime for registered validators. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
        /// <param name="filter">An optional filter to include or exclude specific scan results.</param>
        /// <param name="includeInternalTypes">Whether to include internal (non-public) validator types. Defaults to <see langword="false"/>.</param>
        public static void AddValidatorsFromAssemblies(this IFluentValidationBuilder builder, IEnumerable<Assembly> assemblies,
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            Func<AssemblyScanner.AssemblyScanResult, bool>? filter = null, bool includeInternalTypes = false)
        {
            builder.Services.AddValidatorsFromAssemblies(assemblies, lifetime, filter, includeInternalTypes);
        }

        /// <summary>
        /// Scans the assembly containing the specified <paramref name="type"/> and registers all
        /// <see cref="IValidator{T}"/> implementations found.
        /// </summary>
        /// <param name="builder">The FluentValidation builder instance.</param>
        /// <param name="type">A type whose containing assembly will be scanned.</param>
        /// <param name="lifetime">The service lifetime for registered validators. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
        /// <param name="filter">An optional filter to include or exclude specific scan results.</param>
        /// <param name="includeInternalTypes">Whether to include internal (non-public) validator types. Defaults to <see langword="false"/>.</param>
        public static void AddValidatorsFromAssemblyContaining(this IFluentValidationBuilder builder, Type type,
            ServiceLifetime lifetime = ServiceLifetime.Scoped, Func<AssemblyScanner.AssemblyScanResult, bool>? filter = null,
            bool includeInternalTypes = false)
        {
            builder.Services.AddValidatorsFromAssemblyContaining(type, lifetime, filter, includeInternalTypes);
        }
    }
}
