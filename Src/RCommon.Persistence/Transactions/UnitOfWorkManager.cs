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


        public UnitOfWorkManager(ILogger<UnitOfWorkManager> logger, IEventBus eventBus, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider)
        {
            _logger = logger;
            _eventBus = eventBus;
            _dataStoreEnlistmentProvider = dataStoreEnlistmentProvider;
        }

        public bool EnlistUnitOfWork(IUnitOfWork unitOfWork)
        {
            _currentUnitOfWork = unitOfWork;
            _logger.LogInformation("UnitOfWork {0} Enlisted.", unitOfWork.TransactionId);
            return true;
        }

        public async Task CommitUnitOfWorkAsync(IUnitOfWork unitOfWork)
        {
            _logger.LogInformation("UnitOfWork {0} Comitting.", unitOfWork.TransactionId);
            await FlushAsync(true, _dataStoreEnlistmentProvider.GetEnlistedDataStores(unitOfWork.TransactionId));
            _dataStoreEnlistmentProvider.RemoveEnlistedDataStores(unitOfWork.TransactionId);
            await _eventBus.PublishAsync(new UnitOfWorkCommittedEvent(unitOfWork));
        }

        public async Task RollbackUnitOfWorkAsync(IUnitOfWork unitOfWork)
        {
            _logger.LogInformation("UnitOfWork {0} Rolling Back.", unitOfWork.TransactionId);
            await _eventBus.PublishAsync(new UnitOfWorkRolledBackEvent(unitOfWork));
        }

        public async Task CompleteUnitOfWorkAsync(IUnitOfWork unitOfWork)
        {
            _logger.LogInformation("UnitOfWork {0} Completing.", unitOfWork.TransactionId);
            _dataStoreEnlistmentProvider.RemoveEnlistedDataStores(unitOfWork.TransactionId);
            await _eventBus.PublishAsync(new UnitOfWorkCompletedEvent(unitOfWork));
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

        /// <summary>
        /// Gets the current <see cref="IUnitOfWork"/> instance.
        /// </summary>
        public IUnitOfWork CurrentUnitOfWork
        {
            get
            {
                return _currentUnitOfWork;
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
