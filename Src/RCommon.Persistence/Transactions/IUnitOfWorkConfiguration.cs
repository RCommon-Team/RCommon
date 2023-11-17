
/* Unmerged change from project 'RCommon.Persistence (net8.0)'
Before:
using System;
After:
using System;
using RCommon;
using RCommon.Persistence;
using RCommon.Persistence;
using RCommon.Persistence.Transactions;
*/
using System;

namespace RCommon.Persistence.Transactions
{
    public interface IUnitOfWorkConfiguration
    {
        IUnitOfWorkConfiguration SetOptions(Action<UnitOfWorkSettings> unitOfWorkOptions);
    }
}