
/* Unmerged change from project 'RCommon.DataServices (net8.0)'
Before:
using System;
After:
using System;
using RCommon;
using RCommon.DataServices;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
*/
using System;

namespace RCommon.DataServices
{
    public interface IUnitOfWorkConfiguration
    {
        IUnitOfWorkConfiguration SetOptions(Action<UnitOfWorkSettings> unitOfWorkOptions);
    }
}