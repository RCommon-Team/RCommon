using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkConfiguration
    {
        /// <summary>
        /// Sets <see cref="UnitOfWorkScope"/> instances to auto complete when disposed.
        /// </summary>
        IUnitOfWorkConfiguration AutoCompleteScope();

        /// <summary>
        /// Sets the default isolation level used by <see cref="UnitOfWorkScope"/>.
        /// </summary>
        /// <param name="isolationLevel"></param>
        IUnitOfWorkConfiguration UseDefaultIsolation(IsolationLevel isolationLevel);

    }
}
