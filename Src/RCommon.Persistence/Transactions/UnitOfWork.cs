using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    public class UnitOfWork : DisposableResource, IUnitOfWork
    {
        private readonly ILogger<UnitOfWork> _logger;
        private readonly IGuidGenerator _guidGenerator;
        private UnitOfWorkState _state;
        private TransactionScope _transactionScope;

        public UnitOfWork(ILogger<UnitOfWork> logger, IGuidGenerator guidGenerator, IOptions<UnitOfWorkSettings> unitOfWorkSettings)
        {
            _logger = logger;
            _guidGenerator = guidGenerator;
            TransactionId = _guidGenerator.Create();

            TransactionMode = TransactionMode.Default;
            IsolationLevel = unitOfWorkSettings.Value.DefaultIsolation;
            AutoComplete = unitOfWorkSettings.Value.AutoCompleteScope;
            _state = UnitOfWorkState.Created;
            _transactionScope = TransactionScopeHelper.CreateScope(_logger, this);
        }
        public UnitOfWork(ILogger<UnitOfWork> logger, IGuidGenerator guidGenerator, TransactionMode transactionMode, IsolationLevel isolationLevel,
            IEventBus eventBus)
        {
            _logger = logger;
            _guidGenerator = guidGenerator;
            TransactionId = _guidGenerator.Create();

            TransactionMode = transactionMode;
            IsolationLevel = isolationLevel;
            AutoComplete = false;
            _state = UnitOfWorkState.Created;
            _transactionScope = TransactionScopeHelper.CreateScope(_logger, this);
        }

        public void Commit()
        {
            Guard.Against<ObjectDisposedException>(_state == UnitOfWorkState.Disposed,
                "Cannot commit a disposed UnitOfWorkScope instance.");
            Guard.Against<UnitOfWorkException>(_state == UnitOfWorkState.Completed,
                "This unit of work scope has been marked completed. A child scope participating in the " +
                "transaction has rolledback and the transaction aborted. The parent scope cannot be commited.");
            _state = UnitOfWorkState.CommitAttempted;
            this.Complete();
        }

        private void Rollback()
        {
            _state = UnitOfWorkState.RolledBack;
        }

        private void Complete()
        {
            _transactionScope.Complete();
            _state = UnitOfWorkState.Completed;
        }

        protected override void Dispose(bool disposing)
        {
            if (_state == UnitOfWorkState.Disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    if (_state == UnitOfWorkState.Completed)
                    {
                        //Scope is marked as completed. Nothing to do here...
                        _state = UnitOfWorkState.Disposed;
                        return;
                    }
                    if (_state != UnitOfWorkState.CommitAttempted && AutoComplete)
                    {
                        //Scope did not try to commit before, and auto complete is switched on. Trying to commit.
                        //If an exception occurs here, the finally block will clean things up for us.
                        this.Commit();
                    }
                    else
                    {
                        //Scope either tried a commit before or auto complete is turned off. Trying to rollback.
                        //If an exception occurs here, the finally block will clean things up for us.
                        this.Rollback();
                    }
                }
                finally
                {
                    _transactionScope.Dispose();
                    _state = UnitOfWorkState.Disposed;
                    _logger.LogDebug("UnitOfWork {0} Disposed.", TransactionId);
                    this.Dispose();
                }
            }
        }

        public Guid TransactionId { get; }
        public TransactionMode TransactionMode { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public bool AutoComplete { get; }

        public UnitOfWorkState State => _state;
    }
}
