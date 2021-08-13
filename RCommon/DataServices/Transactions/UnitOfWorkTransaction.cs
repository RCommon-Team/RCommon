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
// Substantial changes by for RCommon Project http://reactor2.com/rcommon

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using RCommon.Extensions;

namespace RCommon.DataServices.Transactions
{
    /// <summary>
    /// Encapsulates a unit of work transaction.
    /// </summary>
    public class UnitOfWorkTransaction : DisposableResource
    {
        bool _disposed;
        TransactionScope _transaction;
        IUnitOfWork _unitOfWork;
        IList<IUnitOfWorkScope> _attachedScopes = new List<IUnitOfWorkScope>();

        readonly Guid _transactionId = Guid.NewGuid();
        readonly ILogger<UnitOfWorkTransactionManager> _logger;
        

        ///<summary>
        /// Raised when the transaction is disposing.
        ///</summary>
        public event Action<UnitOfWorkTransaction> TransactionDisposing;

        ///<summary>
        /// Default Constructor.
        /// Creates a new instance of the <see cref="UnitOfWorkTransaction"/> class.
        ///</summary>
        ///<param name="unitOfWork">The <see cref="IUnitOfWork"/> instance managed by the 
        /// <see cref="UnitOfWorkTransaction"/> instance.</param>
        ///<param name="transaction">The <see cref="TransactionScope"/> instance managed by 
        /// the <see cref="UnitOfWorkTransaction"/> instance.</param>
        public UnitOfWorkTransaction(ILogger<UnitOfWorkTransactionManager> logger, IUnitOfWork unitOfWork, TransactionScope transaction)
        {
            Guard.Against<ArgumentNullException>(unitOfWork == null,
                                                 "Expected a non-null UnitOfWork instance.");
            Guard.Against<ArgumentNullException>(transaction == null,
                                                 "Expected a non-null TransactionScope instance.");
            _unitOfWork = unitOfWork;
            _transaction = transaction;
            _logger = logger;
            _logger.LogInformation("New UnitOfWorkTransction created with Id {0}", _transactionId);
        }

        ///<summary>
        /// Gets the unique transaction id of the <see cref="UnitOfWorkTransaction"/> instance.
        ///</summary>
        /// <value>A <see cref="Guid"/> representing the unique id of the <see cref="UnitOfWorkTransaction"/> instance.</value>
        public Guid TransactionId
        {
            get { return _transactionId; }
        }

        /// <summary>
        /// Gets the <see cref="IUnitOfWork"/> instance managed by the 
        /// <see cref="UnitOfWorkTransaction"/> instance.
        /// </summary>
        public IUnitOfWork UnitOfWork
        {
            get { return _unitOfWork; }
        }

        /// <summary>
        /// Attaches a <see cref="UnitOfWorkScope"/> instance to the 
        /// <see cref="UnitOfWorkTransaction"/> instance.
        /// </summary>
        /// <param name="scope">The <see cref="UnitOfWorkScope"/> instance to attach.</param>
        public void EnlistScope(IUnitOfWorkScope scope)
        {
            Guard.Against<ArgumentNullException>(scope == null, "Expected a non-null IUnitOfWorkScope instance.");

            _logger.LogInformation("Scope {1} enlisted with transaction {1}", scope.ScopeId, _transactionId);
            _attachedScopes.Add(scope);
            this.UnitOfWork.TransactionId = _transactionId;
            scope.ScopeComitting += OnScopeCommitting;
            scope.ScopeRollingback += OnScopeRollingBack;

        }


        

        /// <summary>
        /// Callback executed when an enlisted scope has comitted.
        /// </summary>
        async void OnScopeCommitting(IUnitOfWorkScope scope)
        {
            Guard.Against<ObjectDisposedException>(_disposed,
                                                   "The transaction attached to the scope has already been disposed.");

            _logger.LogInformation("Commit signalled by scope {0} on transaction {1}.", scope.ScopeId, _transactionId);
           if (!_attachedScopes.Contains(scope))
           {
               Dispose();
               throw new InvalidOperationException("The scope being comitted is not attached to the current transaction.");
           }
            scope.ScopeComitting -= OnScopeCommitting;
            scope.ScopeRollingback -= OnScopeRollingBack;
            scope.Complete();
            _attachedScopes.Remove(scope);
            if (_attachedScopes.Count == 0)
            {
                _logger.LogInformation("All scopes have signalled a commit on transaction {0}. Flushing unit of work and comitting attached TransactionScope.", _transactionId);
                try
                {
                    await _unitOfWork.FlushAsync();
                    _transaction.Complete();
                }
                finally
                {
                    Dispose(); //Dispose the transaction after comitting.
                }
            }
        }

        /// <summary>
        /// Callback executed when an enlisted scope is rolledback.
        /// </summary>
        void OnScopeRollingBack(IUnitOfWorkScope scope)
        {
            Guard.Against<ObjectDisposedException>(_disposed,
                                                   "The transaction attached to the scope has already been disposed.");
            _logger.LogInformation("Rollback signalled by scope {0} on transaction {1}.", scope.ScopeId, _transactionId);
            _logger.LogInformation("Detaching all scopes and disposing of attached TransactionScope on transaction {0}", _transactionId);

            scope.ScopeComitting -= OnScopeCommitting;
            scope.ScopeRollingback -= OnScopeRollingBack;
            scope.Complete();
            _attachedScopes.Remove(scope);
            Dispose();
        }


        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _logger.LogInformation("Disposing off transction {0}", _transactionId);
                if (_unitOfWork != null)
                    _unitOfWork.Dispose();

                if (_transaction != null)
                    _transaction.Dispose();

                if (TransactionDisposing != null)
                    TransactionDisposing(this);

                if (_attachedScopes != null && _attachedScopes.Count > 0)
                {
                    _attachedScopes.ForEach(scope =>
                    {
                        scope.ScopeComitting -= OnScopeCommitting;
                        scope.ScopeRollingback -= OnScopeRollingBack;
                        scope.Complete();
                    });
                    _attachedScopes.Clear();     
                }
            }
            TransactionDisposing = null;
            _unitOfWork = null;
            _transaction = null;
            _attachedScopes = null;
            _disposed = true;
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            if (_disposed)
                await Task.CompletedTask;

            if (disposing)
            {
                _logger.LogInformation("Disposing off transction {0}", _transactionId);
                if (_unitOfWork != null)
                    await _unitOfWork.DisposeAsync();

                if (_transaction != null)
                    _transaction.Dispose();

                if (TransactionDisposing != null)
                    TransactionDisposing(this);

                if (_attachedScopes != null && _attachedScopes.Count > 0)
                {
                    _attachedScopes.ForEach(scope =>
                    {
                        scope.ScopeComitting -= OnScopeCommitting;
                        scope.ScopeRollingback -= OnScopeRollingBack;
                        scope.Complete();
                    });
                    _attachedScopes.Clear();
                }
            }
            TransactionDisposing = null;
            _unitOfWork = null;
            _transaction = null;
            _attachedScopes = null;
            _disposed = true;
            await Task.CompletedTask;
        }



    }
}