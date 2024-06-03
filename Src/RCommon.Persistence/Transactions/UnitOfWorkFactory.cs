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

        public UnitOfWorkFactory(IServiceProvider serviceProvider, IGuidGenerator guidGenerator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _guidGenerator = guidGenerator;
        }

        public IUnitOfWork Create()
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            return unitOfWork;
        }

        public IUnitOfWork Create(TransactionMode transactionMode)
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            unitOfWork.TransactionMode = transactionMode;
            return unitOfWork;
        }

        public IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel)
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            unitOfWork.TransactionMode = transactionMode;
            return unitOfWork;
        }
    }
}
