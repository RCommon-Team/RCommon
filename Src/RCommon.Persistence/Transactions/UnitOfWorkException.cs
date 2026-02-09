using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Exception thrown when a <see cref="IUnitOfWork"/> operation fails,
    /// such as attempting to commit an already completed or rolled-back scope.
    /// </summary>
    public class UnitOfWorkException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the unit of work failure.</param>
        public UnitOfWorkException(string message) : base(message)
        {

        }
    }
}
