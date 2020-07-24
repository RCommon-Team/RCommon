using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.DataServices.Transactions
{
    public class UnitOfWorkScopeFactory : IUnitOfWorkScopeFactory
    {

        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public UnitOfWorkScopeFactory(IUnitOfWorkManager unitOfWorkManager) //TODO: IUnitOfWorkManager should subscribe to UnitOfWorkFactory.OnScopeCreated
        {
            this._unitOfWorkManager = unitOfWorkManager;
        }

        public IUnitOfWorkScope Create()
        {
            var scope = new UnitOfWorkScope(_unitOfWorkManager);
            _unitOfWorkManager.CurrentTransactionManager.EnlistScope(scope, TransactionMode.Default); //TODO: Probably should be event driven so we don't violate SRP
            return scope;
        }

        public IUnitOfWorkScope Create(TransactionMode mode)
        {
            var scope = new UnitOfWorkScope(_unitOfWorkManager);
            _unitOfWorkManager.CurrentTransactionManager.EnlistScope(scope, mode); //TODO: Probably should be event driven so we don't violate SRP
            return scope;
        }

        public IUnitOfWorkScope Create(TransactionMode mode, Action<IUnitOfWorkScope> customize)
        {
            var scope = new UnitOfWorkScope(_unitOfWorkManager);
            customize(scope);
            _unitOfWorkManager.CurrentTransactionManager.EnlistScope(scope, mode); // TODO: Probably should be event driven so we don't violate SRP
            return scope;
        }
    }
}
