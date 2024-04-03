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
#region license compliance
//Substantial changes to the original code have been made in the form of namespace reorganization, 
//dependency injection API updates, and configuration initialization.
//Original code here: https://github.com/riteshrao/ncommon/blob/v1.2/NCommon/src/Configuration/DefaultUnitOfWorkConfiguration.cs
#endregion

using System;
using System.ComponentModel;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace RCommon.Persistence.Transactions
{
    ///<summary>
    /// Implementation of <see cref="IUnitOfWorkBuilder"/>.
    ///</summary>
    public class DefaultUnitOfWorkBuilder : IUnitOfWorkBuilder
    {
        private readonly IServiceCollection _services;

        public DefaultUnitOfWorkBuilder(IServiceCollection services)
        {
            // Data Store Management
            services.AddScoped<IDataStoreEnlistmentProvider, ScopedDataStoreEnlistmentProvider>();

            // Transaction Management
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkScopeManager>();

            // Factory for Unit Of Work Scope
            services.AddScoped<IUnitOfWork, UnitOfWorkScope>();
            services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
            _services = services;
        }

        public IUnitOfWorkBuilder SetOptions(Action<UnitOfWorkSettings> unitOfWorkOptions)
        {
            _services.Configure(unitOfWorkOptions);
            return this;
        }
    }
}
