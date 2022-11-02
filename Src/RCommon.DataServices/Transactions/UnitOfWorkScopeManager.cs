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
