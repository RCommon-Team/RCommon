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
using RCommon.DependencyInjection;

namespace RCommon.Configuration
{
    ///<summary>
    /// Implementation of <see cref="IUnitOfWorkConfiguration"/>.
    ///</summary>
    public class DefaultUnitOfWorkConfiguration : IUnitOfWorkConfiguration
    {
        bool _autoCompleteScope = false;
        IsolationLevel _defaultIsolation = IsolationLevel.ReadCommitted;

        /// <summary>
        /// Configures <see cref="UnitOfWorkScope"/> settings.
        /// </summary>
        /// <param name="containerAdapter">The <see cref="IContainerAdapter"/> instance.</param>
        public void Configure(IContainerAdapter containerAdapter)
        {
            // Transaction Management
            containerAdapter.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            containerAdapter.AddTransient<IUnitOfWorkTransactionManager, UnitOfWorkTransactionManager>();
            UnitOfWorkSettings.AutoCompleteScope = _autoCompleteScope;
            UnitOfWorkSettings.DefaultIsolation = _defaultIsolation;

            // Factory for Unit Of Work
            containerAdapter.AddTransient<IUnitOfWork, UnitOfWork>();
            containerAdapter.AddTransient<Func<IUnitOfWork>>(x => () => x.GetService<IUnitOfWork>());
            containerAdapter.AddTransient<ICommonFactory<IUnitOfWork>, CommonFactory<IUnitOfWork>>();

            // Factory for Unit Of Work Scope
            //containerAdapter.AddTransient<TransactionMode, TransactionMode>();
            containerAdapter.AddTransient<IUnitOfWorkScope, UnitOfWorkScope>();
            containerAdapter.AddTransient<IUnitOfWorkScopeFactory, UnitOfWorkScopeFactory>();



        }

        /// <summary>
        /// Sets <see cref="UnitOfWorkScope"/> instances to auto complete when disposed.
        /// </summary>
        public IUnitOfWorkConfiguration AutoCompleteScope()
        {
            _autoCompleteScope = true;
            return this;
        }

        /// <summary>
        /// Sets the default isolation level used by <see cref="UnitOfWorkScope"/>.
        /// </summary>
        /// <param name="isolationLevel"></param>
        public IUnitOfWorkConfiguration WithDefaultIsolation(IsolationLevel isolationLevel)
        {
            _defaultIsolation = isolationLevel;
            return this;
        }
    }
}