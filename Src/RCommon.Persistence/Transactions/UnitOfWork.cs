using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Default implementation of <see cref="IUnitOfWork"/> that wraps a <see cref="TransactionScope"/>
    /// to provide transactional consistency across persistence operations.
    /// </summary>
    /// <remarks>
    /// The unit of work transitions through the <see cref="UnitOfWorkState"/> lifecycle:
    /// Created -> CommitAttempted -> Completed -> Disposed, or Created -> RolledBack -> Disposed.
    /// Disposing without committing will trigger either auto-complete (if enabled) or rollback.
    /// </remarks>
    public class UnitOfWork : DisposableResource, IUnitOfWork
    {
        private readonly ILogger<UnitOfWork> _logger;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IEntityEventTracker? _eventTracker;
        private UnitOfWorkState _state;
        private TransactionScope _transactionScope;
        private bool _transactionScopeDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class using configured settings.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="guidGenerator">The GUID generator for creating the transaction identifier.</param>
        /// <param name="unitOfWorkSettings">The configured settings for isolation level and auto-complete behavior.</param>
        /// <param name="eventTracker">Optional entity event tracker for dispatching domain events after commit.</param>
        public UnitOfWork(ILogger<UnitOfWork> logger, IGuidGenerator guidGenerator, IOptions<UnitOfWorkSettings> unitOfWorkSettings, IEntityEventTracker? eventTracker = null)
        {
            _logger = logger;
            _guidGenerator = guidGenerator;
            _eventTracker = eventTracker;
            TransactionId = _guidGenerator.Create();

            TransactionMode = TransactionMode.Default;
            IsolationLevel = unitOfWorkSettings.Value.DefaultIsolation;
            AutoComplete = unitOfWorkSettings.Value.AutoCompleteScope;
            _state = UnitOfWorkState.Created;
            _transactionScope = TransactionScopeHelper.CreateScope(_logger, this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class with explicit transaction settings.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="guidGenerator">The GUID generator for creating the transaction identifier.</param>
        /// <param name="transactionMode">The transaction mode for this unit of work.</param>
        /// <param name="isolationLevel">The isolation level for the underlying transaction.</param>
        /// <param name="eventTracker">Optional entity event tracker for dispatching domain events after commit.</param>
        public UnitOfWork(ILogger<UnitOfWork> logger, IGuidGenerator guidGenerator, TransactionMode transactionMode, IsolationLevel isolationLevel, IEntityEventTracker? eventTracker = null)
        {
            _logger = logger;
            _guidGenerator = guidGenerator;
            _eventTracker = eventTracker;
            TransactionId = _guidGenerator.Create();

            TransactionMode = transactionMode;
            IsolationLevel = isolationLevel;
            AutoComplete = false;
            _state = UnitOfWorkState.Created;
            _transactionScope = TransactionScopeHelper.CreateScope(_logger, this);
        }

        /// <inheritdoc />
        [Obsolete("Use CommitAsync instead for automatic domain event dispatch.")]
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

        /// <inheritdoc />
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Guard.Against<ObjectDisposedException>(_state == UnitOfWorkState.Disposed,
                "Cannot commit a disposed UnitOfWorkScope instance.");
            Guard.Against<UnitOfWorkException>(_state == UnitOfWorkState.Completed,
                "This unit of work scope has been marked completed.");

            _state = UnitOfWorkState.CommitAttempted;

            // 1. Mark scope for commit
            _transactionScope.Complete();

            // 2. Dispose scope — this is where the actual DB commit occurs
            _transactionScope.Dispose();
            _transactionScopeDisposed = true;
            _state = UnitOfWorkState.Completed;

            // 3. Post-commit: dispatch domain events (transaction is fully committed)
            if (_eventTracker != null)
            {
                var dispatched = await _eventTracker
                    .EmitTransactionalEventsAsync()
                    .ConfigureAwait(false);

                if (!dispatched)
                {
                    _logger.LogWarning(
                        "UnitOfWork {TransactionId}: domain event dispatch returned false.",
                        TransactionId);
                }
            }
        }

        /// <summary>
        /// Marks the unit of work as rolled back, preventing the transaction from being committed.
        /// </summary>
        private void Rollback()
        {
            _state = UnitOfWorkState.RolledBack;
        }

        /// <summary>
        /// Completes the underlying <see cref="TransactionScope"/>, signaling that all operations succeeded.
        /// </summary>
        private void Complete()
        {
            _transactionScope.Complete();
            _state = UnitOfWorkState.Completed;
        }

        /// <summary>
        /// Disposes the unit of work, handling auto-complete or rollback based on current state.
        /// </summary>
        /// <param name="disposing"><c>true</c> if called from <see cref="IDisposable.Dispose"/>; <c>false</c> if from a finalizer.</param>
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
                    if (!_transactionScopeDisposed)
                    {
                        _transactionScope.Dispose();
                    }
                    _state = UnitOfWorkState.Disposed;
                    _logger.LogDebug("UnitOfWork {0} Disposed.", TransactionId);
                    base.Dispose(disposing);
                }
            }
        }

        /// <inheritdoc />
        public Guid TransactionId { get; }

        /// <inheritdoc />
        public TransactionMode TransactionMode { get; set; }

        /// <inheritdoc />
        public IsolationLevel IsolationLevel { get; set; }

        /// <inheritdoc />
        public bool AutoComplete { get; }

        /// <inheritdoc />
        public UnitOfWorkState State => _state;
    }
}
