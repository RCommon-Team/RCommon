using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Represents the lifecycle state of a <see cref="UnitOfWork"/>.
    /// </summary>
    public enum UnitOfWorkState
    {
        /// <summary>
        /// The unit of work has been created but no commit or rollback has been attempted.
        /// </summary>
        Created = 1,

        /// <summary>
        /// A commit has been attempted on the unit of work.
        /// </summary>
        CommitAttempted = 2,

        /// <summary>
        /// The unit of work has been rolled back.
        /// </summary>
        RolledBack = 3,

        /// <summary>
        /// The unit of work has been successfully completed (committed).
        /// </summary>
        Completed = 4,

        /// <summary>
        /// The unit of work has been disposed and can no longer be used.
        /// </summary>
        Disposed = 5
    }
}
