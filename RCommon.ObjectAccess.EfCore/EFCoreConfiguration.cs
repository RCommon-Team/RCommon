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
using RCommon.Configuration;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.DependencyInjection;
using RCommon.Domain.Repositories;
using RCommon.StateStorage;

namespace RCommon.ObjectAccess.EFCore
{
    /// <summary>
    /// Implementation of <see cref="IObjectAccessConfiguration"/> for Entity Framework.
    /// </summary>
    public class EFCoreConfiguration : IObjectAccessConfiguration
    {
        private List<string> _dbContextTypes = new List<string>();

        /// <summary>
        /// Called by RCommon <see cref="Configure"/> to configure data providers.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance that allows
        /// registering components.</param>
        public void Configure(IContainerAdapter containerAdapter)
        {

            containerAdapter.AddGeneric(typeof(IEFCoreRepository<>), typeof(EFCoreRepository<,>));
            containerAdapter.AddGeneric(typeof(IEagerFetchingRepository<>), typeof(EFCoreRepository<,>));

            foreach (var dbContext in _dbContextTypes)
            {
                containerAdapter.AddTransient(typeof(RCommonDbContext), Type.GetType(dbContext));
            }
            
            
        }


        public IObjectAccessConfiguration UsingDbContext<TDbContext>()
            where TDbContext : RCommonDbContext
        {
            var dbContext = typeof(TDbContext).AssemblyQualifiedName;
            _dbContextTypes.Add(dbContext);

            //_dbContextType = Activator.CreateInstance(typeof(TDbContext));

            return this;
        }
    }
}