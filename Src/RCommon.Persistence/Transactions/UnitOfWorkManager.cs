using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling;

namespace RCommon.Persistence.Transactions
{

    public class UnitOfWorkManager : DisposableResource, IUnitOfWorkManager
    {
        private bool _disposed = false;
        private ILogger<UnitOfWorkManager> _logger;
        private readonly IEventBus _eventBus;
        private readonly IDataStoreEnlistmentProvider _dataStoreEnlistmentProvider;
        private IUnitOfWork _currentUnitOfWork;
        private Guid _currentUnitOfWorkTransactionId;


        public UnitOfWorkManager(ILogger<UnitOfWorkManager> logger, IEventBus eventBus, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider)
        {
            _logger = logger;
            _eventBus = eventBus;
            _dataStoreEnlistmentProvider = dataStoreEnlistmentProvider;
        }

        public bool EnlistUnitOfWork(IUnitOfWork unitOfWork)
        {
            _currentUnitOfWork = unitOfWork;
            _currentUnitOfWorkTransactionId = unitOfWork.TransactionId;
            _logger.LogInformation("UnitOfWork {0} Enlisted.", unitOfWork.TransactionId);
            return true;
        }

        public async Task CommitUnitOfWorkAsync(IUnitOfWork unitOfWork)
        {
            _logger.LogInformation("UnitOfWork {0} Comitting.", unitOfWork.TransactionId);
            await FlushAsync(true, _dataStoreEnlistmentProvider.GetEnlistedDataStores(unitOfWork.TransactionId));
            _dataStoreEnlistmentProvider.RemoveEnlistedDataStores(unitOfWork.TransactionId);
            await _eventBus.PublishAsync(new UnitOfWorkCommittedEvent(unitOfWork.TransactionId));
        }

        public async Task RollbackUnitOfWorkAsync(IUnitOfWork unitOfWork)
        {
            _logger.LogInformation("UnitOfWork {0} Rolling Back.", unitOfWork.TransactionId);
            await _eventBus.PublishAsync(new UnitOfWorkRolledBackEvent(unitOfWork.TransactionId));
        }

        public async Task CompleteUnitOfWorkAsync(IUnitOfWork unitOfWork)
        {
            _logger.LogInformation("UnitOfWork {0} Completing.", unitOfWork.TransactionId);
            _dataStoreEnlistmentProvider.RemoveEnlistedDataStores(unitOfWork.TransactionId);
            await _eventBus.PublishAsync(new UnitOfWorkCompletedEvent(unitOfWork.TransactionId));
        }

        private async Task FlushAsync(bool allowPersist, IList<IDataStore> dataStores)
        {
            if (dataStores.Count == 0)
            {
                throw new UnitOfWorkException("There were no enlisted data sources to persist changes against. This can happen when your repository is not enlisting data sources or if you instantiate repositories outside of a UnitOfWorkScope");
            }

            foreach (var item in dataStores)
            {
                if (allowPersist)
                {
                    await item.PersistChangesAsync();
                }
            }
        }


        [Obsolete("Please use UnitOfWorkManager.CurrentUnitOfWorkTransactionId. Public access will be removed in a future version and be limited to derived types.")]
        public IUnitOfWork CurrentUnitOfWork
        {
            // This should get removed as it exposes the UnitOfWork to modification outside this class.
            get
            {
                return _currentUnitOfWork;
            }
        }

        /// <summary>
        /// Gets the current <see cref="IUnitOfWork"/> Transaction Id.
        /// </summary>
        public Guid CurrentUnitOfWorkTransactionId
        {
            get
            {
                return _currentUnitOfWorkTransactionId;
            }
        }

        public bool IsUnitOfWorkActive
        {
            get
            {
                return (_currentUnitOfWork == null ? false : true);
            }
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            if (_disposed)
            {
                await Task.CompletedTask;
            }

            if (disposing)
            {
                _disposed = true;
                await this.DisposeAsync();
            }
        }
    }
}
