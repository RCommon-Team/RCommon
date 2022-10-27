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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RCommon.Extensions;

namespace RCommon.DataServices.Transactions
{
    /// <summary>
    /// Default implementation of <see cref="IUnitOfWorkTransactionManager"/> interface.
    /// </summary>
    public class UnitOfWorkTransactionManager : DisposableResource, IUnitOfWorkTransactionManager
    {
        bool _disposed;
        readonly Guid _transactionManagerId = Guid.NewGuid();
        readonly ILogger<UnitOfWorkTransactionManager> _logger;
        readonly LinkedList<UnitOfWorkTransaction> _transactions = new LinkedList<UnitOfWorkTransaction>();
        readonly ICommonFactory<IUnitOfWork> _unitOfWorkFactory;

        /// <summary>
        /// Default Constructor.
        /// Creates a new instance of the <see cref="UnitOfWorkTransactionManager"/> class.
        /// </summary>
        public UnitOfWorkTransactionManager(ILogger<UnitOfWorkTransactionManager> logger, ICommonFactory<IUnitOfWork> unitOfWorkFactory)
        {
            _logger = logger;
            _unitOfWorkFactory = unitOfWorkFactory;
            _logger.LogDebug("New instance of TransactionManager with Id {0} created.", _transactionManagerId);
        }

        /// <summary>
        /// Gets the current <see cref="IUnitOfWork"/> instance.
        /// </summary>
        public IUnitOfWork CurrentUnitOfWork
        {
            get 
            {
                return CurrentTransaction == null ? null : CurrentTransaction.UnitOfWork;
            }
        }

        /// <summary>
        /// Gets the current <see cref="UnitOfWorkTransaction"/> instance.
        /// </summary>
        public UnitOfWorkTransaction CurrentTransaction
        {
            get
            {
                return _transactions.Count == 0 ? null : _transactions.First.Value;
            }
        }


        /// <summary>
        /// Enlists a <see cref="UnitOfWorkScope"/> instance with the transaction manager,
        /// with the specified transaction mode.
        /// </summary>
        /// <param name="scope">The <see cref="IUnitOfWorkScope"/> to register.</param>
        /// <param name="mode">A <see cref="TransactionMode"/> enum specifying the transaciton
        /// mode of the unit of work.</param>
        public void EnlistScope(IUnitOfWorkScope scope, TransactionMode mode)
        {
            _logger.LogInformation("Enlisting scope {0} with transaction manager {1} with transaction mode {2}",
                                scope.ScopeId,
                                _transactionManagerId,
                                mode);

            if (_transactions.Count == 0 ||
                mode == TransactionMode.New ||
                mode == TransactionMode.Supress)
            {
                _logger.LogDebug("Enlisting scope {0} with mode {1} requires a new TransactionScope to be created.", scope.ScopeId, mode);
                var txScope = TransactionScopeHelper.CreateScope(_logger, UnitOfWorkSettings.DefaultIsolation, mode);
                var unitOfWork = _unitOfWorkFactory.Create();
                var transaction = new UnitOfWorkTransaction(_logger, unitOfWork, txScope);
                transaction.TransactionDisposing += OnTransactionDisposing;
                transaction.EnlistScope(scope);
                _transactions.AddFirst(transaction);
                return;
            }
            CurrentTransaction.EnlistScope(scope);
        }

        

        /// <summary>
        /// Handles a Dispose signal from a transaction.
        /// </summary>
        /// <param name="transaction"></param>
        void OnTransactionDisposing(UnitOfWorkTransaction transaction)
        {
            _logger.LogInformation("UnitOfWorkTransaction {0} signalled a disposed. Unregistering transaction from TransactionManager {1}",
                                    transaction.TransactionId, _transactionManagerId);

            transaction.TransactionDisposing -= OnTransactionDisposing;
            var node = _transactions.Find(transaction);
            if (node != null)
                _transactions.Remove(node);
        }

        /// <summary>
        /// Internal dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _logger.LogInformation("Disposing off transction manager {0}", _transactionManagerId);
                if (_transactions != null && _transactions.Count > 0)
                {
                    _transactions.ForEach(tx =>
                    {
                        tx.TransactionDisposing -= OnTransactionDisposing;
                        tx.Dispose();
                    });
                    _transactions.Clear();
                }
            }
            _disposed = true;
        }
    }
}