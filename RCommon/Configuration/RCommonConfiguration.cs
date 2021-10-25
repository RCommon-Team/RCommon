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
using RCommon;
using RCommon.DependencyInjection;

namespace RCommon.Configuration
{
    ///<summary>
    /// Default implementation of <see cref="IRCommonConfiguration"/> class.
    ///</summary>
    public class RCommonConfiguration : IRCommonConfiguration
    {
        readonly IContainerAdapter _containerAdapter;

        ///<summary>
        /// Default Constructor.
        /// Creates a new instance of the <see cref="RCommonConfiguration"/>  class.
        ///</summary>
        ///<param name="containerAdapter">An instance of <see cref="IContainerAdapter"/> that can be
        /// used to register components.</param>
        public RCommonConfiguration(IContainerAdapter containerAdapter)
        {
            _containerAdapter = containerAdapter;
            InitializeDefaults();
        }

        /// <summary>
        /// Registers default components for RCommon.
        /// </summary>
        private void InitializeDefaults()
        {
            _containerAdapter.AddTransient<IEnvironmentAccessor, EnvironmentAccessor>();
        }

        /// <summary>
        /// Configure RCommon state storage using a <see cref="IStateStorageConfiguration"/> instance.
        /// </summary>
        /// <typeparam name="T">A <see cref="IStateStorageConfiguration"/> type that can be used to configure
        /// state storage services exposed by RCommon.
        /// </typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IRCommonConfiguration WithStateStorage<T>() where T : IStateStorageConfiguration, new()
        {
            var configuration = (T) Activator.CreateInstance(typeof (T));
            configuration.Configure(_containerAdapter);
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
        public IRCommonConfiguration WithStateStorage<T>(Action<T> actions) where T : IStateStorageConfiguration, new()
        {
            var configuration = (T) Activator.CreateInstance(typeof (T));
            actions(configuration);
            configuration.Configure(_containerAdapter);
            return this;
        }

        

        
        /// <summary>
        /// Configures RCommon unit of work settings.
        /// </summary>
        /// <typeparam name="T">A <see cref="IUnitOfWorkConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        public IRCommonConfiguration WithExceptionHandling<T>() where T : IExceptionHandlingConfiguration, new()
        {
            var exHandling = (T)Activator.CreateInstance(typeof(T));
            exHandling.Configure(_containerAdapter);
            return this;
        }

        ///<summary>
        /// Configures RCommon unit of work settings.
        ///</summary>
        /// <typeparam name="T">A <see cref="IRCommonConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        ///<param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IUnitOfWorkConfiguration"/> instance.</param>
        ///<returns><see cref="IRCommonConfiguration"/></returns>
        public IRCommonConfiguration WithExceptionHandling<T>(Action<T> actions) where T : IExceptionHandlingConfiguration, new()
        {
            var exHandling = (T)Activator.CreateInstance(typeof(T));
            actions(exHandling);
            exHandling.Configure(_containerAdapter);
            return this;
        }

        public IRCommonConfiguration And<T>() where T : IServiceConfiguration, new()
        {
            var configuration = (T)Activator.CreateInstance(typeof(T));
            configuration.Configure(_containerAdapter);
            return this;
        }

        public IRCommonConfiguration And<T>(Action<T> actions) where T : IServiceConfiguration, new()
        {
            var configuration = (T)Activator.CreateInstance(typeof(T));
            actions(configuration);
            configuration.Configure(_containerAdapter);
            return this;
        }
    }
}