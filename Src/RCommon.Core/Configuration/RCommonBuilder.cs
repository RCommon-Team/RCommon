using System;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace RCommon
{
    ///<summary>
    /// Default implementation of <see cref="IRCommonBuilder"/> class.
    ///</summary>
    public class RCommonBuilder : IRCommonBuilder
    {
        public IServiceCollection Services { get; }

        private bool _guidConfigured = false;
        private bool _dateTimeConfigured = false;

        public RCommonBuilder(IServiceCollection services)
        {
            Guard.Against<NullReferenceException>(services == null, "IServiceCollection cannot be null");
            Services = services;
        }

        public IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions)
        {
            Guard.Against<RCommonBuilderException>(this._guidConfigured,
                "Guid Generator has already been configured once. You cannot configure multiple times");
            this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
            this.Services.AddTransient<IGuidGenerator, SequentialGuidGenerator>();
            this._guidConfigured = true;
            return this;
        }

        public IRCommonBuilder WithSimpleGuidGenerator()
        {
            Guard.Against<RCommonBuilderException>(this._guidConfigured,
                "Guid Generator has already been configured once. You cannot configure multiple times");
            this.Services.AddTransient<IGuidGenerator, SimpleGuidGenerator>();
            this._guidConfigured = true;
            return this;
        }

        public IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions)
        {
            Guard.Against<RCommonBuilderException>(this._dateTimeConfigured,
                "Date/Time System has already been configured once. You cannot configure multiple times");
            this.Services.Configure<SystemTimeOptions>(actions);
            this.Services.AddTransient<ISystemTime, SystemTime>();
            this._dateTimeConfigured = true;
            return this;
        }

        public IRCommonBuilder WithCommonFactory<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            this.Services.AddTransient<TService, TImplementation>();
            this.Services.AddScoped<Func<TService>>(x => () => x.GetService<TService>());
            this.Services.AddScoped<ICommonFactory<TService>, CommonFactory<TService>>();
            return this;
        }

        public virtual IServiceCollection Configure()
        {
            return this.Services;
        }
    }
}
