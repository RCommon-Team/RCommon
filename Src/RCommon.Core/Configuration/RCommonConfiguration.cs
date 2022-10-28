using System;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;

namespace RCommon
{
    ///<summary>
    /// Default implementation of <see cref="IRCommonConfiguration"/> class.
    ///</summary>
    public class RCommonConfiguration : IRCommonConfiguration
    {
        public IServiceCollection Services { get; }

        private bool _guidConfigured = false;
        private bool _dateTimeConfigured = false;

        public RCommonConfiguration(IServiceCollection services)
        {
            Guard.Against<NullReferenceException>(services == null, "IServiceCollection cannot be null");
            Services = services;

            this.Services.AddMediatR(Assembly.GetEntryAssembly()); // MediaR is a first class citizen in the RCommon Framework
        }

        public IRCommonConfiguration WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions)
        {
            Guard.Against<RCommonConfigurationException>(this._guidConfigured,
                "Guid Generator has already been configured once. You cannot configure multiple times");
            this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
            this.Services.AddTransient<IGuidGenerator, SequentialGuidGenerator>();
            this._guidConfigured = true;
            return this;
        }

        public IRCommonConfiguration WithSimpleGuidGenerator()
        {
            Guard.Against<RCommonConfigurationException>(this._guidConfigured,
                "Guid Generator has already been configured once. You cannot configure multiple times");
            this.Services.AddTransient<IGuidGenerator, SimpleGuidGenerator>();
            this._guidConfigured = true;
            return this;
        }

        public IRCommonConfiguration WithDateTimeSystem(Action<SystemTimeOptions> actions)
        {
            Guard.Against<RCommonConfigurationException>(this._dateTimeConfigured,
                "Date/Time System has already been configured once. You cannot configure multiple times");
            this.Services.Configure<SystemTimeOptions>(actions);
            this.Services.AddTransient<ISystemTime, SystemTime>();
            this._dateTimeConfigured = true;
            return this;
        }

        public virtual IServiceCollection Configure()
        {
            return this.Services;
        }
    }
}
