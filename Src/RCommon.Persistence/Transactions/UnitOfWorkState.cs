using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Transactions
{
    public enum UnitOfWorkState
    {
        Created =1,
        CommitAttempted = 2,
        RolledBack = 3,
        Completed = 4,
        Disposed = 5
    }
}
