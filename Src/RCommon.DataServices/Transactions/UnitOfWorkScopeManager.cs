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
//Original code here: https://github.com/riteshrao/ncommon/blob/v1.2/NCommon/src/Data/UnitOfWorkManager.cs
#endregion

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RCommon.DataServices.Transactions
{

    public class UnitOfWorkScopeManager : DisposableResource, IUnitOfWorkManager
    {
        private bool _disposed = false;
        private ILogger<UnitOfWorkScopeManager> _logger;
        private IUnitOfWork _currentUnitOfWork;


        public UnitOfWorkScopeManager(ILogger<UnitOfWorkScopeManager> logger)
        {
            _logger = logger;
            this.EnlistedTransactions = new ConcurrentDictionary<Guid, IUnitOfWork>();
        }

        public bool EnlistUnitOfWork(IUnitOfWork unitOfWorkScope)
        {
            unitOfWorkScope.ScopeBeginning += OnUnitOfWorkScopeBeginning;
            unitOfWorkScope.ScopeCompleted += OnUnitOfWorkScopeCompleted;
            return this.EnlistedTransactions.TryAdd(unitOfWorkScope.TransactionId, unitOfWorkScope);
        }

        private void OnUnitOfWorkScopeCompleted(IUnitOfWork unitOfWorkScope)
        {
            this.EnlistedTransactions.TryRemove(unitOfWorkScope.TransactionId, out _);
            this._logger.LogDebug("UnitOfWorkScope {0} Removed from enlisted transactions", unitOfWorkScope.TransactionId);
        }

        private void OnUnitOfWorkScopeBeginning(IUnitOfWork unitOfWorkScope)
        {
            this._currentUnitOfWork = unitOfWorkScope;
        }

        /// <summary>
        /// Gets the current <see cref="IUnitOfWork"/> instance.
        /// </summary>
        public IUnitOfWork CurrentUnitOfWork
        {
            get
            {
                return this._currentUnitOfWork;
            }
        }

        public ConcurrentDictionary<Guid, IUnitOfWork> EnlistedTransactions { get; }

        protected override void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            if (disposing)
            {
                this.EnlistedTransactions.Clear();
                this._logger.LogDebug("UnitOfWorkScopeManager has removed all enlisted transactions");
                this._disposed = true;
            }
        }
    }
}
