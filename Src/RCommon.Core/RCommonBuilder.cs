using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using Microsoft.Extensions.Options;

namespace RCommon
{
    ///<summary>
    /// Default implementation of <see cref="IRCommonBuilder"/> class.
    ///</summary>
    public class RCommonBuilder : IRCommonBuilder
    {
        /// <inheritdoc />
        public IServiceCollection Services { get; }

        private SingletonRegistration _guidRegistration;
        private SingletonRegistration _dateTimeRegistration;
        private readonly Dictionary<Type, object> _subBuilderCache = new();
        private bool _diagnosticsRun;
        private string _bootstrapDiagnostics = string.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="RCommonBuilder"/> and registers core framework services
        /// including caching options, the <see cref="EventSubscriptionManager"/>, the <see cref="IEventBus"/>,
        /// and the default <see cref="IEventRouter"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
        /// <exception cref="NullReferenceException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
        public RCommonBuilder(IServiceCollection services)
        {
            Guard.Against<NullReferenceException>(services == null, "IServiceCollection cannot be null");
            Services = services!;

            this.Services.Configure<CachingOptions>(x => { x.CachingEnabled = false; });

            // Event Subscription Manager - tracks which events are routed to which producers
            Services.AddSingleton(new EventSubscriptionManager());

            // Event Bus
            Services.AddSingleton<IEventBus>(sp =>
            {
                return new InMemoryEventBus(sp);
            });
            Services.AddScoped<IEventRouter, InMemoryTransactionalEventRouter>();
            Services.AddOptions<EventHandlingOptions>();
        }

        /// <inheritdoc />
        /// <exception cref="RCommonBuilderException">Thrown if a different GUID generator implementation has already been configured.</exception>
        public IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions)
        {
            if (_guidRegistration.Configured)
            {
                if (_guidRegistration.ImplementationType == typeof(SequentialGuidGenerator))
                {
                    // Same impl re-registered: idempotent; just append the options delegate
                    this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
                    return this;
                }
                throw new RCommonBuilderException(
                    $"IGuidGenerator already configured as '{_guidRegistration.ImplementationType?.FullName}'; " +
                    $"cannot reconfigure as '{typeof(SequentialGuidGenerator).FullName}'. " +
                    "To configure multiple modules consistently, ensure all modules agree on the same IGuidGenerator implementation, " +
                    "or designate a single composition root that performs this registration.");
            }

            this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
            this.Services.AddTransient<IGuidGenerator, SequentialGuidGenerator>();
            _guidRegistration = new SingletonRegistration { Configured = true, ImplementationType = typeof(SequentialGuidGenerator) };
            return this;
        }

        /// <inheritdoc />
        /// <exception cref="RCommonBuilderException">Thrown if a different GUID generator implementation has already been configured.</exception>
        public IRCommonBuilder WithSimpleGuidGenerator()
        {
            if (_guidRegistration.Configured)
            {
                if (_guidRegistration.ImplementationType == typeof(SimpleGuidGenerator))
                {
                    return this;
                }
                throw new RCommonBuilderException(
                    $"IGuidGenerator already configured as '{_guidRegistration.ImplementationType?.FullName}'; " +
                    $"cannot reconfigure as '{typeof(SimpleGuidGenerator).FullName}'. " +
                    "To configure multiple modules consistently, ensure all modules agree on the same IGuidGenerator implementation, " +
                    "or designate a single composition root that performs this registration.");
            }

            this.Services.AddScoped<IGuidGenerator, SimpleGuidGenerator>();
            _guidRegistration = new SingletonRegistration { Configured = true, ImplementationType = typeof(SimpleGuidGenerator) };
            return this;
        }

        /// <inheritdoc />
        public IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions)
        {
            if (_dateTimeRegistration.Configured)
            {
                // Only one impl type exists; always idempotent. Still append the options delegate so
                // additional configuration accumulates per Options pattern.
                this.Services.Configure<SystemTimeOptions>(actions);
                return this;
            }

            this.Services.Configure<SystemTimeOptions>(actions);
            this.Services.AddTransient<ISystemTime, SystemTime>();
            _dateTimeRegistration = new SingletonRegistration { Configured = true, ImplementationType = typeof(SystemTime) };
            return this;
        }

        /// <inheritdoc />
        public IRCommonBuilder WithCommonFactory<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            this.Services.AddTransient<TService, TImplementation>();
            this.Services.AddScoped<Func<TService>>(x => () => x.GetService<TService>()!);
            this.Services.AddScoped<ICommonFactory<TService>, CommonFactory<TService>>();
            return this;
        }

        /// <inheritdoc />
        public virtual IServiceCollection Configure()
        {
            return this.Services;
        }

        /// <inheritdoc />
        public TSubBuilder GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder> factory)
            where TSubBuilder : class
        {
            if (_subBuilderCache.TryGetValue(typeof(TSubBuilder), out var cached))
            {
                return (TSubBuilder)cached;
            }

            var built = factory();
            _subBuilderCache[typeof(TSubBuilder)] = built;
            return built;
        }

        /// <summary>
        /// Walks the sub-builder cache and returns the first cached concrete type that is assignable
        /// to <paramref name="interfaceType"/>, or <c>null</c> if no such cached builder exists.
        /// </summary>
        /// <remarks>
        /// Used by singleton-style WithX verbs defined outside RCommon.Core to detect "different T"
        /// configuration conflicts before delegating to <see cref="GetOrAddBuilder{TSubBuilder}(Func{TSubBuilder})"/>.
        /// </remarks>
        internal Type? TryGetCachedSubBuilderImplementing(Type interfaceType)
        {
            foreach (var key in _subBuilderCache.Keys)
            {
                if (interfaceType.IsAssignableFrom(key))
                {
                    return key;
                }
            }
            return null;
        }

        /// <summary>
        /// Atomically marks the diagnostics scanner as having run. Returns <c>true</c> on first call,
        /// <c>false</c> thereafter. Used by <see cref="RCommonBootstrapDiagnosticsHostedService"/> to
        /// ensure the duplicate-registration scan executes at most once per builder instance.
        /// </summary>
        internal bool TrySetDiagnosticsRun()
        {
            if (_diagnosticsRun) return false;
            _diagnosticsRun = true;
            return true;
        }

        /// <summary>
        /// Stashes the soft-duplicate diagnostic report so it can be retrieved later via
        /// <see cref="GetBootstrapDiagnostics"/> when no <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>
        /// was available to log the warning directly.
        /// </summary>
        internal void StashDiagnostics(string message) => _bootstrapDiagnostics = message;

        /// <inheritdoc />
        public string GetBootstrapDiagnostics() => _bootstrapDiagnostics;
    }
}
