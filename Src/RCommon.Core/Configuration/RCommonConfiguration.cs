#region license
//Copyright 2010 Ritesh Rao 

//Licensed under the Apache License, Version 2.0 (the "License"); 
//you may not use this file except in compliance with the License. 
//You may obtain a copy of the License at 

//http://www.apache.org/licenses/LICENSE-2.0 

//Unless required by applicable law or agreed to in writing, software 
//distributed under the License is distributed on an "AS IS" BASIS, 
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//See the License for the specific language governing permissions and 
//limitations under the License. 
#endregion

using System;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.DependencyInjection;
using MediatR;
using System.Reflection;

namespace RCommon.Configuration
{
    ///<summary>
    /// Default implementation of <see cref="IRCommonConfiguration"/> class.
    ///</summary>
    public class RCommonConfiguration : IRCommonConfiguration
    {
        private readonly IContainerAdapter _containerAdapter;
        public IContainerAdapter ContainerAdapter => _containerAdapter;

        ///<summary>
        /// Default Constructor.
        /// Creates a new instance of the <see cref="RCommonConfiguration"/>  class.
        ///</summary>
        ///<param name="containerAdapter">An instance of <see cref="IContainerAdapter"/> that can be
        /// used to register components.</param>
        public RCommonConfiguration(IContainerAdapter containerAdapter)
        {
            Guard.Against<NullReferenceException>(containerAdapter == null, "IContainerAdapter cannot be null");
            _containerAdapter = containerAdapter;
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
            var configuration = (T) Activator.CreateInstance(typeof (T), new object[] { this.ContainerAdapter });
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
            var configuration = (T) Activator.CreateInstance(typeof (T), new object[] { this.ContainerAdapter });
            actions(configuration);
            configuration.Configure();
            return this;
        }

        public IRCommonConfiguration WithGuidGenerator<T>(Action<SequentialGuidGeneratorOptions> actions) where T : IGuidGenerator
        {
            this.ContainerAdapter.Services.Configure<SequentialGuidGeneratorOptions>(actions);
            this.ContainerAdapter.AddTransient<IGuidGenerator, T>();
            return this;
        }

        public IRCommonConfiguration WithGuidGenerator<T>() where T : IGuidGenerator
        {
            if (typeof(T) == typeof(SequentialGuidGenerator))
            {
                this.ContainerAdapter.Services.Configure<SequentialGuidGeneratorOptions>(x=>x.GetDefaultSequentialGuidType());
            }
            this.ContainerAdapter.AddTransient<IGuidGenerator, T>();
            return this;
        }

        public IRCommonConfiguration WithDateTimeSystem<T>(Action<SystemTimeOptions> actions) where T : ISystemTime
        {
            this.ContainerAdapter.Services.Configure<SystemTimeOptions>(actions);
            this.ContainerAdapter.AddTransient<ISystemTime, SystemTime>();
            return this;
        }


        public IRCommonConfiguration And<T>() where T : IServiceConfiguration
        {
            var configuration = (T)Activator.CreateInstance(typeof(T), new object[] { this.ContainerAdapter });
            configuration.Configure();
            return this;
        }

        public IRCommonConfiguration And<T>(Action<T> actions) where T : IServiceConfiguration
        {
            var configuration = (T)Activator.CreateInstance(typeof(T), new object[] { this.ContainerAdapter });
            actions(configuration);
            configuration.Configure();
            return this;
        }

        public virtual void Configure()
        {
            _containerAdapter.AddTransient<IEnvironmentAccessor, EnvironmentAccessor>();

            // MediaR is a first class citizen in the RCommon Framework
            _containerAdapter.Services.AddMediatR(Assembly.GetEntryAssembly());
        }
    }
}
