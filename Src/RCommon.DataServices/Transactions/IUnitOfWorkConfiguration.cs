using System;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkConfiguration
    {
        IUnitOfWorkConfiguration SetOptions(Action<UnitOfWorkSettings> unitOfWorkOptions);
    }
}