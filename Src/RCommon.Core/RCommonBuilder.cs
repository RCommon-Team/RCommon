using System;
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

        private bool _guidConfigured = false;
        private bool _dateTimeConfigured = false;

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
                return new InMemoryEventBus(sp, Services);
            });
            Services.AddScoped<IEventRouter, InMemoryTransactionalEventRouter>();
        }

        /// <inheritdoc />
        /// <exception cref="RCommonBuilderException">Thrown if a GUID generator has already been configured.</exception>
        public IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions)
        {
            Guard.Against<RCommonBuilderException>(this._guidConfigured,
                "Guid Generator has already been configured once. You cannot configure multiple times");
            this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
            this.Services.AddTransient<IGuidGenerator, SequentialGuidGenerator>();
            this._guidConfigured = true;
            return this;
        }

        /// <inheritdoc />
        /// <exception cref="RCommonBuilderException">Thrown if a GUID generator has already been configured.</exception>
        public IRCommonBuilder WithSimpleGuidGenerator()
        {
            Guard.Against<RCommonBuilderException>(this._guidConfigured,
                "Guid Generator has already been configured once. You cannot configure multiple times");
            this.Services.AddScoped<IGuidGenerator, SimpleGuidGenerator>();
            this._guidConfigured = true;
            return this;
        }

        /// <inheritdoc />
        /// <exception cref="RCommonBuilderException">Thrown if the date/time system has already been configured.</exception>
        public IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions)
        {
            Guard.Against<RCommonBuilderException>(this._dateTimeConfigured,
                "Date/Time System has already been configured once. You cannot configure multiple times");
            this.Services.Configure<SystemTimeOptions>(actions);
            this.Services.AddTransient<ISystemTime, SystemTime>();
            this._dateTimeConfigured = true;
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
    }
}
