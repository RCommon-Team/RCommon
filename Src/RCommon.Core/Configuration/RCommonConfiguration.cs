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

        public RCommonConfiguration(IServiceCollection services)
        {
            Guard.Against<NullReferenceException>(services == null, "IServiceCollection cannot be null");
            Services = services;
        }

        /// <summary>
        /// Configure RCommon state storage using a <see cref="IStateStorageConfiguration"/> instance.
        /// </summary>
        /// <typeparam name="T">A <see cref="IStateStorageConfiguration"/> type that can be used to configure
        /// state storage services exposed by RCommon.
        /// </typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IRCommonConfiguration WithStateStorage<T>() where T : IStateStorageConfiguration
        {
            var configuration = (T) Activator.CreateInstance(typeof (T), new object[] { this.Services });
            configuration.Configure();
            return this;
        }

        /// <summary>
        /// Configure RCommon state storage using a <see cref="IStateStorageConfiguration"/> instance.
        /// </summary>
        /// <typeparam name="T">A <see cref="IStateStorageConfiguration"/> type that can be used to configure
        /// state storage services exposed by RCommon.
        /// </typeparam>
        /// <param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IStateStorageConfiguration"/> instance.</param>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IRCommonConfiguration WithStateStorage<T>(Action<T> actions) where T : IStateStorageConfiguration
        {
            var configuration = (T) Activator.CreateInstance(typeof (T), new object[] { this.Services });
            actions(configuration);
            configuration.Configure();
            return this;
        }

        public IRCommonConfiguration WithGuidGenerator(IGuidGenerator guidGenerator, Action<SequentialGuidGeneratorOptions> actions)
        {
            this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
            this.Services.AddTransient(typeof(IGuidGenerator), guidGenerator.GetType());
            return this;
        }

        public IRCommonConfiguration WithGuidGenerator<T>() where T : IGuidGenerator
        {
            if (typeof(T) == typeof(SequentialGuidGenerator))
            {
                this.Services.Configure<SequentialGuidGeneratorOptions>(x=>x.GetDefaultSequentialGuidType());
            }
            this.Services.AddTransient<IGuidGenerator, T>();
            return this;
        }

        public IRCommonConfiguration WithDateTimeSystem<T>(Action<SystemTimeOptions> actions) where T : ISystemTime
        {
            this.Services.Configure<SystemTimeOptions>(actions);
            this.Services.AddTransient<ISystemTime, T>();
            return this;
        }

        public virtual void Configure()
        {
            this.Services.AddTransient<IEnvironmentAccessor, EnvironmentAccessor>();

            // MediaR is a first class citizen in the RCommon Framework
            this.Services.AddMediatR(Assembly.GetEntryAssembly());
        }
    }
}
