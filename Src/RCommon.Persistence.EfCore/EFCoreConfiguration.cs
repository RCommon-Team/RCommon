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
using RCommon.StateStorage;

namespace RCommon.Persistence.EFCore
{
    /// <summary>
    /// Implementation of <see cref="IEFCoreConfiguration"/> for Entity Framework.
    /// </summary>
    public class EFCoreConfiguration : RCommonConfiguration, IEFCoreConfiguration
    {
        private List<string> _dbContextTypes = new List<string>();


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
            this.ContainerAdapter.AddGeneric(typeof(ILinqMapperRepository<>), typeof(EFCoreRepository<>));
            this.ContainerAdapter.AddGeneric(typeof(IEagerFetchingRepository<>), typeof(EFCoreRepository<>));

            // Registered DbContexts
            foreach (var dbContext in _dbContextTypes)
            {
                this.ContainerAdapter.AddTransient(Type.GetType(dbContext), Type.GetType(dbContext));
            }
        }


        public IEFCoreConfiguration UsingDbContext<TDbContext>()
            where TDbContext : RCommonDbContext
        {
            string dbContext = typeof(TDbContext).AssemblyQualifiedName;
            _dbContextTypes.Add(dbContext);

            return this;
        }
    }
}
