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
using RCommon.DependencyInjection;
using RCommon.StateStorage;
using RCommon.StateStorage.AspNetCore;

namespace RCommon.Configuration
{
    /// <summary>
    /// Default implementation of <see cref="IStateStorageConfiguration"/> that allows configuring
    /// state storage in RCommon.
    /// </summary>
    public class DefaultStateStorageConfiguration : RCommonConfiguration, IStateStorageConfiguration
    {
        Type _customSessionType;
        Type _customLocalStateType;
        Type _customApplicationStateType;

        public DefaultStateStorageConfiguration(IContainerAdapter containerAdapter):base(containerAdapter)
        {

        }

        

        /// <summary>
        /// Instructs RCommon to use a custom <see cref="ISessionState"/> type as the session state storage.
        /// </summary>
        /// <typeparam name="T">A type that implements the <see cref="ISessionState"/> interface.</typeparam>
        /// <returns>The <see cref="DefaultStateStorageConfiguration"/> instance</returns>
        public DefaultStateStorageConfiguration UseCustomSessionStateOf<T>() where T : ISessionState
        {
            _customSessionType = typeof (T);
            return this;
        }

        /// <summary>
        /// Instructs RCommon to use a custom <see cref="IContextState"/> type as the local state storage.
        /// </summary>
        /// <typeparam name="T">A type that implements the <see cref="IContextState"/> interface.</typeparam>
        /// <returns>The <see cref="DefaultStateStorageConfiguration"/> instance.</returns>
        public DefaultStateStorageConfiguration UseCustomLocalStateOf<T>() where T : IContextState
        {
            _customLocalStateType = typeof (T);
            return this;
        }

        /// <summary>
        /// Instructs RCommon to use a custom <see cref="IApplicationState"/> type as the application stage storage.
        /// </summary>
        /// <typeparam name="T">A type that implements the <see cref="IApplicationState"/> interface.</typeparam>
        /// <returns>The <see cref="DefaultStateStorageConfiguration"/> instance.</returns>
        public DefaultStateStorageConfiguration UseCustomApplicationStateOf<T>() where T : IApplicationState
        {
            _customApplicationStateType = typeof (T);
            return this;
        }

        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure state storage.
        /// </summary>
        public override void Configure()
        {
            if (_customSessionType != null)
                this.ContainerAdapter.AddTransient(typeof(ISessionState), _customSessionType);
            else
            {
                this.ContainerAdapter.AddTransient<ISessionStateSelector, DefaultSessionStateSelector>();
                this.ContainerAdapter.AddTransient<ISessionState, SessionStateWrapper>();
            }

            if (_customLocalStateType != null)
                this.ContainerAdapter.AddTransient(typeof(IContextState), _customLocalStateType);
            else
            {
                this.ContainerAdapter.AddTransient<IContextStateSelector, DefaultContextStateSelector>();
                this.ContainerAdapter.AddTransient<IContextState, ContextStateWrapper>();
            }
           
            if (_customApplicationStateType != null)
                this.ContainerAdapter.AddSingleton(typeof(IApplicationState), _customApplicationStateType);
            else
                this.ContainerAdapter.AddSingleton<IApplicationState, ApplicationState>();

            this.ContainerAdapter.AddTransient<IStateStorage, StateStorageWrapper>();
        }
    }
}