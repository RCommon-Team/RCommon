using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    public class UnitOfWorkScopeFactory : IUnitOfWorkScopeFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWorkScopeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IUnitOfWorkScope Create()
        {
            var unitOfWorkScope = this._serviceProvider.GetService<IUnitOfWorkScope>();
            unitOfWorkScope.Begin(TransactionMode.Default);
            return unitOfWorkScope;
        }

        public IUnitOfWorkScope Create(TransactionMode transactionMode)
        {
            var unitOfWorkScope = this._serviceProvider.GetService<IUnitOfWorkScope>();
            unitOfWorkScope.Begin(transactionMode);
            return unitOfWorkScope;
        }

        public IUnitOfWorkScope Create(TransactionMode transactionMode, IsolationLevel isolationLevel)
        {
            var unitOfWorkScope = this._serviceProvider.GetService<IUnitOfWorkScope>();
            unitOfWorkScope.Begin(transactionMode, isolationLevel);
            return unitOfWorkScope;
        }
    }
}
