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

namespace RCommon.Configuration
{
    /// <summary>
    /// Configuration interface exposed by RCommon to configure different services exposed by RCommon.
    /// </summary>
    public interface IRCommonConfiguration
    {
        /// <summary>
        /// Configure RCommon state storage using a <see cref="IStateStorageConfiguration"/> instance.
        /// </summary>
        /// <typeparam name="T">A <see cref="IStateStorageConfiguration"/> type that can be used to configure
        /// state storage services exposed by RCommon.
        /// </typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithStateStorage<T>() where T : IStateStorageConfiguration, new();

        /// <summary>
        /// Configure RCommon state storage using a <see cref="IStateStorageConfiguration"/> instance.
        /// </summary>
        /// <typeparam name="T">A <see cref="IStateStorageConfiguration"/> type that can be used to configure
        /// state storage services exposed by RCommon.
        /// </typeparam>
        /// <param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IStateStorageConfiguration"/> instance.</param>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithStateStorage<T>(Action<T> actions) where T : IStateStorageConfiguration, new();

        /// <summary>
        /// Configure data providers used by RCommon.
        /// </summary>
        /// <typeparam name="T">A <see cref="IObjectAccessConfiguration"/> type that can be used to configure
        /// data providers for RCommon.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithObjectAccess<T>() where T : IObjectAccessConfiguration, new();

        /// <summary>
        /// Configure data providers used by RCommon.
        /// </summary>
        /// <typeparam name="T">A <see cref="IObjectAccessConfiguration"/> type that can be used to configure
        /// data providers for RCommon.</typeparam>
        /// <param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IObjectAccessConfiguration"/> instance.</param>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithObjectAccess<T>(Action<T> actions) where T : IObjectAccessConfiguration, new();

        /// <summary>
        /// Configures RCommon unit of work settings.
        /// </summary>
        /// <typeparam name="T">A <see cref="IUnitOfWorkConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithUnitOfWork<T> () where T : IUnitOfWorkConfiguration, new();

        ///<summary>
        /// Configures RCommon unit of work settings.
        ///</summary>
        /// <typeparam name="T">A <see cref="IRCommonConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        ///<param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IUnitOfWorkConfiguration"/> instance.</param>
        ///<returns><see cref="IRCommonConfiguration"/></returns>
        IRCommonConfiguration WithUnitOfWork<T>(Action<T> actions) where T : IUnitOfWorkConfiguration, new();

       /* IRCommonConfiguration WithExceptionHandling<T>() where T : IExceptionHandlingConfiguration, new();

        IRCommonConfiguration WithExceptionHandling<T>(Action<T> actions) where T : IExceptionHandlingConfiguration, new();*/

        IRCommonConfiguration And<T>() where T : IServiceConfiguration, new();

        IRCommonConfiguration And<T>(Action<T> actions) where T : IServiceConfiguration, new();
    }
}