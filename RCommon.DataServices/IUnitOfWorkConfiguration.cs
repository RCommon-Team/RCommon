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
using System.Transactions;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection;

namespace RCommon.DataServices
{
    /// <summary>
    /// Configuration settings for <see cref="UnitOfWorkScope"/> instances in RCommon.
    /// </summary>
    public interface IUnitOfWorkConfiguration
    {
        /// <summary>
        /// Configures <see cref="UnitOfWorkScope"/> settings.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance.</param>
        void Configure(IContainerAdapter containerAdapter);

        /// <summary>
        /// Sets <see cref="UnitOfWorkScope"/> instances to auto complete when disposed.
        /// </summary>
        IUnitOfWorkConfiguration AutoCompleteScope();

        /// <summary>
        /// Sets the default isolation level used by <see cref="UnitOfWorkScope"/>.
        /// </summary>
        /// <param name="isolationLevel"></param>
        IUnitOfWorkConfiguration WithDefaultIsolation(IsolationLevel isolationLevel);


        /// <summary>
        /// Configures RCommon unit of work settings.
        /// </summary>
        /// <typeparam name="T">A <see cref="IUnitOfWorkConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        /// <returns><see cref="IRCommonConfiguration"/></returns>
        IUnitOfWorkConfiguration WithUnitOfWork<T>() where T : IUnitOfWorkConfiguration, new();

        ///<summary>
        /// Configures RCommon unit of work settings.
        ///</summary>
        /// <typeparam name="T">A <see cref="IRCommonConfiguration"/> type that can be used to configure
        /// unit of work settings.</typeparam>
        ///<param name="actions">An <see cref="Action{T}"/> delegate that can be used to perform
        /// custom actions on the <see cref="IUnitOfWorkConfiguration"/> instance.</param>
        ///<returns><see cref="IRCommonConfiguration"/></returns>
        IUnitOfWorkConfiguration WithUnitOfWork<T>(Action<T> actions) where T : IUnitOfWorkConfiguration, new();
    }
}