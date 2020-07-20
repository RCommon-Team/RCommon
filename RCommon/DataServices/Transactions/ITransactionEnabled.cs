using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.DataServices.Transactions
{
    public interface  ITransactionEnabled
    {

        /// <summary>
        /// Gets the a <see cref="IUnitOfWork"/> of <typeparamref name="T"/> that
        /// the repository will use to query the underlying store.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IUnitOfWork"/> implementation to retrieve.</typeparam>
        /// <returns>The <see cref="IUnitOfWork"/> implementation.</returns>
        T UnitOfWork<T>() where T : IUnitOfWork;

    }
}
