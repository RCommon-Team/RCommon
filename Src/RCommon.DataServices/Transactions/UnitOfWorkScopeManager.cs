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
        private IUnitOfWorkScope _currentUnitOfWork;


        public UnitOfWorkScopeManager(ILogger<UnitOfWorkScopeManager> logger)
        {
            _logger = logger;
            this.EnlistedTransactions = new ConcurrentDictionary<Guid, IUnitOfWorkScope>();
        }

        public bool EnlistUnitOfWork(IUnitOfWorkScope unitOfWorkScope)
        {
            unitOfWorkScope.ScopeBeginning += OnUnitOfWorkScopeBeginning;
            unitOfWorkScope.ScopeCompleted += OnUnitOfWorkScopeCompleted;
            return this.EnlistedTransactions.TryAdd(unitOfWorkScope.TransactionId, unitOfWorkScope);
        }

        private void OnUnitOfWorkScopeCompleted(IUnitOfWorkScope unitOfWorkScope)
        {
            this.EnlistedTransactions.TryRemove(unitOfWorkScope.TransactionId, out _);
        }

        private void OnUnitOfWorkScopeBeginning(IUnitOfWorkScope unitOfWorkScope)
        {
            this._currentUnitOfWork = unitOfWorkScope;
        }

        /// <summary>
        /// Gets the current <see cref="IUnitOfWorkScope"/> instance.
        /// </summary>
        public IUnitOfWorkScope CurrentUnitOfWork
        {
            get
            {
                return this._currentUnitOfWork;
            }
        }

        public ConcurrentDictionary<Guid, IUnitOfWorkScope> EnlistedTransactions { get; }

        protected override void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            if (disposing)
            {
                this.EnlistedTransactions.Clear();
                this._disposed = true;
            }
        }
    }
}
