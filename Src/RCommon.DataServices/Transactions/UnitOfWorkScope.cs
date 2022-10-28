using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    public class UnitOfWorkScope : DisposableResource, IUnitOfWorkScope
    {
        private bool _disposed = false;
        private bool _commitAttempted = false;
        private bool _completed = false;
        private bool _started = false;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IDataStoreEnlistmentProvider _dataStoreEnlistmentProvider;
        private readonly Guid _transactionId;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<UnitOfWorkScope> _logger;
        private TransactionScope _transactionScope;
        private readonly UnitOfWorkSettings _unitOfWorkSettings;

        public event Action<IUnitOfWorkScope> ScopeComitting;
        public event Action<IUnitOfWorkScope> ScopeRollingback;
        public event Action<IUnitOfWorkScope> ScopeBeginning;
        public event Action<IUnitOfWorkScope> ScopeCompleted;

        public UnitOfWorkScope(IUnitOfWorkManager unitOfWorkManager, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider, IGuidGenerator guidGenerator, 
            ILogger<UnitOfWorkScope> logger, IOptions<UnitOfWorkSettings> unitOfWorkSettings)
        {
            _unitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
            _dataStoreEnlistmentProvider = dataStoreEnlistmentProvider ?? throw new ArgumentNullException(nameof(dataStoreEnlistmentProvider));
            _guidGenerator = guidGenerator ?? throw new ArgumentNullException(nameof(guidGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transactionId = _guidGenerator.Create();
            _unitOfWorkSettings = unitOfWorkSettings.Value;

            _unitOfWorkManager.EnlistUnitOfWork(this);
            
        }

        


        public void Begin(TransactionMode transactionMode, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            OnBegin();
            _transactionScope = TransactionScopeHelper.CreateScope(_logger, isolationLevel, transactionMode);
        }

        public void Begin(TransactionMode transactionMode)
        {
            OnBegin();
            _transactionScope = TransactionScopeHelper.CreateScope(_logger, _unitOfWorkSettings.DefaultIsolation, transactionMode);
        }


        ///<summary>
        /// Commits the current running transaction in the scope.
        ///</summary>
        public void Commit()
        {
            Guard.Against<ObjectDisposedException>(_disposed,
                "Cannot commit a disposed UnitOfWorkScope instance.");
            Guard.Against<InvalidOperationException>(_completed,
                "This unit of work scope has been marked completed. A child scope participating in the " +
                "transaction has rolledback and the transaction aborted. The parent scope cannot be commit.");

            
            _commitAttempted = true;
            OnCommit();
        }

        private void OnBegin()
        {
            Guard.Against<InvalidOperationException>(_started, 
                "This unit of work scope has already started and cannot begin again as it would disrupt the state of the current transaction.");
            
            _started = true;
            _logger.LogInformation("UnitOfWorkScope {0} Beginning.", this.TransactionId);
            if (ScopeBeginning != null)
            {
                ScopeBeginning(this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnCommit()
        {
            _logger.LogInformation("UnitOfWorkScope {0} Comitting.", this.TransactionId);
            if (ScopeComitting != null)
            {
                ScopeComitting(this);
            }
            this.Flush(true);
            _dataStoreEnlistmentProvider.RemoveEnlistedDataStores(this.TransactionId);
            _transactionScope.Complete();
            OnComplete();
            
        }

        private void OnComplete()
        {
            _completed = true;
            _logger.LogInformation("UnitOfWorkScope {0} Completed.", this.TransactionId);
            if (ScopeCompleted != null)
            {
                ScopeCompleted(this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnRollback()
        {
            _logger.LogInformation("UnitOfWorkScope {0} Rolling Back.", this.TransactionId);
            if (ScopeRollingback != null)
            {
                ScopeRollingback(this);
            }
            _dataStoreEnlistmentProvider.RemoveEnlistedDataStores(this.TransactionId);
        }

        private void Flush(bool allowPersist)
        {
            Guard.Against<ObjectDisposedException>(this._disposed, "The current UnitOfWork instance has been disposed. Cannot get registered IDataStores from a disposed UnitOfWork instance.");
            var dataStores = this._dataStoreEnlistmentProvider.GetEnlistedDataStores(this.TransactionId);

            foreach (var item in dataStores)
            {
                if (allowPersist)
                {
                    item.PersistChanges();
                }
            }
        }

        /// <summary>
        /// Gets the unique Transaction Id of the <see cref="UnitOfWorkScope"/>.
        /// </summary>
        /// <value>A <see cref="Guid"/> representing the unique Id of the Transaction.</value>
        public Guid TransactionId
        {
            get { return _transactionId; }
        }

        /// <summary>
        /// Disposes off the managed and un-managed resources used.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    if (_completed)
                    {
                        //Scope is marked as completed. Nothing to do here...
                        _disposed = true;
                        return;
                    }

                    if (!_commitAttempted && _unitOfWorkSettings.AutoCompleteScope)
                        //Scope did not try to commit before, and auto complete is switched on. Trying to commit.
                        //If an exception occurs here, the finally block will clean things up for us.
                        OnCommit();
                    else
                        //Scope either tried a commit before or auto complete is turned off. Trying to rollback.
                        //If an exception occurs here, the finally block will clean things up for us.
                        OnRollback();
                }
                finally
                {
                    _transactionScope.Dispose();
                    ScopeComitting = null;
                    ScopeRollingback = null;
                    _disposed = true;
                    _logger.LogDebug("UnitOfWorkScope {0} Disposed.", this.TransactionId);
                }
            }
        }

    }
}
