using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace RCommon.DataServices
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWorkFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IUnitOfWork Create()
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            unitOfWork.Begin(TransactionMode.Default);
            return unitOfWork;
        }

        public IUnitOfWork Create(TransactionMode transactionMode)
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            unitOfWork.Begin(transactionMode);
            return unitOfWork;
        }

        public IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel)
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            unitOfWork.Begin(transactionMode, isolationLevel);
            return unitOfWork;
        }
    }
}
