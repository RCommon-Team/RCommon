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
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.StateStorage;

namespace RCommon
{
    /// <summary>
    /// Implementation of <see cref="IEFCoreConfiguration"/> for Entity Framework.
    /// </summary>
    public class EFCoreConfiguration : IEFCoreConfiguration
    {
        private readonly IServiceCollection _services;

        public EFCoreConfiguration(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // EF Core Repository
            services.AddTransient(typeof(IFullFeaturedRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IReadOnlyRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IWriteOnlyRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IGraphRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(ILinqRepository<>), typeof(EFCoreRepository<>));
            services.AddTransient(typeof(IEagerFetchingRepository<>), typeof(EFCoreRepository<>));
        }


        public IEFCoreConfiguration AddDbContext<TDbContext>(Action<DbContextOptionsBuilder>? options = null)
            where TDbContext : RCommonDbContext
        {
            this._services.AddDbContext<TDbContext>(options, ServiceLifetime.Scoped); 

            return this;
        }

        public IObjectAccessConfiguration SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            this._services.Configure(options);
            return this;
        }
    }
}
