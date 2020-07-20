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
using Microsoft.EntityFrameworkCore;
using RCommon.Configuration;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection;
using RCommon.Domain.Repositories;
using RCommon.StateStorage;

namespace RCommon.ObjectAccess.EFCore
{
    /// <summary>
    /// Implementation of <see cref="IObjectAccessConfiguration"/> for Entity Framework.
    /// </summary>
    public class EFConfiguration : IObjectAccessConfiguration
    {
        EFUnitOfWorkFactory _factory;



        /// <summary>
        /// Configures unit of work instances to use the specified <see cref="DbContext"/>.
        /// </summary>
        /// <param name="objectContextProvider">A <see cref="Func{T}"/> of type <see cref="ObjectContext"/>
        /// that can be used to construct <see cref="DbContext"/> instances.</param>
        /// <returns><see cref="EFConfiguration"/></returns>
        public IObjectAccessConfiguration WithObjectContext(Func<DbContext> objectContextProvider)
        {
            Guard.Against<ArgumentNullException>(objectContextProvider == null,
                                                 "Expected a non-null Func<ObjectContext> instance.");
            var stateStorage = ServiceLocatorWorker.GetInstance<IStateStorage>();

            Guard.Against<ArgumentNullException>(stateStorage == null, "IStateStorage must be set prior to storing a DbContext object in the application.");

            _factory = new EFUnitOfWorkFactory(new EFObjectSourceLifetimeManager(stateStorage));
            _factory.RegisterObjectSource(objectContextProvider);
            return this;
        }

        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance that allows
        /// registering components.</param>
        public void Configure(IContainerAdapter containerAdapter)
        {
            containerAdapter.RegisterInstance<IUnitOfWorkFactory>(_factory);
            containerAdapter.RegisterGeneric(typeof(IEagerFetchingRepository<,>), typeof(EFCoreRepository<,>));
            containerAdapter.Register<ObjectSourceLifetimeManager<DbContext>, EFObjectSourceLifetimeManager>();
        }


       
    }
}