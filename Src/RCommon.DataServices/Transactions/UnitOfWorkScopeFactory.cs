using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    public class UnitOfWorkScopeFactory : IUnitOfWorkScopeFactory
    {
        private readonly IUnitOfWorkScope _unitOfWorkScope;

        public UnitOfWorkScopeFactory(IUnitOfWorkScope unitOfWorkScope)
        {
            _unitOfWorkScope=unitOfWorkScope;
        }

        public IUnitOfWorkScope Create()
        {
            _unitOfWorkScope.Begin(TransactionMode.Default);
            return _unitOfWorkScope;
        }

        public IUnitOfWorkScope Create(TransactionMode transactionMode)
        {
            _unitOfWorkScope.Begin(transactionMode);
            return _unitOfWorkScope;
        }

        public IUnitOfWorkScope Create(TransactionMode transactionMode, IsolationLevel isolationLevel)
        {
            _unitOfWorkScope.Begin(transactionMode, isolationLevel);
            return _unitOfWorkScope;
        }
    }
}
