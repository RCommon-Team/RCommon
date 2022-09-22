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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Configuration;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.StateStorage;

namespace RCommon
{
    /// <summary>
    /// Implementation of <see cref="IEFCoreConfiguration"/> for Entity Framework.
    /// </summary>
    public class EFCoreConfiguration : RCommonConfiguration, IEFCoreConfiguration
    {


        public EFCoreConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

        }


        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        public override void Configure()
        {

            // EF Core Repository
            this.ContainerAdapter.AddGeneric(typeof(IFullFeaturedRepository<>), typeof(EFCoreRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IReadOnlyRepository<>), typeof(EFCoreRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IWriteOnlyRepository<>), typeof(EFCoreRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IGraphRepository<>), typeof(EFCoreRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(ILinqRepository<>), typeof(EFCoreRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IEagerFetchingRepository<>), typeof(EFCoreRepository<>));
        }


        public IEFCoreConfiguration AddDbContext<TDbContext>(Action<DbContextOptionsBuilder>? options = null)
            where TDbContext : RCommonDbContext
        {
            // TODO: Should this be a factory so that we don't interfere with other uses of this DbContext?
            // Transient due to RCommon DataStoreProvider storing as scoped
            this.ContainerAdapter.Services.AddDbContext<TDbContext>(options, ServiceLifetime.Transient); 

            return this;
        }
    }
}
