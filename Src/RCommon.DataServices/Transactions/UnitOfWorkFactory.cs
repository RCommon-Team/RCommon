using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace RCommon.DataServices.Transactions
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
            var unitOfWorkScope = this._serviceProvider.GetService<IUnitOfWork>();
            unitOfWorkScope.Begin(TransactionMode.Default);
            return unitOfWorkScope;
        }

        public IUnitOfWork Create(TransactionMode transactionMode)
        {
            var unitOfWorkScope = this._serviceProvider.GetService<IUnitOfWork>();
            unitOfWorkScope.Begin(transactionMode);
            return unitOfWorkScope;
        }

        public IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel)
        {
            var unitOfWorkScope = this._serviceProvider.GetService<IUnitOfWork>();
            unitOfWorkScope.Begin(transactionMode, isolationLevel);
            return unitOfWorkScope;
        }
    }
}
