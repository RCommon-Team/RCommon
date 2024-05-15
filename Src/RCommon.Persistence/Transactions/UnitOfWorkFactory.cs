using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBus _eventBus;
        private readonly IGuidGenerator _guidGenerator;

        public UnitOfWorkFactory(IServiceProvider serviceProvider, IEventBus eventBus, IGuidGenerator guidGenerator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _eventBus = eventBus;
            _guidGenerator = guidGenerator;
        }

        public async Task<IUnitOfWork> CreateAsync()
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            await _eventBus.PublishAsync(new UnitOfWorkCreatedEvent(unitOfWork.TransactionId));
            return unitOfWork;
        }

        public async Task<IUnitOfWork> CreateAsync(TransactionMode transactionMode)
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            unitOfWork.TransactionMode = transactionMode;
            await _eventBus.PublishAsync(new UnitOfWorkCreatedEvent(unitOfWork.TransactionId));
            return unitOfWork;
        }

        public async Task<IUnitOfWork> CreateAsync(TransactionMode transactionMode, IsolationLevel isolationLevel)
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            unitOfWork.TransactionMode = transactionMode;
            await _eventBus.PublishAsync(new UnitOfWorkCreatedEvent(unitOfWork.TransactionId));
            return unitOfWork;
        }
    }
}
