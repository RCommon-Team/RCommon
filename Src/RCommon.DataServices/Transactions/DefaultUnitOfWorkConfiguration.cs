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
using System.ComponentModel;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.DataServices.Transactions;

namespace RCommon.DataServices.Transactions
{
    ///<summary>
    /// Implementation of <see cref="IUnitOfWorkConfiguration"/>.
    ///</summary>
    public class DefaultUnitOfWorkConfiguration : IUnitOfWorkConfiguration
    {
        private readonly IServiceCollection _services;

        public DefaultUnitOfWorkConfiguration(IServiceCollection services)
        {
            // Data Store Management
            services.AddScoped<IDataStoreEnlistmentProvider, ScopedDataStoreEnlistmentProvider>();

            // Transaction Management
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkScopeManager>();

            // Factory for Unit Of Work Scope
            services.AddTransient<IUnitOfWork, UnitOfWorkScope>();
            services.AddTransient<IUnitOfWorkFactory, UnitOfWorkFactory>();
            _services = services;
        }

        public IUnitOfWorkConfiguration SetOptions(Action<UnitOfWorkSettings> unitOfWorkOptions)
        {
            this._services.Configure<UnitOfWorkSettings>(unitOfWorkOptions);
            return this;
        }
    }
}
