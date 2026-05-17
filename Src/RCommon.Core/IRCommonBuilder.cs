using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon
{
    /// <summary>
    /// Defines the fluent builder interface for configuring RCommon framework services including
    /// GUID generation, date/time systems, and common factories.
    /// </summary>
    /// <seealso cref="RCommonBuilder"/>
    public interface IRCommonBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register services.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Finalizes the configuration and returns the populated <see cref="IServiceCollection"/>.
        /// </summary>
        /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
        IServiceCollection Configure();

        /// <summary>
        /// Configures the date/time system using the specified <see cref="SystemTimeOptions"/>.
        /// </summary>
        /// <param name="actions">An action to configure <see cref="SystemTimeOptions"/>.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions);

        /// <summary>
        /// Configures the <see cref="IGuidGenerator"/> to use <see cref="SequentialGuidGenerator"/>
        /// with the specified options.
        /// </summary>
        /// <param name="actions">An action to configure <see cref="SequentialGuidGeneratorOptions"/>.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions);

        /// <summary>
        /// Configures the <see cref="IGuidGenerator"/> to use <see cref="SimpleGuidGenerator"/>
        /// which generates standard random GUIDs.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        IRCommonBuilder WithSimpleGuidGenerator();

        /// <summary>
        /// Registers a service and its implementation along with a corresponding <see cref="ICommonFactory{T}"/>
        /// for creating instances through dependency injection.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
        /// <returns>The builder instance for method chaining.</returns>
        IRCommonBuilder WithCommonFactory<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Returns the cached sub-builder for <typeparamref name="TSubBuilder"/> if one exists,
        /// otherwise invokes <paramref name="factory"/>, caches the result, and returns it.
        /// </summary>
        /// <typeparam name="TSubBuilder">Concrete sub-builder type (e.g., <c>EFCorePerisistenceBuilder</c>).</typeparam>
        /// <param name="factory">Parameterless factory invoked exactly once per <typeparamref name="TSubBuilder"/>
        /// per <see cref="IRCommonBuilder"/>. Callers close over whichever constructor argument the sub-builder
        /// requires — typically <c>builder.Services</c> or <c>builder</c> itself.</param>
        TSubBuilder GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder> factory)
            where TSubBuilder : class;

        /// <summary>
        /// Returns soft-duplicate diagnostic output stashed at finalize when no <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>
        /// was available. Empty string if no duplicates were detected.
        /// </summary>
        string GetBootstrapDiagnostics();
    }
}
